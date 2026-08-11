using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Dimensions;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class DimProductoLoader : IDimensionLoader
{
    public const int UnknownKey = 1;

    private const int NombreWidth = 100;
    private const int CategoriaWidth = 50;

    private static readonly string[] Columns = ["SkProducto", "NkProducto", "Nombre", "Categoria"];

    private readonly SqlWarehouseLoadSession _session;
    private readonly string _oltpConnectionString;
    private readonly DimensionLoadOptions _options;

    public DimProductoLoader(
        SqlWarehouseLoadSession session,
        IOptions<OltpConnectionOptions> oltpConnection,
        IOptions<DimensionLoadOptions> options)
    {
        _session = session;
        _oltpConnectionString = oltpConnection.Value.ConnectionString;
        _options = options.Value;
    }

    public string DimensionName => "DimProducto";

    public string TableName => WarehouseTables.DimProducto;

    public int Order => 4;

    public async Task<Result<DimensionLoadStats>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_oltpConnectionString))
        {
            return Result<DimensionLoadStats>.Failure(new PreconditionError(
                "OLTP connection string",
                $"'{OltpConnectionOptions.ConnectionStringName}' is not configured, and DimProducto "
                + "takes its names and categories from dbo.Productos"));
        }

        var stats = new DimensionLoadStats(DimensionName, TableName);

        var rows = new List<DimProducto>
        {
            new()
            {
                SkProducto = UnknownKey,
                NkProducto = null,
                Nombre = DimensionSentinels.UnknownMember,
                Categoria = DimensionSentinels.UncategorizedProduct,
            },
        };

        var nextKey = UnknownKey + 1;

        await using (var oltp = new SqlConnection(_oltpConnectionString))
        {
            await oltp.OpenAsync(cancellationToken);

            await using var command = oltp.CreateCommand();
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.CommandText = OltpDimensionQueries.Productos;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                stats.RecordRead();

                rows.Add(new DimProducto
                {
                    SkProducto = nextKey++,
                    NkProducto = reader.GetInt32(0),
                    Nombre = RawValueParsing.Truncate(reader.GetString(1), NombreWidth),
                    Categoria = RawValueParsing.Truncate(reader.GetString(2), CategoriaWidth),
                });
            }
        }

        await SqlWarehouseWriter.WriteAsync(
            _session, TableName, Columns, rows,
            r => [r.SkProducto, r.NkProducto, r.Nombre, r.Categoria],
            _options.CommandTimeoutSeconds, cancellationToken);

        await SqlWarehouseWriter.ReseedAsync(
            _session, TableName, rows.Count, _options.CommandTimeoutSeconds, cancellationToken);

        stats.RecordWritten(rows.Count);
        stats.Note = $"catálogo completo + miembro '{DimensionSentinels.UnknownMember}'";

        return Result<DimensionLoadStats>.Success(stats);
    }
}
