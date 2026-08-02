using CsvHelper.Configuration;

namespace CustomerFeedbackSystem.Data.Dto.Maps;

public sealed class FuenteDatoCsvRecordMap : ClassMap<FuenteDatoCsvRecord>
{
    public FuenteDatoCsvRecordMap()
    {
        Map(m => m.IdFuente).Name("IdFuente");
        Map(m => m.TipoFuente).Name("TipoFuente");
        Map(m => m.FechaCarga).Name("FechaCarga");
    }
}
