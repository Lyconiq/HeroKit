using System.Buffers;
using System.Buffers.Binary;

namespace HeroKit.FileTypes;

// Finds a VBA project in an OLE compound file (legacy DOC and XLS): Word keeps it in the "Macros" storage and Excel in
// "_VBA_PROJECT_CUR". Reads only the sector allocation table and the directory, never the streams, from a span with the whole
// file or from a seekable stream. A file that is cut short, damaged or larger than the limits below counts as without macros.
internal ref struct CompoundFileMacros
{
    private const uint EndOfChain = 0xFFFFFFFE;
    private const uint MaxRegularSector = 0xFFFFFFFA;
    private const int HeaderLength = 512;
    private const int DirectoryEntryLength = 128;
    private const int HeaderDifatCount = 109;

    // Directory and allocation table sectors read at most: a directory of about 16,000 entries in a file of up to 2 GB.
    private const int MaxDirectorySectors = 4096;
    private const int MaxDifatSectors = 4096;

    private readonly ReadOnlySpan<byte> _file;
    private readonly Stream? _stream;
    private readonly long _start;
    private int _sectorSize;

    private CompoundFileMacros(ReadOnlySpan<byte> file, Stream? stream, long start)
    {
        _file = file;
        _stream = stream;
        _start = start;
    }

    public static bool StartsLikeCompoundFile(ReadOnlySpan<byte> header) =>
        header.Length >= 8 && BinaryPrimitives.ReadUInt64LittleEndian(header) == 0xE11AB1A1E011CFD0;

    // The span must hold the whole file; later sectors are not seen otherwise.
    public static bool HasMacros(ReadOnlySpan<byte> file) =>
        StartsLikeCompoundFile(file) && new CompoundFileMacros(file, null, 0).Scan();

    // The stream must be seekable and start at position start; its position is not restored.
    public static bool HasMacros(Stream stream, long start) =>
        stream.CanSeek && new CompoundFileMacros(default, stream, start).Scan();

    private bool Scan()
    {
        Span<byte> header = stackalloc byte[HeaderLength];
        if (!TryRead(0, header) || !StartsLikeCompoundFile(header))
            return false;

        int shift = BinaryPrimitives.ReadUInt16LittleEndian(header[0x1E..]);
        if (shift is not (9 or 12))
            return false;

        _sectorSize = 1 << shift;
        byte[] sector = ArrayPool<byte>.Shared.Rent(_sectorSize);
        byte[] table = ArrayPool<byte>.Shared.Rent(_sectorSize);
        try
        {
            return ScanDirectory(header, sector.AsSpan(0, _sectorSize), table.AsSpan(0, _sectorSize));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sector);
            ArrayPool<byte>.Shared.Return(table);
        }
    }

    // Follows the directory's sector chain and checks every entry name.
    private readonly bool ScanDirectory(scoped ReadOnlySpan<byte> header, scoped Span<byte> sector, scoped Span<byte> table)
    {
        uint current = BinaryPrimitives.ReadUInt32LittleEndian(header[0x30..]);
        long loadedTable = -1;
        for (int count = 0; current <= MaxRegularSector && count < MaxDirectorySectors; count++)
        {
            if (!TryReadSector(current, sector))
                return false;

            for (int entry = 0; entry + DirectoryEntryLength <= sector.Length; entry += DirectoryEntryLength)
            {
                if (IsMacroStorage(sector.Slice(entry, DirectoryEntryLength)))
                    return true;
            }

            // The allocation table holds the next sector of the chain.
            int perSector = _sectorSize / 4;
            long tableIndex = current / perSector;
            if (tableIndex != loadedTable)
            {
                if (!TryFindTableSector(header, tableIndex, out uint tableSector) || !TryReadSector(tableSector, table))
                    return false;

                loadedTable = tableIndex;
            }

            current = BinaryPrimitives.ReadUInt32LittleEndian(table[(int)(current % perSector * 4)..]);
        }

        return false;
    }

    // Finds the location of the n-th allocation table sector: the first 109 are listed in the header, the rest in a chain of DIFAT sectors.
    private readonly bool TryFindTableSector(scoped ReadOnlySpan<byte> header, long index, out uint location)
    {
        location = 0;
        if (index < HeaderDifatCount)
        {
            location = BinaryPrimitives.ReadUInt32LittleEndian(header[(0x4C + (int)index * 4)..]);
            return location <= MaxRegularSector;
        }

        int perDifat = _sectorSize / 4 - 1;
        long remaining = index - HeaderDifatCount;
        uint difat = BinaryPrimitives.ReadUInt32LittleEndian(header[0x44..]);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(_sectorSize);
        try
        {
            Span<byte> sector = buffer.AsSpan(0, _sectorSize);
            for (int count = 0; difat <= MaxRegularSector && count < MaxDifatSectors; count++)
            {
                if (!TryReadSector(difat, sector))
                    return false;

                if (remaining < perDifat)
                {
                    location = BinaryPrimitives.ReadUInt32LittleEndian(sector[(int)(remaining * 4)..]);
                    return location <= MaxRegularSector;
                }

                remaining -= perDifat;
                difat = BinaryPrimitives.ReadUInt32LittleEndian(sector[(perDifat * 4)..]);
            }

            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    // A storage named "Macros" (Word) or "_VBA_PROJECT_CUR" (Excel). Names compare without case, as in the format.
    private static bool IsMacroStorage(ReadOnlySpan<byte> entry)
    {
        const byte Storage = 1;
        int nameBytes = BinaryPrimitives.ReadUInt16LittleEndian(entry[0x40..]);
        if (entry[0x42] != Storage || nameBytes is < 2 or > 64)
            return false;

        ReadOnlySpan<char> name = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, char>(entry[..(nameBytes - 2)]);
        return name.Equals("Macros", StringComparison.OrdinalIgnoreCase)
            || name.Equals("_VBA_PROJECT_CUR", StringComparison.OrdinalIgnoreCase);
    }

    private readonly bool TryReadSector(uint index, scoped Span<byte> destination) =>
        TryRead(((long)index + 1) * _sectorSize, destination);

    private readonly bool TryRead(long offset, scoped Span<byte> destination)
    {
        if (_stream is null)
        {
            if (offset < 0 || offset + destination.Length > _file.Length)
                return false;

            _file.Slice((int)offset, destination.Length).CopyTo(destination);
            return true;
        }

        if (_start + offset + destination.Length > _stream.Length)
            return false;

        _stream.Position = _start + offset;
        _stream.ReadExactly(destination);
        return true;
    }
}
