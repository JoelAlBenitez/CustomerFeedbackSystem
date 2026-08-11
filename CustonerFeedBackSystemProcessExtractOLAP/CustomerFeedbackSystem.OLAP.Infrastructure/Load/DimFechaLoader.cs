using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Dimensions;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class DimFechaLoader : IDimensionLoader
{
    private static readonly string[] Columns =
        ["SkFecha", "FechaCompleta", "Dia", "Mes", "Anio", "Trimestre"];

    private readonly SqlWarehouseLoadSession _session;
    private readonly DimensionLoadOptions _options;
    private readonly ILogger<DimFechaLoader> _logger;

    public DimFechaLoader(
        SqlWarehouseLoadSession session,
        IOptions<DimensionLoadOptions> options,
        ILogger<DimFechaLoader> logger)
    {
        _session = session;
        _options = options.Value;
        _logger = logger;
    }

    public string DimensionName => "DimFecha";

    public string TableName => WarehouseTables.DimFecha;

    public int Order => 1;

    public async Task<Result<DimensionLoadStats>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var stats = new DimensionLoadStats(DimensionName, TableName);

        var (minDate, maxDate) = await ReadObservedRangeAsync(stats, cancellationToken);

        var rows = new List<DimFecha> { BuildUnknownMember() };

        if (minDate is not null && maxDate is not null)
        {
            var from = new DateTime(minDate.Value.Year, 1, 1);
            var to = new DateTime(maxDate.Value.Year + Math.Max(0, _options.DateDimensionPaddingYears), 12, 31);

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                rows.Add(BuildForDate(date));
            }

            stats.Note = $"{from:yyyy-MM-dd} … {to:yyyy-MM-dd}";
        }
        else
        {
            stats.Note = "sin fechas legibles en staging: solo la fila centinela";
            _logger.LogWarning(
                "No parseable date was found in staging. DimFecha holds only the sentinel row {Key}.",
                DimensionSentinels.UnknownDateKey);
        }

        await SqlWarehouseWriter.WriteAsync(
            _session, TableName, Columns, rows,
            r => [r.SkFecha, r.FechaCompleta, r.Dia, r.Mes, r.Anio, r.Trimestre],
            _options.CommandTimeoutSeconds, cancellationToken);

        stats.RecordWritten(rows.Count);
        return Result<DimensionLoadStats>.Success(stats);
    }

    private static DimFecha BuildUnknownMember() => new()
    {
        SkFecha = DimensionSentinels.UnknownDateKey,
        FechaCompleta = DimensionSentinels.UnknownDate,
        Dia = DimensionSentinels.UnknownDate.Day,
        Mes = DimensionSentinels.UnknownDate.Month,
        Anio = DimensionSentinels.UnknownDate.Year,
        Trimestre = 1,
    };

    private static DimFecha BuildForDate(DateTime date) => new()
    {
        SkFecha = RawValueParsing.ToDateKey(date),
        FechaCompleta = date.Date,
        Dia = date.Day,
        Mes = date.Month,
        Anio = date.Year,
        Trimestre = ((date.Month - 1) / 3) + 1,
    };

    private async Task<(DateTime? Min, DateTime? Max)> ReadObservedRangeAsync(
        DimensionLoadStats stats,
        CancellationToken cancellationToken)
    {
        DateTime? min = null;
        DateTime? max = null;

        await using var command = _session.Connection.CreateCommand();
        command.Transaction = _session.Transaction;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.CommandText = StagingQueries.DistinctDates;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            stats.RecordRead();

            var raw = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!RawValueParsing.TryParseDate(raw, out var date))
            {
                stats.RecordDiscarded();
                continue;
            }

            if (min is null || date < min)
            {
                min = date;
            }

            if (max is null || date > max)
            {
                max = date;
            }
        }

        return (min, max);
    }
}
