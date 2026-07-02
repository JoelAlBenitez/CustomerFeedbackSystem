namespace CustomerFeedbackSystem.Load.Configuration;

public sealed class CsvSourcesOptions
{
    public const string SectionName = "CsvSources";

    public string BaseDirectory { get; set; } = string.Empty;

    public string ClientsFile { get; set; } = "clients.csv";

    public string ProductsFile { get; set; } = "products.csv";

    public string DataSourcesFile { get; set; } = "fuente_datos.csv";

    public string SocialCommentsFile { get; set; } = "social_comments.csv";

    public string SurveysFile { get; set; } = "surveys_part1.csv";

    public string WebReviewsFile { get; set; } = "web_reviews.csv";

    public string ResolveClientsPath() => Path.Combine(BaseDirectory, ClientsFile);

    public string ResolveProductsPath() => Path.Combine(BaseDirectory, ProductsFile);

    public string ResolveDataSourcesPath() => Path.Combine(BaseDirectory, DataSourcesFile);

    public string ResolveSocialCommentsPath() => Path.Combine(BaseDirectory, SocialCommentsFile);

    public string ResolveSurveysPath() => Path.Combine(BaseDirectory, SurveysFile);

    public string ResolveWebReviewsPath() => Path.Combine(BaseDirectory, WebReviewsFile);
}
