namespace CustomerFeedbackSystem.Load.Common.Errors;

public sealed record ReferentialIntegrityError(string SourceFile, int RowNumber, string ForeignKeyField, string Value)
    : Error("REFERENTIAL_INTEGRITY", $"{SourceFile} row {RowNumber}: '{ForeignKeyField}' value '{Value}' does not resolve to an existing record");
