namespace CustomerFeedbackSystem.Load.Common.Errors;

public sealed record ValidationError(string SourceFile, int RowNumber, string Field, string Reason)
    : Error("VALIDATION", $"{SourceFile} row {RowNumber}: field '{Field}' {Reason}");
