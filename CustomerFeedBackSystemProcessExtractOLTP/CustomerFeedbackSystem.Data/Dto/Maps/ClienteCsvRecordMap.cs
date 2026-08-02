using CsvHelper.Configuration;

namespace CustomerFeedbackSystem.Data.Dto.Maps;

public sealed class ClienteCsvRecordMap : ClassMap<ClienteCsvRecord>
{
    public ClienteCsvRecordMap()
    {
        Map(m => m.IdCliente).Name("IdCliente");
        Map(m => m.Nombre).Name("Nombre");
        Map(m => m.Email).Name("Email");
    }
}
