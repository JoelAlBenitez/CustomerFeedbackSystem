using System.Globalization;
using CustomerFeedbackSystem.OLAP.Core.Dimensions;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;

public static class RawValueParsing
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
    ];

    public static bool TryParseDate(string? raw, out DateTime date)
    {
        date = default;

        if (DimensionSentinels.IsMissing(raw))
        {
            return false;
        }

        var value = raw!.Trim();

        if (DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var exact))
        {
            date = exact.Date;
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose))
        {
            date = loose.Date;
            return true;
        }

        return false;
    }

    public static bool TryExtractNumericId(string? raw, out int id)
    {
        id = 0;

        if (DimensionSentinels.IsMissing(raw))
        {
            return false;
        }

        var value = raw!.Trim();
        var start = 0;
        while (start < value.Length && !char.IsDigit(value[start]))
        {
            start++;
        }

        return start < value.Length
            && int.TryParse(value[start..], NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
    }

    public static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    public static string OrUnknownAttribute(string? value) =>
        DimensionSentinels.IsMissing(value) ? DimensionSentinels.UnknownAttribute : value!.Trim();
}
