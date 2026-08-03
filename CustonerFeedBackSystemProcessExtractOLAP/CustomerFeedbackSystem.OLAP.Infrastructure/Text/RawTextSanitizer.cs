using System.Globalization;
using System.Text;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Text;


public sealed class RawTextSanitizer : ITextSanitizer
{
    public const string TextSentinel = "-";

    public const string NumericSentinel = "0";

    public SanitizedText Sanitize(string? value, int maxLength, string sentinel = TextSentinel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new SanitizedText(sentinel, WasTruncated: false);
        }

       
        var normalized = value.Normalize(NormalizationForm.FormC);

        var collapsed = StripControlAndCollapseWhitespace(normalized);

        if (collapsed.Length == 0)
        {
            return new SanitizedText(sentinel, WasTruncated: false);
        }

        if (maxLength > 0 && collapsed.Length > maxLength)
        {
            return new SanitizedText(collapsed[..CutPoint(collapsed, maxLength)], WasTruncated: true);
        }

        return new SanitizedText(collapsed, WasTruncated: false);
    }

   
    private static int CutPoint(string value, int maxLength) =>
        char.IsHighSurrogate(value[maxLength - 1]) ? maxLength - 1 : maxLength;

   
    private static string StripControlAndCollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasSpace = false;

        foreach (var rune in value.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                if (builder.Length > 0 && !previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

           
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format
                or UnicodeCategory.Surrogate or UnicodeCategory.PrivateUse)
            {
                continue;
            }

            builder.Append(rune);
            previousWasSpace = false;
        }

        if (builder.Length > 0 && builder[^1] == ' ')
        {
            builder.Length--;
        }

        return builder.ToString();
    }
}
