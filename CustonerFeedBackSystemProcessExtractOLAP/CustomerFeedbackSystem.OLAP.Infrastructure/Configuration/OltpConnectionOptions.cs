namespace CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;

public sealed class OltpConnectionOptions
{
    public const string ConnectionStringName = "CustomerReviewSystemData";

    public string ConnectionString { get; set; } = string.Empty;
}
