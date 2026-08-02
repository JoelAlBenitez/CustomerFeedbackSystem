using System.Globalization;

namespace CustomerFeedbackSystem.Data.Validation;

internal static class ValidationRules
{
    public static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);

    public static bool IsValidDate(string? value, out DateTime parsed)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
    }

    public static bool IsPositiveInt(string? value, out int parsed)
    {
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) && parsed > 0;
    }

    public static bool IsIntInRange(string? value, int min, int max, out int parsed)
    {
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed)
            && parsed >= min && parsed <= max;
    }

    public static bool MatchesPrefixedId(string? value, char prefix)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 2)
        {
            return false;
        }

        if (char.ToUpperInvariant(value[0]) != char.ToUpperInvariant(prefix))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]))
            {
                return false;
            }
        }

        return true;
    }
}
