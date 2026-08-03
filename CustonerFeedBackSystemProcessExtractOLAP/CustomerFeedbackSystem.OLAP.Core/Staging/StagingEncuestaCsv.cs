namespace CustomerFeedbackSystem.OLAP.Core.Staging;

public sealed class StagingEncuestaCsv
{
    public required string IdEncuestaRaw { get; init; }          
    public required string IdClienteRaw { get; init; }          
    public required string IdProductoRaw { get; init; }          
    public required string FechaEncuestaRaw { get; init; }       
    public required string NivelSatisfaccionRaw { get; init; }  
    public required string ComentariosRaw { get; init; }         
    public required string ClasificacionRaw { get; init; }       

    public required string FuenteRaw { get; init; }              

    public required string NombreArchivoMeta { get; init; }      
    public required DateTime FechaCargaMeta { get; init; }       
}
