using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Staging;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Database;

public sealed class SqlWebReviewExtractor : IExtractor<StagingResenaWeb>
{
    private const int IdWidth = 50;
    private const int StarsWidth = 10;
    private const int TitleWidth = 255;
    private const int Unbounded = 0;  

    private const string Sentinel = "-";

    private readonly string _connectionString;
    private readonly DatabaseSourceOptions _sourceOptions;
    private readonly ITextSanitizer _sanitizer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SqlWebReviewExtractor> _logger;

    public SqlWebReviewExtractor(
        IOptions<OltpConnectionOptions> connectionOptions,
        IOptions<DatabaseSourceOptions> sourceOptions,
        ITextSanitizer sanitizer,
        TimeProvider timeProvider,
        ILogger<SqlWebReviewExtractor> logger)
    {
        _connectionString = connectionOptions.Value.ConnectionString;
        _sourceOptions = sourceOptions.Value;
        _sanitizer = sanitizer;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string SourceName => "CustomerReviewSystemData.dbo.Reseñas";

    public async IAsyncEnumerable<Result<StagingResenaWeb>> ExtractAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            yield return Result<StagingResenaWeb>.Failure(new SourceUnavailableError(
                SourceName, "no OLTP connection string is configured"));
            yield break;
        }

        var loadedAt = _timeProvider.GetUtcNow().UtcDateTime;
        var stopwatch = Stopwatch.StartNew();
        var emitted = 0L;

        SqlConnection? connection = null;
        SqlCommand? command = null;
        SqlDataReader? reader = null;
        string? openFailure = null;

        try
        {
            connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            command = connection.CreateCommand();
            command.CommandText = WebReviewQuery.Sql;
            command.CommandTimeout = _sourceOptions.CommandTimeoutSeconds;

         
            reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        }
        catch (SqlException ex)
        {
            
            openFailure = ex.Message;
        }

        if (openFailure is not null)
        {
            if (reader is not null) await reader.DisposeAsync();
            if (command is not null) await command.DisposeAsync();
            if (connection is not null) await connection.DisposeAsync();

            yield return Result<StagingResenaWeb>.Failure(new SourceUnavailableError(SourceName, openFailure));
            yield break;
        }

        await using (connection)
        await using (command)
        await using (reader)
        {
            var fields = new RecordFieldSanitizer(_sanitizer, SourceName);
            var rowNumber = 0L;

            while (await reader!.ReadAsync(cancellationToken))
            {
                rowNumber++;
                fields.BeginRecord(rowNumber);

                var idReview = reader.GetInt32(WebReviewQuery.IdReview);
                var idCliente = reader.GetInt32(WebReviewQuery.IdCliente);
                var idProducto = reader.GetInt32(WebReviewQuery.IdProducto);
                var rating = reader.GetInt32(WebReviewQuery.Rating);
                var comentarios = reader.GetString(WebReviewQuery.Comentarios);
                var fechaCarga = reader.IsDBNull(WebReviewQuery.FechaCarga)
                    ? (DateTime?)null
                    : reader.GetDateTime(WebReviewQuery.FechaCarga);

                var entity = Project(idReview, idCliente, idProducto, rating, comentarios, fechaCarga, loadedAt, fields);

                foreach (var truncation in fields.Truncations)
                {
                    yield return Result<StagingResenaWeb>.Failure(truncation);
                }

                emitted++;
                yield return Result<StagingResenaWeb>.Success(entity);
            }
        }

        stopwatch.Stop();
        var rowsPerSecond = stopwatch.Elapsed.TotalSeconds > 0 ? emitted / stopwatch.Elapsed.TotalSeconds : emitted;
        _logger.LogInformation(
            "Source {Source} streamed {Rows} rows in {Elapsed} ({RowsPerSecond:F0} rows/s).",
            SourceName, emitted, stopwatch.Elapsed, rowsPerSecond);
    }

  
    internal StagingResenaWeb Project(
        int idReview,
        int idCliente,
        int idProducto,
        int rating,
        string? comentarios,
        DateTime? fechaCargaFuenteWeb,
        DateTime loadedAt,
        RecordFieldSanitizer fields) =>
        new()
        {
            IdResenaRaw = fields.Take(idReview.ToString(CultureInfo.InvariantCulture), IdWidth, "IdReview"),
            IdUsuarioRaw = fields.Take(idCliente.ToString(CultureInfo.InvariantCulture), IdWidth, "IdCliente"),
            IdProductoRaw = fields.Take(idProducto.ToString(CultureInfo.InvariantCulture), IdWidth, "IdProducto"),

           
            FechaPublicacionRaw = fields.Take(
                fechaCargaFuenteWeb?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                IdWidth,
                "FechaPublicacion"),

            EstrellasRaw = fields.Take(rating.ToString(CultureInfo.InvariantCulture), StarsWidth, "Rating"),

          
            TituloResenaRaw = fields.Take(Sentinel, TitleWidth, "TituloResena"),

            CuerpoResenaRaw = fields.Take(comentarios, Unbounded, "Comentarios"),
            FechaCargaMeta = loadedAt,
        };
}
