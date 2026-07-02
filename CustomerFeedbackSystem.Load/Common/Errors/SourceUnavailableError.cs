namespace CustomerFeedbackSystem.Load.Common.Errors;

public sealed record SourceUnavailableError(string SourceFile, string Reason)
    : Error("SOURCE_UNAVAILABLE", $"{SourceFile} could not be read: {Reason}");
