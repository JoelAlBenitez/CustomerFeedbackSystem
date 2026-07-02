using CsvHelper.Configuration;

namespace CustomerFeedbackSystem.Data.Dto.Maps;

public sealed class SurveyCsvRecordMap : ClassMap<SurveyCsvRecord>
{
    public SurveyCsvRecordMap()
    {
        Map(m => m.IdOpinion).Name("IdOpinion");
        Map(m => m.IdCliente).Name("IdCliente");
        Map(m => m.IdProducto).Name("IdProducto");
        Map(m => m.Fecha).Name("Fecha");
        Map(m => m.Comentario).Name("Comentario");
        Map(m => m.Clasificacion).Name("Clasificación");
        Map(m => m.PuntajeSatisfaccion).Name("PuntajeSatisfacción");
        Map(m => m.Fuente).Name("Fuente");
    }
}
