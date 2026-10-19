namespace HeroKit.Globalization;

internal static class TimeZones
{
    // Finds a zone by its IANA id, then by its Windows id. Without time zone data (for example invariant globalization on
    // Windows), falls back to a fixed offset, which is right for countries without daylight saving time such as Türkiye since 2016.
    public static TimeZoneInfo Find(string ianaId, string windowsId, TimeSpan fixedOffset, string displayName)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out TimeZoneInfo? zone) || TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out zone))
            return zone;

        return TimeZoneInfo.CreateCustomTimeZone(ianaId, fixedOffset, displayName, displayName);
    }
}
