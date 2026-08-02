namespace CustomerFeedbackSystem.Data.Common.Errors;

public sealed record DuplicateRecordError(string SourceFile, int RowNumber, string Key)
    : Error("DUPLICATE", $"{SourceFile} row {RowNumber}: duplicate record for key '{Key}'");
