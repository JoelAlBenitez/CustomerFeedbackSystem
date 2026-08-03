namespace CustomerFeedbackSystem.OLAP.Api.Configuration;

public sealed class PagingOptions
{
    public const string SectionName = "Paging";

    public int DefaultPageSize { get; set; } = 500;

 
    public int MaxPageSize { get; set; } = 1000;
}
