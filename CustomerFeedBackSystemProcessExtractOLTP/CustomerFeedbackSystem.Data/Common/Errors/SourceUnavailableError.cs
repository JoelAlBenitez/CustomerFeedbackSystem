namespace CustomerFeedbackSystem.Data.Common.Errors;

public sealed record SourceUnavailableError(string SourceFile, string Reason)
    : Error("SOURCE_UNAVAILABLE", $"{SourceFile} could not be read: {Reason}");
