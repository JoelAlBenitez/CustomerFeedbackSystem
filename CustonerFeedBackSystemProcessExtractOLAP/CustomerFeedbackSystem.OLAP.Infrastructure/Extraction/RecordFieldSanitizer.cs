using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Extraction;

internal sealed class RecordFieldSanitizer
{
    private readonly ITextSanitizer _sanitizer;
    private readonly string _sourceName;
    private readonly List<FieldTruncatedError> _truncations = [];

    private long _recordNumber;

    public RecordFieldSanitizer(ITextSanitizer sanitizer, string sourceName)
    {
        _sanitizer = sanitizer;
        _sourceName = sourceName;
    }

    public IReadOnlyList<FieldTruncatedError> Truncations => _truncations;

    public void BeginRecord(long recordNumber)
    {
        _recordNumber = recordNumber;
        _truncations.Clear();
    }

    public string Take(string? value, int maxLength, string field, string sentinel = "-")
    {
        var sanitized = _sanitizer.Sanitize(value, maxLength, sentinel);

        if (sanitized.WasTruncated)
        {
            _truncations.Add(new FieldTruncatedError(_sourceName, _recordNumber, field, maxLength));
        }

        return sanitized.Value;
    }

    public static bool IsMissing(string sanitized, string sentinel = "-") =>
        string.Equals(sanitized, sentinel, StringComparison.Ordinal);
}
