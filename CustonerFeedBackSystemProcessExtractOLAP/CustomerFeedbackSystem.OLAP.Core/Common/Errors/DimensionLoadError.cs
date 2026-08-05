namespace CustomerFeedbackSystem.OLAP.Core.Common.Errors;

public sealed record DimensionLoadError(string DimensionName, string Reason)
    : Error("DIMENSION_LOAD", $"dimension '{DimensionName}' could not be loaded: {Reason}");
