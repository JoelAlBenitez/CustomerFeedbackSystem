namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

public sealed class DimFecha
{
    public required int SkFecha { get; init; }
    public required DateTime FechaCompleta { get; init; }
    public required int Dia { get; init; }
    public required int Mes { get; init; }
    public required int Anio { get; init; }
    public required int Trimestre { get; init; }

    public static DimFecha ForDate(DateTime date) => new()
    {
        SkFecha = (date.Year * 10_000) + (date.Month * 100) + date.Day,
        FechaCompleta = date.Date,
        Dia = date.Day,
        Mes = date.Month,
        Anio = date.Year,
        Trimestre = ((date.Month - 1) / 3) + 1,
    };
}
