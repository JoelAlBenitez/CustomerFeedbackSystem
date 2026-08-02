using CsvHelper.Configuration;

namespace CustomerFeedbackSystem.Data.Dto.Maps;

public sealed class SocialCommentCsvRecordMap : ClassMap<SocialCommentCsvRecord>
{
    public SocialCommentCsvRecordMap()
    {
        Map(m => m.IdComment).Name("IdComment");
        Map(m => m.IdCliente).Name("IdCliente");
        Map(m => m.IdProducto).Name("IdProducto");
        Map(m => m.Fuente).Name("Fuente");
        Map(m => m.Fecha).Name("Fecha");
        Map(m => m.Comentario).Name("Comentario");
    }
}
