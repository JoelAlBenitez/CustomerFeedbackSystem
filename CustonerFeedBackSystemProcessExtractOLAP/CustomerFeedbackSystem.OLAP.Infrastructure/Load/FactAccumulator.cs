using CustomerFeedbackSystem.OLAP.Core.Facts;
using CustomerFeedbackSystem.OLAP.Core.Reporting;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

internal sealed class FactAccumulator
{
    public List<HechoOpinion> Facts { get; } = [];

    public List<HechoOpinionPalabra> Bridge { get; } = [];

    public List<ChannelFactStats> Channels { get; } = [];

    public FactLoadStats FactStats { get; } =
        new("HechoOpiniones", WarehouseTables.HechoOpiniones);

    public FactLoadStats BridgeStats { get; } =
        new("HechoOpinionPalabra", WarehouseTables.HechoOpinionPalabra);

    public KeyResolutionStats Resolution { get; } = new();

    public ClassifierAgreement Agreement { get; } = new();

    public long NextFactId { get; set; } = 1;
}
