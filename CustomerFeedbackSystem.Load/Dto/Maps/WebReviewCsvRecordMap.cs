using CsvHelper.Configuration;

namespace CustomerFeedbackSystem.Load.Dto.Maps;

public sealed class WebReviewCsvRecordMap : ClassMap<WebReviewCsvRecord>
{
    public WebReviewCsvRecordMap()
    {
        Map(m => m.IdReview).Name("IdReview");
        Map(m => m.IdCliente).Name("IdCliente");
        Map(m => m.IdProducto).Name("IdProducto");
        Map(m => m.Fecha).Name("Fecha");
        Map(m => m.Comentario).Name("Comentario");
        Map(m => m.Rating).Name("Rating");
    }
}
