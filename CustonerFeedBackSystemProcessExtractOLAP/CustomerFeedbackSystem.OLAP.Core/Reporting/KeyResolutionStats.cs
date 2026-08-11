namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed class KeyResolutionStats
{
    public int ClientesResueltos { get; private set; }

    public int ClientesDesconocidos { get; private set; }

    public int ClientesAnonimos { get; private set; }

    public int ProductosResueltos { get; private set; }

    public int ProductosDesconocidos { get; private set; }

    public int ProductosSinOrigen { get; private set; }

    public int FechasResueltas { get; private set; }

    public int FechasSinResolver { get; private set; }

    public int PuntajesValidos { get; private set; }

    public int PuntajesAusentes { get; private set; }

    public void RecordCliente(bool resuelto, bool anonimo)
    {
        if (anonimo)
        {
            ClientesAnonimos++;
        }
        else if (resuelto)
        {
            ClientesResueltos++;
        }
        else
        {
            ClientesDesconocidos++;
        }
    }

    public void RecordProducto(bool resuelto, bool sinOrigen)
    {
        if (sinOrigen)
        {
            ProductosSinOrigen++;
        }
        else if (resuelto)
        {
            ProductosResueltos++;
        }
        else
        {
            ProductosDesconocidos++;
        }
    }

    public void RecordFecha(bool resuelta)
    {
        if (resuelta)
        {
            FechasResueltas++;
        }
        else
        {
            FechasSinResolver++;
        }
    }

    public void RecordPuntaje(bool valido)
    {
        if (valido)
        {
            PuntajesValidos++;
        }
        else
        {
            PuntajesAusentes++;
        }
    }
}
