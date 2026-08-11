namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed class ChannelFactStats
{
    public ChannelFactStats(string canal)
    {
        Canal = canal;
    }

    public string Canal { get; }

    public int Total { get; private set; }

    public int Positivas { get; private set; }

    public int Neutras { get; private set; }

    public int Negativas { get; private set; }

    public int SinClasificar { get; private set; }

    public int ConPuntaje { get; private set; }

    public void RecordPositiva() => Positivas++;

    public void RecordNeutra() => Neutras++;

    public void RecordNegativa() => Negativas++;

    public void RecordSinClasificar() => SinClasificar++;

    public void RecordOpinion(bool tienePuntaje)
    {
        Total++;

        if (tienePuntaje)
        {
            ConPuntaje++;
        }
    }
}
