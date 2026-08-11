using System.Globalization;
using System.Text;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;

public static class TextNormalization
{
    public static string Fold(string value)
    {
        var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static IReadOnlyList<string> TokenizeFolded(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var tokens = new List<string>();
        var builder = new StringBuilder();

        foreach (var character in Fold(text))
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (builder.Length > 0)
            {
                tokens.Add(builder.ToString());
                builder.Clear();
            }
        }

        if (builder.Length > 0)
        {
            tokens.Add(builder.ToString());
        }

        return tokens;
    }
}
