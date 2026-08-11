using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Dimensions;
using CustomerFeedbackSystem.OLAP.Core.Facts;
using CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

internal static class WarehouseKeyMapReader
{
    public static async Task<Result<WarehouseKeyMaps>> ReadAsync(
        SqlWarehouseLoadSession session,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var clientes = await ReadPairsAsync(session, WarehouseKeyQueries.Clientes, timeoutSeconds, cancellationToken);
        var productos = await ReadPairsAsync(session, WarehouseKeyQueries.Productos, timeoutSeconds, cancellationToken);
        var fechas = await ReadKeysAsync(session, WarehouseKeyQueries.Fechas, timeoutSeconds, cancellationToken);
        var palabras = await ReadPalabrasAsync(session, timeoutSeconds, cancellationToken);
        var fuentes = await ReadFuentesAsync(session, timeoutSeconds, cancellationToken);
        var clasificaciones = await ReadClasificacionesAsync(session, timeoutSeconds, cancellationToken);

        foreach (var polarity in new[]
                 {
                     SentimentPolarity.Positiva,
                     SentimentPolarity.Neutra,
                     SentimentPolarity.Negativa,
                     SentimentPolarity.SinClasificar,
                 })
        {
            if (!clasificaciones.ContainsKey(polarity))
            {
                return Result<WarehouseKeyMaps>.Failure(new PreconditionError(
                    "Dimenciones.DimClasificacion",
                    $"the '{polarity}' member is missing. Reload the dimensions before loading the facts"));
            }
        }

        if (fuentes.Count == 0)
        {
            return Result<WarehouseKeyMaps>.Failure(new PreconditionError(
                "Dimenciones.DimFuente",
                "no source member exists. Reload the dimensions before loading the facts"));
        }

        return Result<WarehouseKeyMaps>.Success(new WarehouseKeyMaps
        {
            Clientes = clientes,
            Productos = productos,
            Fechas = fechas,
            Palabras = palabras,
            Fuentes = fuentes,
            Clasificaciones = clasificaciones,
        });
    }

    private static async Task<Dictionary<int, int>> ReadPairsAsync(
        SqlWarehouseLoadSession session,
        string sql,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, int>();

        await using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandTimeout = timeoutSeconds;
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            map[reader.GetInt32(0)] = reader.GetInt32(1);
        }

        return map;
    }

    private static async Task<HashSet<int>> ReadKeysAsync(
        SqlWarehouseLoadSession session,
        string sql,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var keys = new HashSet<int>();

        await using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandTimeout = timeoutSeconds;
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            keys.Add(reader.GetInt32(0));
        }

        return keys;
    }

    private static async Task<Dictionary<string, int>> ReadPalabrasAsync(
        SqlWarehouseLoadSession session,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);

        await using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandTimeout = timeoutSeconds;
        command.CommandText = WarehouseKeyQueries.Palabras;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            map[reader.GetString(1)] = reader.GetInt32(0);
        }

        return map;
    }

    private static async Task<List<FuenteMember>> ReadFuentesAsync(
        SqlWarehouseLoadSession session,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var fuentes = new List<FuenteMember>();

        await using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandTimeout = timeoutSeconds;
        command.CommandText = WarehouseKeyQueries.Fuentes;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            fuentes.Add(new FuenteMember(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        }

        return fuentes;
    }

    private static async Task<Dictionary<SentimentPolarity, int>> ReadClasificacionesAsync(
        SqlWarehouseLoadSession session,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<SentimentPolarity, int>();

        await using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandTimeout = timeoutSeconds;
        command.CommandText = WarehouseKeyQueries.Clasificaciones;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var polarity = ToPolarity(reader.GetString(1));
            if (polarity is not null)
            {
                map[polarity.Value] = reader.GetInt32(0);
            }
        }

        return map;
    }

    public static SentimentPolarity? ToPolarity(string clasificacion)
    {
        var folded = TextNormalization.Fold(clasificacion.Trim());

        if (folded == TextNormalization.Fold(DimensionSentinels.ClasificacionPositiva))
        {
            return SentimentPolarity.Positiva;
        }

        if (folded == TextNormalization.Fold(DimensionSentinels.ClasificacionNeutra))
        {
            return SentimentPolarity.Neutra;
        }

        if (folded == TextNormalization.Fold(DimensionSentinels.ClasificacionNegativa))
        {
            return SentimentPolarity.Negativa;
        }

        if (folded == TextNormalization.Fold(DimensionSentinels.UnclassifiedLabel))
        {
            return SentimentPolarity.SinClasificar;
        }

        return null;
    }
}
