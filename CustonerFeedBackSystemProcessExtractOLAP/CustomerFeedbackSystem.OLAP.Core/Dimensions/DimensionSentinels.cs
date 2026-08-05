namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

public static class DimensionSentinels
{
    public const string RawSentinel = "-";

    public const string UnknownAttribute = "No disponible";

    public const string UnknownMember = "Desconocido";

    public const string AnonymousMember = "Anónimo";

    public const string UncategorizedProduct = "Sin categoria";

    public const string UnclassifiedLabel = "Sin Clasificar";

    public const string ClasificacionPositiva = "Positiva";

    public const string ClasificacionNegativa = "Negativa";

    public const string ClasificacionNeutra = "Neutra";

    public const string CanalEncuestas = "Encuestas";

    public const string CanalResenasWeb = "Reseñas Web";

    public const string CanalRedesSociales = "Redes Sociales";

    public const string TipoCargaCsv = "CSV";

    public const string TipoCargaBaseDatos = "BD";

    public const string TipoCargaApi = "API";

    public const int UnknownDateKey = 19000101;

    public const int PalabraMaxLength = 100;

    public static readonly DateTime UnknownDate = new(1900, 1, 1);

    public static bool IsMissing(string? value) =>
        string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), RawSentinel, StringComparison.Ordinal);
}
