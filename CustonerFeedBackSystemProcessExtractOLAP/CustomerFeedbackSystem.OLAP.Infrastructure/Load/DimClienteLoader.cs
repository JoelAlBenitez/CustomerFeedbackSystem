using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Dimensions;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class DimClienteLoader : IDimensionLoader
{
    public const int UnknownKey = 1;
    public const int AnonymousKey = 2;

    private const int NombreWidth = 100;

    private static readonly string[] Columns =
        ["SkCliente", "NkCliente", "Nombre", "Pais", "RangoEdad", "TipoCliente", "EsAnonimo"];

    private readonly SqlWarehouseLoadSession _session;
    private readonly string _oltpConnectionString;
    private readonly DimensionLoadOptions _options;
    private readonly ILogger<DimClienteLoader> _logger;

    public DimClienteLoader(
        SqlWarehouseLoadSession session,
        IOptions<OltpConnectionOptions> oltpConnection,
        IOptions<DimensionLoadOptions> options,
        ILogger<DimClienteLoader> logger)
    {
        _session = session;
        _oltpConnectionString = oltpConnection.Value.ConnectionString;
        _options = options.Value;
        _logger = logger;
    }

    public string DimensionName => "DimCliente";

    public string TableName => WarehouseTables.DimCliente;

    public int Order => 5;

    public async Task<Result<DimensionLoadStats>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_oltpConnectionString))
        {
            return Result<DimensionLoadStats>.Failure(new PreconditionError(
                "OLTP connection string",
                $"'{OltpConnectionOptions.ConnectionStringName}' is not configured, and DimCliente "
                + "takes its names from dbo.Clientes"));
        }

        var stats = new DimensionLoadStats(DimensionName, TableName);

        var rows = new List<DimCliente>
        {
            BuildSpecialMember(UnknownKey, DimensionSentinels.UnknownMember),
            BuildSpecialMember(AnonymousKey, DimensionSentinels.AnonymousMember),
        };

        var nextKey = AnonymousKey + 1;
        var collapsed = 0;

        await using (var oltp = new SqlConnection(_oltpConnectionString))
        {
            await oltp.OpenAsync(cancellationToken);

            await using var command = oltp.CreateCommand();
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.CommandText = OltpDimensionQueries.Clientes;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                stats.RecordRead();

                var nombre = reader.GetString(1);

                if (DimensionSentinels.IsMissing(nombre))
                {
                    stats.RecordDiscarded();
                    collapsed++;
                    continue;
                }

                rows.Add(new DimCliente
                {
                    SkCliente = nextKey++,
                    NkCliente = reader.GetInt32(0),
                    Nombre = RawValueParsing.Truncate(nombre, NombreWidth),
                    Pais = DimensionSentinels.UnknownAttribute,
                    RangoEdad = DimensionSentinels.UnknownAttribute,
                    TipoCliente = DimensionSentinels.UnknownAttribute,
                    EsAnonimo = false,
                });
            }
        }

        if (collapsed > 0)
        {
            _logger.LogInformation(
                "{Count} OLTP sentinel customer(s) collapsed into the '{Member}' member.",
                collapsed, DimensionSentinels.AnonymousMember);
        }

        await SqlWarehouseWriter.WriteAsync(
            _session, TableName, Columns, rows,
            r => [r.SkCliente, r.NkCliente, r.Nombre, r.Pais, r.RangoEdad, r.TipoCliente, r.EsAnonimo],
            _options.CommandTimeoutSeconds, cancellationToken);

        await SqlWarehouseWriter.ReseedAsync(
            _session, TableName, rows.Count, _options.CommandTimeoutSeconds, cancellationToken);

        stats.RecordWritten(rows.Count);
        stats.Note = $"Pais/RangoEdad/TipoCliente = '{DimensionSentinels.UnknownAttribute}' (sin origen)";

        return Result<DimensionLoadStats>.Success(stats);
    }

    private static DimCliente BuildSpecialMember(int skCliente, string nombre) => new()
    {
        SkCliente = skCliente,
        NkCliente = null,
        Nombre = nombre,
        Pais = DimensionSentinels.UnknownAttribute,
        RangoEdad = DimensionSentinels.UnknownAttribute,
        TipoCliente = DimensionSentinels.UnknownAttribute,
        EsAnonimo = true,
    };
}
