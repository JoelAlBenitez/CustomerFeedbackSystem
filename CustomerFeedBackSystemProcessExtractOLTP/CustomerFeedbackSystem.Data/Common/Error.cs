namespace CustomerFeedbackSystem.Data.Common;

public abstract record Error(string Code, string Message)
{
    public sealed override string ToString() => $"[{Code}] {Message}";
}
