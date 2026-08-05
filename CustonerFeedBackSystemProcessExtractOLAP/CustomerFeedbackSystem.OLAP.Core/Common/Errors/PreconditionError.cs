namespace CustomerFeedbackSystem.OLAP.Core.Common.Errors;

public sealed record PreconditionError(string Requirement, string Reason)
    : Error("PRECONDITION", $"{Requirement}: {Reason}");
