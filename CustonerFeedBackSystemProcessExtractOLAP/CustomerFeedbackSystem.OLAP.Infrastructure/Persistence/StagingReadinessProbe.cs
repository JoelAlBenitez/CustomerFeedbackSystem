using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Load;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;

// The load reads from staging, so the E phase has to have run. Failing here with a sentence the
// person can act on beats failing later with an empty DimFecha and no explanation.
public sealed class StagingReadinessProbe
{
    private readonly string _connectionString;
    private readonly ILogger<StagingReadinessProbe> _logger;

    public StagingReadinessProbe(
        IOptions<OlapConnectionOptions> connectionOptions,
        ILogger<StagingReadinessProbe> logger)
    {
        _connectionString = connectionOptions.Value.ConnectionString;
        _logger = logger;
    }

    public async Task<Result> EnsurePopulatedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = StagingQueries.RowCounts;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return Result.Success();
            }

            var encuestas = reader.GetInt32(0);
            var resenas = reader.GetInt32(1);
            var sociales = reader.GetInt32(2);

            if (encuestas + resenas + sociales == 0)
            {
                return Result.Failure(new PreconditionError(
                    "populated staging",
                    "the three Staging.* tables are empty. Run the extraction phase first: "
                    + "dotnet run --project CustomerFeedbackSystem.OLAP.Worker -- --phase Extract"));
            }

            _logger.LogInformation(
                "Staging ready: {Encuestas} encuestas, {Resenas} reseñas, {Sociales} comentarios sociales.",
                encuestas, resenas, sociales);

            return Result.Success();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Could not read staging; SQL error {Number}.", ex.Number);
            return Result.Failure(new PreconditionError("staging access", ex.Message));
        }
    }
}
