namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

public sealed class DimCliente
{
    public required int SkCliente { get; init; }

    public int? NkCliente { get; init; }

    public required string Nombre { get; init; }

    public required string Pais { get; init; }
    public required string RangoEdad { get; init; }
    public required string TipoCliente { get; init; }

    public required bool EsAnonimo { get; init; }

   
    public static DimCliente SpecialMember(int skCliente, string nombre) => new()
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
