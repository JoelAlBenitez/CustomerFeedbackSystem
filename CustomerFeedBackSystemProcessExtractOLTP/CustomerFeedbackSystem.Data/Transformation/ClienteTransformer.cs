using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Data.Common;
using CustomerFeedbackSystem.Data.Dto;

namespace CustomerFeedbackSystem.Data.Transformation;

public sealed class ClienteTransformer
{
    public const string SentinelEmail = "cliente.desconocido@sistema.local";
    public const string SentinelName = "-";

    public Result<Cliente> Transform(ClienteCsvRecord record, int assignedId)
    {
        var nombre = TextNormalization.Truncate(TextNormalization.OrSentinel(record.Nombre), 50);
        var email = TextNormalization.Truncate(record.Email!.Trim(), 254);

        return Result<Cliente>.Success(new Cliente
        {
            IdCliente = assignedId,
            Nombre = nombre,
            Email = email,
        });
    }

    public static Cliente BuildSentinel(int assignedId) => new()
    {
        IdCliente = assignedId,
        Nombre = SentinelName,
        Email = SentinelEmail,
    };
}
