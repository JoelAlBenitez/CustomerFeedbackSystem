using CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;
using CustomerFeedbackSystem.OLAP.Tests.Fakes;
using FluentAssertions;

namespace CustomerFeedbackSystem.OLAP.Tests.Infrastructure;

/// <summary>
/// Short tests that catch a class of bug which is notoriously hard to diagnose at runtime:
/// a column/value mismatch surfaces as a SqlBulkCopy message that does not name the offending
/// column. These few lines save an afternoon.
/// </summary>
public sealed class StagingDescriptorTests
{
    [Fact]
    public void EncuestasDescriptor_ColumnCountMatchesValueCount()
    {
        var descriptor = StagingDescriptors.Encuestas;

        descriptor.ValueSelector(StagingSamples.Encuesta())
            .Should().HaveCount(descriptor.ColumnNames.Count);
    }

    [Fact]
    public void ResenasDescriptor_ColumnCountMatchesValueCount()
    {
        var descriptor = StagingDescriptors.Resenas;

        descriptor.ValueSelector(StagingSamples.Resena())
            .Should().HaveCount(descriptor.ColumnNames.Count);
    }

    [Fact]
    public void SocialesDescriptor_ColumnCountMatchesValueCount()
    {
        var descriptor = StagingDescriptors.Sociales;

        descriptor.ValueSelector(StagingSamples.Social())
            .Should().HaveCount(descriptor.ColumnNames.Count);
    }

    [Theory]
    [InlineData("FechaEncuesta_Raw")]
    [InlineData("ClasificacionRaw")]
    [InlineData("FuenteRaw")]
    public void EncuestasDescriptor_UsesTheSchemaColumnNames(string columnName)
    {
        // The underscored name is where the mistake is easiest to make: the C# property is
        // FechaEncuestaRaw while the column is FechaEncuesta_Raw.
        StagingDescriptors.Encuestas.ColumnNames.Should().Contain(columnName);
    }

    [Theory]
    [InlineData("UsuarioRedSocial_Raw")]
    [InlineData("Interacciones_Raw")]
    [InlineData("EndpointAPIMeta")]
    public void SocialesDescriptor_UsesTheSchemaColumnNames(string columnName)
    {
        StagingDescriptors.Sociales.ColumnNames.Should().Contain(columnName);
    }

    [Fact]
    public void QualifiedNames_AreBracketedAndSchemaPrefixed()
    {
        StagingDescriptors.Encuestas.QualifiedName.Should().Be("[Staging].[stgEncuestasCSV]");
        StagingDescriptors.Resenas.QualifiedName.Should().Be("[Staging].[stgResenasWebBD]");
        StagingDescriptors.Sociales.QualifiedName.Should().Be("[Staging].[stgRedesSocialesAPI]");
    }

    [Fact]
    public void AllQualifiedNames_CoversExactlyTheThreeStagingTables()
    {
        // This set is the allow-list StagingResetService validates against before
        // concatenating a table name into a TRUNCATE statement.
        StagingDescriptors.AllQualifiedNames.Should().HaveCount(3);
        StagingDescriptors.AllQualifiedNames.Should().NotContain("[Staging].[anything else]");
    }

    [Fact]
    public void EncuestasDescriptor_ValuesLineUpWithTheirColumns()
    {
        // The column order IS the contract: the value array must match position by position.
        var descriptor = StagingDescriptors.Encuestas;
        var sample = StagingSamples.Encuesta("42");
        var values = descriptor.ValueSelector(sample);

        var indexOfId = descriptor.ColumnNames.ToList().IndexOf("IdEncuestaRaw");
        var indexOfClasificacion = descriptor.ColumnNames.ToList().IndexOf("ClasificacionRaw");

        values[indexOfId].Should().Be("42");
        values[indexOfClasificacion].Should().Be(sample.ClasificacionRaw);
    }
}
