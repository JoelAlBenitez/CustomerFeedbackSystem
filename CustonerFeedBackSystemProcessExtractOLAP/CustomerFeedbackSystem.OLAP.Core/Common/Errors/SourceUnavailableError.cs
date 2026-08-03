namespace CustomerFeedbackSystem.OLAP.Core.Common.Errors;

public sealed record SourceUnavailableError(string SourceName, string Reason)
    : Error("SOURCE_UNAVAILABLE", $"{SourceName} could not be read: {Reason}");
