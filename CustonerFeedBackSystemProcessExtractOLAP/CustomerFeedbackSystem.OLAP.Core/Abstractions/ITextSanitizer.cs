namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;
public readonly record struct SanitizedText(string Value, bool WasTruncated);
public interface ITextSanitizer
{
   
    SanitizedText Sanitize(string? value, int maxLength, string sentinel = "-");
}
