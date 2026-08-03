using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Staging;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Csv;

public sealed class CsvSurveyExtractor : IExtractor<StagingEncuestaCsv>
{
   
    private const int IdWidth = 50;
    private const int SatisfactionWidth = 10;
    private const int ClassificationWidth = 20;
    private const int SourceWidth = 50;
    private const int FileNameWidth = 255;
    private const int Unbounded = 0;   

    private const int StreamBufferSize = 64 * 1024;

    private readonly CsvSourceOptions _options;
    private readonly ITextSanitizer _sanitizer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CsvSurveyExtractor> _logger;

    public CsvSurveyExtractor(
        IOptions<CsvSourceOptions> options,
        ITextSanitizer sanitizer,
        TimeProvider timeProvider,
        ILogger<CsvSurveyExtractor> logger)
    {
        _options = options.Value;
        _sanitizer = sanitizer;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string SourceName => _options.SurveysFile;

    public async IAsyncEnumerable<Result<StagingEncuestaCsv>> ExtractAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var directory = CsvDirectoryResolver.TryResolve(_options.BaseDirectory);
        if (directory is null)
        {
            yield return Result<StagingEncuestaCsv>.Failure(new SourceUnavailableError(
                SourceName,
                "could not locate the 'CSV opiniones de clientes' folder; set Sources:Csv:BaseDirectory explicitly"));
            yield break;
        }

        var path = Path.Combine(directory, _options.SurveysFile);

      
        if (!File.Exists(path))
        {
            yield return Result<StagingEncuestaCsv>.Failure(
                new SourceUnavailableError(SourceName, $"file not found at '{path}'"));
            yield break;
        }

        
        var loadedAt = _timeProvider.GetUtcNow().UtcDateTime;
        var fileName = Path.GetFileName(path);
        var stopwatch = Stopwatch.StartNew();
        var emitted = 0L;

        await foreach (var result in ReadRowsAsync(path, fileName, loadedAt, cancellationToken))
        {
            if (result.IsSuccess)
            {
                emitted++;
            }

            yield return result;
        }

        stopwatch.Stop();
        LogThroughput(emitted, stopwatch.Elapsed);
    }

    private async IAsyncEnumerable<Result<StagingEncuestaCsv>> ReadRowsAsync(
        string path,
        string fileName,
        DateTime loadedAt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        StreamReader? streamReader = null;
        CsvReader? csvReader = null;
        Exception? openException = null;

        try
        {
           
            streamReader = new StreamReader(
                path,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: StreamBufferSize);

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim,
                DetectDelimiter = false,
            };

            csvReader = new CsvReader(streamReader, configuration);
            csvReader.Context.RegisterClassMap<SurveyCsvRowMap>();
        }
        catch (Exception ex)
        {
            openException = ex;
        }

        
        if (openException is not null)
        {
            streamReader?.Dispose();
            csvReader?.Dispose();
            yield return Result<StagingEncuestaCsv>.Failure(
                new SourceUnavailableError(SourceName, openException.Message));
            yield break;
        }

        using (streamReader)
        using (csvReader)
        {
            Exception? headerException = null;
            try
            {
                await csvReader!.ReadAsync();
                csvReader.ReadHeader();
            }
            catch (Exception ex)
            {
                headerException = ex;
            }

           
            if (headerException is not null)
            {
                yield return Result<StagingEncuestaCsv>.Failure(
                    new SourceUnavailableError(SourceName, $"invalid header: {headerException.Message}"));
                yield break;
            }

           
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fields = new RecordFieldSanitizer(_sanitizer, SourceName);
            var rowNumber = 1L;   

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool moved;
                Exception? readException = null;
                try
                {
                    moved = await csvReader!.ReadAsync();
                }
                catch (Exception ex)
                {
                    moved = false;
                    readException = ex;
                }

                if (readException is not null)
                {
                    yield return Result<StagingEncuestaCsv>.Failure(new RecordValidationError(
                        SourceName, rowNumber + 1, "<row>", $"could not be read: {readException.Message}"));
                    yield break;
                }

                if (!moved)
                {
                    yield break;
                }

                rowNumber++;

                SurveyCsvRow? row = null;
                Exception? mapException = null;
                try
                {
                    row = csvReader!.GetRecord<SurveyCsvRow>();
                }
                catch (Exception ex)
                {
                    mapException = ex;
                }

                if (mapException is not null)
                {
                    yield return Result<StagingEncuestaCsv>.Failure(new RecordValidationError(
                        SourceName, rowNumber, "<row>", $"could not be parsed: {mapException.Message}"));
                    continue;
                }

                foreach (var result in ProjectRow(row!, rowNumber, fileName, loadedAt, seenIds, fields))
                {
                    yield return result;
                }
            }
        }
    }

   
    private IEnumerable<Result<StagingEncuestaCsv>> ProjectRow(
        SurveyCsvRow row,
        long rowNumber,
        string fileName,
        DateTime loadedAt,
        HashSet<string> seenIds,
        RecordFieldSanitizer fields)
    {
        fields.BeginRecord(rowNumber);

        var idEncuesta = fields.Take(row.IdOpinion, IdWidth, "IdOpinion");
        if (RecordFieldSanitizer.IsMissing(idEncuesta))
        {
            yield return Result<StagingEncuestaCsv>.Failure(
                new RecordValidationError(SourceName, rowNumber, "IdOpinion", "is empty"));
            yield break;
        }

        if (!seenIds.Add(idEncuesta))
        {
            yield return Result<StagingEncuestaCsv>.Failure(
                new RecordValidationError(SourceName, rowNumber, "IdOpinion", "is duplicated"));
            yield break;
        }

      
        var comentarios = fields.Take(row.Comentario, Unbounded, "Comentario");
        if (RecordFieldSanitizer.IsMissing(comentarios))
        {
            yield return Result<StagingEncuestaCsv>.Failure(
                new RecordValidationError(SourceName, rowNumber, "Comentario", "is empty"));
            yield break;
        }

        var entity = new StagingEncuestaCsv
        {
            IdEncuestaRaw = idEncuesta,
            IdClienteRaw = fields.Take(row.IdCliente, IdWidth, "IdCliente"),
            IdProductoRaw = fields.Take(row.IdProducto, IdWidth, "IdProducto"),
            FechaEncuestaRaw = fields.Take(row.Fecha, IdWidth, "Fecha"),
            NivelSatisfaccionRaw = fields.Take(row.PuntajeSatisfaccion, SatisfactionWidth, "PuntajeSatisfacción"),
            ComentariosRaw = comentarios,
            ClasificacionRaw = fields.Take(row.Clasificacion, ClassificationWidth, "Clasificación"),
            FuenteRaw = fields.Take(row.Fuente, SourceWidth, "Fuente"),
            NombreArchivoMeta = fields.Take(fileName, FileNameWidth, "NombreArchivoMeta"),
            FechaCargaMeta = loadedAt,
        };

        foreach (var truncation in fields.Truncations)
        {
            yield return Result<StagingEncuestaCsv>.Failure(truncation);
        }

        yield return Result<StagingEncuestaCsv>.Success(entity);
    }

    private void LogThroughput(long rows, TimeSpan elapsed)
    {
        var rowsPerSecond = elapsed.TotalSeconds > 0 ? rows / elapsed.TotalSeconds : rows;

        _logger.LogInformation(
            "Source {Source} streamed {Rows} rows in {Elapsed} ({RowsPerSecond:F0} rows/s).",
            SourceName,
            rows,
            elapsed,
            rowsPerSecond);
    }
}
