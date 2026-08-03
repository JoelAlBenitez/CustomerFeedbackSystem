namespace CustomerFeedbackSystem.OLAP.Core.Common;


public abstract record Error(string Code, string Message)
{
    public sealed override string ToString() => $"[{Code}] {Message}";
}
