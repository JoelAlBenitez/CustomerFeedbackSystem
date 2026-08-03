namespace CustomerFeedbackSystem.OLAP.Core.Common.Errors;

public sealed record RecordValidationError(string SourceName, long RecordNumber, string Field, string Reason)
    : Error("VALIDATION", $"{SourceName} record {RecordNumber}: field '{Field}' {Reason}");
