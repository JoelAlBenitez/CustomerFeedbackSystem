namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed class ClassifierAgreement
{
    public int Comparadas { get; private set; }

    public int Coincidencias { get; private set; }

    public double Porcentaje => Comparadas == 0 ? 0d : Coincidencias * 100d / Comparadas;

    public void Record(bool coincide)
    {
        Comparadas++;

        if (coincide)
        {
            Coincidencias++;
        }
    }
}
