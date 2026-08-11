using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Load;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;

public sealed class DimensionReadinessProbe
{
    private static readonly string[] Names =
        ["DimFecha", "DimCliente", "DimProducto", "DimFuente", "DimClasificacion", "PalabraClave"];

    private readonly string _connectionString;
    private readonly ILogger<DimensionReadinessProbe> _logger;

    public DimensionReadinessProbe(
        IOptions<OlapConnectionOptions> connectionOptions,
        ILogger<DimensionReadinessProbe> logger)
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
            command.CommandText = WarehouseKeyQueries.RowCounts;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return Result.Success();
            }

            var counts = new int[Names.Length];
            for (var i = 0; i < Names.Length; i++)
            {
                counts[i] = reader.GetInt32(i);
            }

            var empty = Names.Where((_, i) => counts[i] == 0).ToList();
            if (empty.Count > 0)
            {
                return Result.Failure(new PreconditionError(
                    "populated dimensions",
                    $"{string.Join(", ", empty)} has no row. Run the dimension load first: "
                    + "dotnet run --project CustomerFeedbackSystem.OLAP.Worker -- --phase Load"));
            }

            _logger.LogInformation(
                "Dimensions ready: {Fechas} fechas, {Clientes} clientes, {Productos} productos, "
                + "{Fuentes} fuentes, {Clasificaciones} clasificaciones, {Palabras} palabras clave.",
                counts[0], counts[1], counts[2], counts[3], counts[4], counts[5]);

            return Result.Success();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Could not read the dimensions; SQL error {Number}.", ex.Number);
            return Result.Failure(new PreconditionError("dimension access", ex.Message));
        }
    }
}
