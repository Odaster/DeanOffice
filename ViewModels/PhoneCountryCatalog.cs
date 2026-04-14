namespace DeanOfficeCourseWork.ViewModels;

public record PhoneCountryOption(
    string CountryCode,
    string CountryName,
    string CallingCode,
    int MinDigits,
    int MaxDigits);

public static class PhoneCountryCatalog
{
    public static IReadOnlyList<PhoneCountryOption> Countries { get; } =
    [
        new("BY", "Беларусь", "+375", 9, 9),
        new("RU", "Россия", "+7", 10, 10),
        new("KZ", "Казахстан", "+7", 10, 10),
        new("UA", "Украина", "+380", 9, 9),
        new("PL", "Польша", "+48", 9, 9),
        new("DE", "Германия", "+49", 10, 11),
        new("LT", "Литва", "+370", 8, 8),
        new("LV", "Латвия", "+371", 8, 8),
        new("US", "США", "+1", 10, 10)
    ];

    public static PhoneCountryOption DefaultCountry => Countries[0];

    public static PhoneCountryOption FindByCountryCode(string? countryCode)
    {
        return Countries.FirstOrDefault(x => string.Equals(x.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase))
            ?? DefaultCountry;
    }

    public static PhoneCountryOption FindByPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return DefaultCountry;
        }

        var normalized = NormalizeInternationalPhone(phoneNumber);
        if (normalized.StartsWith("+77", StringComparison.Ordinal))
        {
            return FindByCountryCode("KZ");
        }

        if (normalized.StartsWith("+79", StringComparison.Ordinal))
        {
            return FindByCountryCode("RU");
        }

        return Countries
            .OrderByDescending(x => x.CallingCode.Length)
            .FirstOrDefault(x => normalized.StartsWith(x.CallingCode, StringComparison.Ordinal))
            ?? DefaultCountry;
    }

    public static string ExtractLocalNumber(string? phoneNumber, PhoneCountryOption country)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        var digits = OnlyDigits(phoneNumber);
        var callingDigits = OnlyDigits(country.CallingCode);
        return digits.StartsWith(callingDigits, StringComparison.Ordinal)
            ? digits[callingDigits.Length..]
            : digits;
    }

    public static bool TryBuildInternationalNumber(
        string? countryCode,
        string? localNumber,
        out string? phoneNumber,
        out string? error)
    {
        phoneNumber = null;
        error = null;

        if (string.IsNullOrWhiteSpace(localNumber))
        {
            return true;
        }

        var country = FindByCountryCode(countryCode);
        var digits = OnlyDigits(localNumber);
        var callingDigits = OnlyDigits(country.CallingCode);

        if (digits.StartsWith(callingDigits, StringComparison.Ordinal) && digits.Length > country.MaxDigits)
        {
            digits = digits[callingDigits.Length..];
        }

        if (digits.Length < country.MinDigits || digits.Length > country.MaxDigits)
        {
            error = country.MinDigits == country.MaxDigits
                ? $"Для страны {country.CountryName} номер должен содержать {country.MinDigits} цифр без кода страны."
                : $"Для страны {country.CountryName} номер должен содержать от {country.MinDigits} до {country.MaxDigits} цифр без кода страны.";
            return false;
        }

        phoneNumber = country.CallingCode + digits;
        return true;
    }

    private static string NormalizeInternationalPhone(string phoneNumber)
    {
        var digits = OnlyDigits(phoneNumber);
        return phoneNumber.TrimStart().StartsWith('+') ? "+" + digits : digits;
    }

    private static string OnlyDigits(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }
}
