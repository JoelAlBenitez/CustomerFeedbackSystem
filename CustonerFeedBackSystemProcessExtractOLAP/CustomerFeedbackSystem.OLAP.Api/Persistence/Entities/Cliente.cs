namespace CustomerFeedbackSystem.OLAP.Api.Persistence.Entities;

public sealed class Cliente
{
    public int IdCliente { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
