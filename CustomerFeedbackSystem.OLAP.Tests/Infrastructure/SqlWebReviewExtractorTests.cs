using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Extraction;
using CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Database;
using CustomerFeedbackSystem.OLAP.Infrastructure.Text;
using CustomerFeedbackSystem.OLAP.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace CustomerFeedbackSystem.OLAP.Tests.Infrastructure;

/// <summary>
/// SqlConnection is sealed and cannot be doubled, so what gets tested here is the part that
/// IS isolable: the row-to-entity projection, which takes primitives rather than a
/// SqlDataReader precisely so this test can exist. The real connection belongs to an
/// integration test run by hand, not to this suite.
/// </summary>
public sealed class SqlWebReviewExtractorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 3, 14, 32, 8, TimeSpan.Zero);

    private static SqlWebReviewExtractor BuildExtractor() =>
        new(
            Options.Create(new OltpConnectionOptions { ConnectionString = "Server=nowhere;" }),
            Options.Create(new DatabaseSourceOptions()),
            new RawTextSanitizer(),
            new FakeTimeProvider(FixedNow),
            NullLogger<SqlWebReviewExtractor>.Instance);

    private static RecordFieldSanitizer BuildFields()
    {
        var fields = new RecordFieldSanitizer(new RawTextSanitizer(), "test");
        fields.BeginRecord(1);
        return fields;
    }

    [Fact]
    public void Project_ConvertsIntegersToText()
    {
        var entity = BuildExtractor().Project(
            idReview: 12,
            idCliente: 7,
            idProducto: 16,
            rating: 4,
            comentarios: "Producto llegó rápido.",
            fechaCargaFuenteWeb: new DateTime(2024, 10, 23),
            loadedAt: FixedNow.UtcDateTime,
            fields: BuildFields());

        entity.IdResenaRaw.Should().Be("12");
        entity.IdUsuarioRaw.Should().Be("7");
        entity.IdProductoRaw.Should().Be("16");
        entity.EstrellasRaw.Should().Be("4");
    }

    [Fact]
    public void Project_WithoutAWebSourceRow_UsesTheDateSentinel()
    {
        // OUTER APPLY returns NULL when there is no 'Web' source row; the review must still
        // come through, with the sentinel rather than being dropped.
        var entity = BuildExtractor().Project(
            idReview: 1, idCliente: 1, idProducto: 1, rating: 3,
            comentarios: "Sin fuente web registrada.",
            fechaCargaFuenteWeb: null,
            loadedAt: FixedNow.UtcDateTime,
            fields: BuildFields());

        entity.FechaPublicacionRaw.Should().Be("-");
    }

    [Fact]
    public void Project_WithAWebSourceRow_FormatsTheDateAsIsoDate()
    {
        var entity = BuildExtractor().Project(
            idReview: 1, idCliente: 1, idProducto: 1, rating: 3,
            comentarios: "Con fuente web.",
            fechaCargaFuenteWeb: new DateTime(2024, 10, 23, 18, 45, 0),
            loadedAt: FixedNow.UtcDateTime,
            fields: BuildFields());

        entity.FechaPublicacionRaw.Should().Be("2024-10-23");
    }

    [Fact]
    public void Project_AlwaysUsesTheSentinelForTitle()
    {
        // No source anywhere supplies a review title, and the column is NOT NULL.
        var entity = BuildExtractor().Project(
            idReview: 1, idCliente: 1, idProducto: 1, rating: 5,
            comentarios: "Cuerpo de la reseña.",
            fechaCargaFuenteWeb: null,
            loadedAt: FixedNow.UtcDateTime,
            fields: BuildFields());

        entity.TituloResenaRaw.Should().Be("-");
    }

    [Fact]
    public void Project_PreservesAccentsInTheBody()
    {
        var entity = BuildExtractor().Project(
            idReview: 1, idCliente: 1, idProducto: 1, rating: 5,
            comentarios: "Gran relación calidad-precio. ¡Muy recomendable!",
            fechaCargaFuenteWeb: null,
            loadedAt: FixedNow.UtcDateTime,
            fields: BuildFields());

        entity.CuerpoResenaRaw.Should().Be("Gran relación calidad-precio. ¡Muy recomendable!");
    }

    [Fact]
    public void Project_StampsTheRunLoadInstant()
    {
        var entity = BuildExtractor().Project(
            idReview: 1, idCliente: 1, idProducto: 1, rating: 5,
            comentarios: "Texto.",
            fechaCargaFuenteWeb: null,
            loadedAt: FixedNow.UtcDateTime,
            fields: BuildFields());

        entity.FechaCargaMeta.Should().Be(FixedNow.UtcDateTime);
    }

    [Fact]
    public async Task ExtractAsync_WithNoConnectionString_YieldsSourceUnavailableWithoutThrowing()
    {
        var extractor = new SqlWebReviewExtractor(
            Options.Create(new OltpConnectionOptions { ConnectionString = string.Empty }),
            Options.Create(new DatabaseSourceOptions()),
            new RawTextSanitizer(),
            new FakeTimeProvider(FixedNow),
            NullLogger<SqlWebReviewExtractor>.Instance);

        var results = await extractor.ExtractAsync().DrainAsync();

        results.Should().ContainSingle();
        results[0].IsFailure.Should().BeTrue();
    }
}
