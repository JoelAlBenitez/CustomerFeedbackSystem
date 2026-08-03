namespace CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;

public sealed class StagingTableDescriptor<T>
{
    public required string SchemaName { get; init; }

    public required string TableName { get; init; }

    public required IReadOnlyList<string> ColumnNames { get; init; }

    public required Func<T, object[]> ValueSelector { get; init; }

    public string QualifiedName => $"[{SchemaName}].[{TableName}]";
}
