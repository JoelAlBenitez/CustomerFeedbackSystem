using CustomerFeedbackSystem.OLAP.Core.Facts;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class WarehouseKeyMaps
{
    public required IReadOnlyDictionary<int, int> Clientes { get; init; }

    public required IReadOnlyDictionary<int, int> Productos { get; init; }

    public required IReadOnlySet<int> Fechas { get; init; }

    public required IReadOnlyDictionary<string, int> Palabras { get; init; }

    public required IReadOnlyDictionary<SentimentPolarity, int> Clasificaciones { get; init; }

    public required IReadOnlyList<FuenteMember> Fuentes { get; init; }

    public bool TryResolveFuente(string canal, string detalle, out int skFuente)
    {
        foreach (var fuente in Fuentes)
        {
            if (Matches(fuente.Canal, canal) && Matches(fuente.FuenteDetalle, detalle))
            {
                skFuente = fuente.SkFuente;
                return true;
            }
        }

        foreach (var fuente in Fuentes)
        {
            if (Matches(fuente.Canal, canal))
            {
                skFuente = fuente.SkFuente;
                return true;
            }
        }

        skFuente = 0;
        return false;
    }

    private static bool Matches(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
