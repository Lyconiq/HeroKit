namespace HeroKit.Globalization.Turkey;

internal static class TurkeyCountry
{
    public static Country Create() => new("TR", "Türkiye", Languages.Turkish)
    {
        Currency = Currencies.TurkishLira,
        PhoneNumbers = TurkeyPhoneNumberRules.Instance,
        VehiclePlates = TurkeyVehiclePlateRules.Instance,
        PostalCodes = TurkeyPostalCodeRules.Instance,
        NationalIds = TurkeyNationalIdRules.Instance,
        TaxIds = TurkeyTaxIdRules.Instance,
        Holidays = TurkeyHolidayCalendar.Instance,
        IbanRules = TurkeyIbanRules.Instance,
        Banks = TurkeyBanks.Instance,
        Regions = TurkeyProvinces.All,
        Districts = TurkeyDistricts.All,
        TimeZone = TimeZones.Find("Europe/Istanbul", "Turkey Standard Time", TimeSpan.FromHours(3), "Türkiye Time"),
    };
}
