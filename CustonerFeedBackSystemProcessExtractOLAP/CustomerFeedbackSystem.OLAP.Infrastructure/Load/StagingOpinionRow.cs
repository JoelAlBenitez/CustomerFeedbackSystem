namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

internal sealed class StagingOpinionRow
{
    public required string? FuenteDetalleRaw { get; init; }

    public required string? ClienteRaw { get; init; }

    public required string? ProductoRaw { get; init; }

    public required string? FechaRaw { get; init; }

    public required string? PuntajeRaw { get; init; }

    public required string? TextoRaw { get; init; }

    public required string? EtiquetaOrigenRaw { get; init; }
}
