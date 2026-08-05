namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

public sealed class DimFecha
{
    public required int SkFecha { get; init; }
    public required DateTime FechaCompleta { get; init; }
    public required int Dia { get; init; }
    public required int Mes { get; init; }
    public required int Anio { get; init; }
    public required int Trimestre { get; init; }
}
