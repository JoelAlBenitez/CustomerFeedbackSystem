using System.Globalization;

namespace CustomerFeedbackSystem.Data.Transformation;

/// <summary>
/// Strips a source id down to its numeric part: "C007" and "P016" (social_comments.csv,
/// web_reviews.csv) as well as plain "8537" (surveys_part1.csv) all resolve to an int
/// that is looked up against the same Clientes/Productos catalog dictionaries.
/// </summary>
internal static class IdParsing
{
    public static int ExtractNumericId(string raw)
    {
        var start = 0;
        while (start < raw.Length && !char.IsDigit(raw[start]))
        {
            start++;
        }

        return int.Parse(raw[start..], CultureInfo.InvariantCulture);
    }
}
