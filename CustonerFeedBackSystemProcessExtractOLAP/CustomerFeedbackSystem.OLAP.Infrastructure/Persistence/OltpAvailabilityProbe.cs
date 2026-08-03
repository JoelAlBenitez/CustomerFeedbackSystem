using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;

public sealed class OltpAvailabilityProbe
{
    private const string CountQuery = """
        SELECT
            (SELECT COUNT(*) FROM dbo.[Reseñas])           AS Resenas,
            (SELECT COUNT(*) FROM dbo.ComentariosSociales) AS Sociales;
        """;

    private readonly string _connectionString;
    private readonly ILogger<OltpAvailabilityProbe> _logger;

    public OltpAvailabilityProbe(
        IOptions<OltpConnectionOptions> connectionOptions,
        ILogger<OltpAvailabilityProbe> logger)
    {
        _connectionString = connectionOptions.Value.ConnectionString;
        _logger = logger;
    }

    public async Task WarnIfOltpEmptyAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogWarning(
                "No OLTP connection string configured. The database and API sources will report as unavailable; "
                + "the CSV source is unaffected.");
            return;
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = CountQuery;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return;
            }

            var resenas = reader.GetInt32(0);
            var sociales = reader.GetInt32(1);

            if (resenas == 0 || sociales == 0)
            {
                _logger.LogWarning(
                    "OLTP looks unpopulated (dbo.Reseñas: {Resenas}, dbo.ComentariosSociales: {Sociales}). "
                    + "Run the OLTP ETL first — two of the three sources read from there.",
                    resenas,
                    sociales);
            }
            else
            {
                _logger.LogInformation(
                    "OLTP is populated: {Resenas} reviews, {Sociales} social comments available to extract.",
                    resenas,
                    sociales);
            }
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(
                "Could not probe the OLTP database ({Reason}). The database and API sources may report as unavailable.",
                ex.Message);
        }
    }
}
