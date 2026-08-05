namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;


public static class DimensionSentinels
{
    public const string RawSentinel = "-";

  
    public const string UnknownAttribute = "No disponible";

  
    public const string UnknownMember = "Desconocido";

    public const string AnonymousMember = "Anónimo";

    public const string UncategorizedProduct = "Sin categoria";

    public const string UnclassifiedLabel = "Sin Clasificar";

    public const string UnknownLoadType = "No disponible";

    public const int UnknownDateKey = 19000101;

    public static readonly DateTime UnknownDate = new(1900, 1, 1);

    public static bool IsMissing(string? value) =>
        string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), RawSentinel, StringComparison.Ordinal);
}
