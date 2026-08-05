namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

// One row per (canal, detalle): the three channels of the ETL, and inside the social channel
// one row per platform, so indicators 6.1-6.3 compare channels without losing the platform.
public sealed class DimFuente
{
    public const string CanalEncuestas = "Encuestas";
    public const string CanalResenasWeb = "Reseñas Web";
    public const string CanalRedesSociales = "Redes Sociales";

    public const string TipoCargaCsv = "CSV";
    public const string TipoCargaBaseDatos = "BD";
    public const string TipoCargaApi = "API";

    public required int SkFuente { get; init; }
    public required string Canal { get; init; }
    public required string FuenteDetalle { get; init; }
    public required string TipoCarga { get; init; }
}
