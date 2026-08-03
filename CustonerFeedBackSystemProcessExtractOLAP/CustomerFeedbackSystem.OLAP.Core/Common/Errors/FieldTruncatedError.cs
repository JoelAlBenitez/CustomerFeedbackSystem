namespace CustomerFeedbackSystem.OLAP.Core.Common.Errors;

public sealed record FieldTruncatedError(string SourceName, long RecordNumber, string Field, int MaxLength)
    : Error("TRUNCATED", $"{SourceName} record {RecordNumber}: field '{Field}' exceeded {MaxLength} characters and was truncated");
