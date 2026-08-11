namespace CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;

public static class SpanishSentimentLexicon
{
    public const int DecisionThreshold = 2;

    private const int NegationWindow = 3;

    private static readonly string[] PositivoFuerte =
    [
        "excelente", "excelentes", "perfecto", "perfecta", "perfectos", "perfectas",
        "encanta", "encantan", "encanto", "encantado", "encantada",
        "maravilloso", "maravillosa", "genial", "gran", "grandioso", "grandiosa",
        "superior", "satisfecho", "satisfecha", "satisfechos", "satisfechas",
        "recomiendo", "recomiendan", "recomendable", "recomendado", "recomendada",
        "fantastico", "fantastica", "optimo", "optima", "impecable", "inmejorable",
    ];

    private static readonly string[] PositivoLeve =
    [
        "bueno", "buena", "buenos", "buenas", "bien",
        "contento", "contenta", "contentos", "contentas",
        "rapido", "rapida", "correcto", "correcta",
        "cumple", "cumplio", "funciona", "funciono", "satisface",
        "agradable", "util", "comodo", "comoda", "barato", "barata", "positivo", "positiva",
    ];

    private static readonly string[] NegativoFuerte =
    [
        "pesimo", "pesima", "pesimos", "pesimas",
        "malo", "mala", "malos", "malas",
        "horrible", "terrible", "defectuoso", "defectuosa",
        "danado", "danada", "decepcionado", "decepcionada", "decepcion", "decepcionante",
        "insatisfecho", "insatisfecha", "inaceptable", "inservible", "estafa",
    ];

    private static readonly string[] NegativoLeve =
    [
        "rompio", "rompe", "roto", "rota", "romper",
        "tardio", "tardia", "tarde", "lento", "lenta", "caro", "cara",
        "falla", "fallo", "fallas", "problema", "problemas",
        "dificil", "incomodo", "incomoda", "negativo", "negativa", "mediocre",
    ];

    private static readonly HashSet<string> Negaciones =
        new(["no", "nada", "nunca", "jamas", "ni", "sin", "tampoco"], StringComparer.Ordinal);

    private static readonly Dictionary<string, int> Pesos = BuildWeights();

    public static int Score(IReadOnlyList<string> tokens)
    {
        var total = 0;
        var negatedUntil = -1;

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            if (Negaciones.Contains(token))
            {
                negatedUntil = index + NegationWindow;
                continue;
            }

            if (!Pesos.TryGetValue(token, out var peso))
            {
                continue;
            }

            total += index <= negatedUntil ? -Math.Sign(peso) : peso;
        }

        return total;
    }

    private static Dictionary<string, int> BuildWeights()
    {
        var pesos = new Dictionary<string, int>(StringComparer.Ordinal);

        Register(pesos, PositivoFuerte, 2);
        Register(pesos, PositivoLeve, 1);
        Register(pesos, NegativoFuerte, -2);
        Register(pesos, NegativoLeve, -1);

        return pesos;
    }

    private static void Register(Dictionary<string, int> pesos, string[] palabras, int peso)
    {
        foreach (var palabra in palabras)
        {
            pesos[palabra] = peso;
        }
    }
}
