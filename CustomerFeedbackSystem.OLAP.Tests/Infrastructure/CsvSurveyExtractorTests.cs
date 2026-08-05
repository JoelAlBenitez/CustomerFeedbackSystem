using System.Text;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Staging;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Csv;
using CustomerFeedbackSystem.OLAP.Infrastructure.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace CustomerFeedbackSystem.OLAP.Tests.Infrastructure;

/// <summary>
/// Tested against temporary files the test itself writes and deletes. That is disk access,
/// but controlled and deterministic — abstracting the file system would be complexity this
/// does not pay for.
/// </summary>
public sealed class CsvSurveyExtractorTests : IDisposable
{
    private const string Header = "IdOpinion,IdCliente,IdProducto,Fecha,Comentario,Clasificación,PuntajeSatisfacción,Fuente";

    private static readonly DateTimeOffset FixedNow = new(2026, 8, 3, 14, 32, 8, TimeSpan.Zero);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"olap-csv-tests-{Guid.NewGuid():N}");

    public CsvSurveyExtractorTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    /// <summary>Writes UTF-8 without BOM and LF line endings, exactly like the real files.</summary>
    private string WriteCsv(string content, string fileName = "surveys_part1.csv")
    {
        var path = Path.Combine(_directory, fileName);
        File.WriteAllText(path, content.ReplaceLineEndings("\n"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }

    private CsvSurveyExtractor BuildExtractor(string fileName = "surveys_part1.csv") =>
        new(
            Options.Create(new CsvSourceOptions { BaseDirectory = _directory, SurveysFile = fileName }),
            new RawTextSanitizer(),
            new FakeTimeProvider(FixedNow),
            NullLogger<CsvSurveyExtractor>.Instance);

    private static async Task<List<Result<StagingEncuestaCsv>>> DrainAsync(CsvSurveyExtractor extractor)
    {
        var results = new List<Result<StagingEncuestaCsv>>();
        await foreach (var result in extractor.ExtractAsync())
        {
            results.Add(result);
        }

        return results;
    }

    [Fact]
    public async Task ExtractAsync_WithThreeValidRows_YieldsThreeEntities()
    {
        WriteCsv($"""
            {Header}
            1,8537,366,2025-07-15,"El producto está bien.",Neutra,3,EncuestaInterna
            2,2721,667,2024-12-07,"Ni malo ni excelente.",Neutra,3,EncuestaInterna
            3,1,2,2025-01-01,"Excelente compra.",Positiva,5,EncuestaInterna
            """);

        var results = await DrainAsync(BuildExtractor());

        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.IsSuccess);
        results[0].Value.IdEncuestaRaw.Should().Be("1");
        results[0].Value.NivelSatisfaccionRaw.Should().Be("3");
        results[0].Value.ClasificacionRaw.Should().Be("Neutra");
        results[0].Value.FuenteRaw.Should().Be("EncuestaInterna");
        results[0].Value.NombreArchivoMeta.Should().Be("surveys_part1.csv");
    }

    [Fact]
    public async Task ExtractAsync_WhenFileMissing_YieldsSourceUnavailableAndDoesNotThrow()
    {
        // The literal "if the CSVs are not found the system does not blow up" requirement.
        var results = await DrainAsync(BuildExtractor("does-not-exist.csv"));

        results.Should().ContainSingle();
        results[0].IsFailure.Should().BeTrue();
        results[0].Errors[0].Should().BeOfType<SourceUnavailableError>();
    }

    [Fact]
    public async Task ExtractAsync_WithHeaderOnly_YieldsNothingWithoutError()
    {
        WriteCsv(Header);

        var results = await DrainAsync(BuildExtractor());

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_WithEmptyIdOpinion_RejectsThatRowAndKeepsGoing()
    {
        WriteCsv($"""
            {Header}
            ,8537,366,2025-07-15,"Sin identificador.",Neutra,3,EncuestaInterna
            2,2721,667,2024-12-07,"Con identificador.",Neutra,3,EncuestaInterna
            """);

        var results = await DrainAsync(BuildExtractor());

        results.Should().HaveCount(2);
        results[0].Errors[0].Should().BeOfType<RecordValidationError>();
        results[1].IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractAsync_WithEmptyComentario_RejectsThatRow()
    {
        // No text means no opinion to analyse.
        WriteCsv($"""
            {Header}
            1,8537,366,2025-07-15,,Neutra,3,EncuestaInterna
            2,2721,667,2024-12-07,"Con texto.",Neutra,3,EncuestaInterna
            """);

        var results = await DrainAsync(BuildExtractor());

        results[0].IsFailure.Should().BeTrue();
        results[0].Errors[0].As<RecordValidationError>().Field.Should().Be("Comentario");
        results[1].IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractAsync_WithDuplicateIdOpinion_DiscardsTheSecond()
    {
        WriteCsv($"""
            {Header}
            1,8537,366,2025-07-15,"Primera.",Neutra,3,EncuestaInterna
            1,2721,667,2024-12-07,"Duplicada.",Neutra,3,EncuestaInterna
            """);

        var results = await DrainAsync(BuildExtractor());

        results[0].IsSuccess.Should().BeTrue();
        results[1].Errors[0].As<RecordValidationError>().Reason.Should().Be("is duplicated");
    }

    [Fact]
    public async Task ExtractAsync_WithEmptyIdCliente_UsesTheSentinel()
    {
        // Empty non-key fields never reject a row: the sentinel applies and the T phase
        // resolves it to the anonymous customer.
        WriteCsv($"""
            {Header}
            1,,366,2025-07-15,"Sin cliente.",Neutra,3,EncuestaInterna
            """);

        var results = await DrainAsync(BuildExtractor());

        results.Should().ContainSingle();
        results[0].Value.IdClienteRaw.Should().Be("-");
    }

    [Fact]
    public async Task ExtractAsync_PreservesAccentsAndEnye()
    {
        WriteCsv($"""
            {Header}
            1,8537,366,2025-07-15,"El niño rompió la caña; ¡pésimo!",Negativa,1,EncuestaInterna
            """);

        var results = await DrainAsync(BuildExtractor());

        results[0].Value.ComentariosRaw.Should().Be("El niño rompió la caña; ¡pésimo!");
    }

    [Fact]
    public async Task ExtractAsync_StampsEveryRowWithTheSameLoadInstant()
    {
        WriteCsv($"""
            {Header}
            1,8537,366,2025-07-15,"Uno.",Neutra,3,EncuestaInterna
            2,2721,667,2024-12-07,"Dos.",Neutra,3,EncuestaInterna
            """);

        var results = await DrainAsync(BuildExtractor());

        results.Should().OnlyContain(r => r.Value.FechaCargaMeta == FixedNow.UtcDateTime);
    }

    [Fact]
    public async Task ExtractAsync_WithAShortRow_FillsTheMissingFieldsWithSentinels()
    {
        // MissingFieldFound is disabled, so a short row yields nulls that become sentinels.
        WriteCsv($"""
            {Header}
            1,8537,366,2025-07-15,"Fila corta."
            """);

        var results = await DrainAsync(BuildExtractor());

        results.Should().ContainSingle();
        results[0].Value.ClasificacionRaw.Should().Be("-");
        results[0].Value.FuenteRaw.Should().Be("-");
    }
}
