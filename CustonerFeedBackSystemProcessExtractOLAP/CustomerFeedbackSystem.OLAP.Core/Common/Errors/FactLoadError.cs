namespace CustomerFeedbackSystem.OLAP.Core.Common.Errors;

public sealed record FactLoadError(string FactName, string Reason)
    : Error("FACT_LOAD", $"fact '{FactName}' could not be loaded: {Reason}");
