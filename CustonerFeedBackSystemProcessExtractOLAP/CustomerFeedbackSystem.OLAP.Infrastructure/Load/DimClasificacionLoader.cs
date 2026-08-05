using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Dimensions;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class DimClasificacionLoader : IDimensionLoader
{
    private static readonly string[] Columns =
        ["SkClasificacion", "Clasificacion", "PuntajeBase", "EsPositiva", "EsNegativa", "EsNeutra"];

    private static readonly DimClasificacion[] Members =
    [
        new()
        {
            SkClasificacion = 1,
            Clasificacion = DimensionSentinels.ClasificacionPositiva,
            PuntajeBase = 5,
            EsPositiva = true,
            EsNegativa = false,
            EsNeutra = false,
        },
        new()
        {
            SkClasificacion = 2,
            Clasificacion = DimensionSentinels.ClasificacionNeutra,
            PuntajeBase = 3,
            EsPositiva = false,
            EsNegativa = false,
            EsNeutra = true,
        },
        new()
        {
            SkClasificacion = 3,
            Clasificacion = DimensionSentinels.ClasificacionNegativa,
            PuntajeBase = 1,
            EsPositiva = false,
            EsNegativa = true,
            EsNeutra = false,
        },
        new()
        {
            SkClasificacion = 4,
            Clasificacion = DimensionSentinels.UnclassifiedLabel,
            PuntajeBase = null,
            EsPositiva = false,
            EsNegativa = false,
            EsNeutra = false,
        },
    ];

    private readonly SqlDimensionLoadSession _session;
    private readonly DimensionLoadOptions _options;

    public DimClasificacionLoader(SqlDimensionLoadSession session, IOptions<DimensionLoadOptions> options)
    {
        _session = session;
        _options = options.Value;
    }

    public string DimensionName => "DimClasificacion";

    public string TableName => DimensionTables.DimClasificacion;

    public int Order => 3;

    public async Task<Result<DimensionLoadStats>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var stats = new DimensionLoadStats(DimensionName, TableName);
        stats.RecordRead(Members.Length);

        await SqlDimensionWriter.WriteAsync(
            _session, TableName, Columns, Members,
            r => [r.SkClasificacion, r.Clasificacion, r.PuntajeBase, r.EsPositiva, r.EsNegativa, r.EsNeutra],
            _options.CommandTimeoutSeconds, cancellationToken);

        await SqlDimensionWriter.ReseedAsync(
            _session, TableName, Members.Length, _options.CommandTimeoutSeconds, cancellationToken);

        stats.RecordWritten(Members.Length);
        stats.Note = "dimensión estática";

        return Result<DimensionLoadStats>.Success(stats);
    }
}
