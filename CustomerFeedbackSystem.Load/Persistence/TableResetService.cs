using Microsoft.Data.SqlClient;

namespace CustomerFeedbackSystem.Load.Persistence;

public sealed class TableResetService
{
    private static readonly string[] TablesInDeleteOrder =
    [
        "ComentariosSociales",
        "Encuestas",
        "Reseñas",
        "FuentesDatos",
        "Comentarios",
        "Productos",
        "Clientes",
        "Categorias",
        "Clasificaciones",
        "FuenteEncuestas",
        "FuentesSociales",
        "TipoFuentesDatos",
    ];

    public async Task ResetAllAsync(SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        foreach (var table in TablesInDeleteOrder)
        {
            await using var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = $"DELETE FROM dbo.[{table}]";
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

            await using var reseedCommand = connection.CreateCommand();
            reseedCommand.Transaction = transaction;
            reseedCommand.CommandText = $"DBCC CHECKIDENT ('dbo.{table}', RESEED, 0)";
            await reseedCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
