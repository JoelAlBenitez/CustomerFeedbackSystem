namespace CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;

public sealed class ApiSourceOptions
{
    public const string SectionName = "Sources:Api";

    public bool Enabled { get; set; } = true;

    public string BaseUrl { get; set; } = string.Empty;

    public string SocialCommentsPath { get; set; } = "/api/v1/social-comments";

    public string HealthPath { get; set; } = "/health";

    
    public int PageSize { get; set; } = 500;

    
    public int MaxPages { get; set; } = 10_000;

    public int TimeoutSeconds { get; set; } = 30;

    
    public string ApiKey { get; set; } = string.Empty;
}
