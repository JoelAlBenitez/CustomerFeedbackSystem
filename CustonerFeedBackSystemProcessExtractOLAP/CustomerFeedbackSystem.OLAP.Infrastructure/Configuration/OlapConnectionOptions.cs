namespace CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;

public sealed class OlapConnectionOptions
{
    public const string ConnectionStringName = "CustomerReviewSystemDataOLAP";

    public string ConnectionString { get; set; } = string.Empty;
}
