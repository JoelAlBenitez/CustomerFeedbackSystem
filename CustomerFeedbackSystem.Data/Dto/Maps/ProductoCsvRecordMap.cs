using CsvHelper.Configuration;

namespace CustomerFeedbackSystem.Data.Dto.Maps;

public sealed class ProductoCsvRecordMap : ClassMap<ProductoCsvRecord>
{
    public ProductoCsvRecordMap()
    {
        Map(m => m.IdProducto).Name("IdProducto");
        Map(m => m.Nombre).Name("Nombre");
        Map(m => m.Categoria).Name("Categoría");
    }
}
