namespace CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;

public sealed class CsvSourceOptions
{
    public const string SectionName = "Sources:Csv";

    public bool Enabled { get; set; } = true;
    public string BaseDirectory { get; set; } = string.Empty;

    public string SurveysFile { get; set; } = "surveys_part1.csv";
}
