namespace CustomerFeedbackSystem.OLAP.Core.Staging;
public sealed class StagingResenaWeb
{
    public required string IdResenaRaw { get; init; }           
    public required string IdUsuarioRaw { get; init; }           
    public required string IdProductoRaw { get; init; }          

   
    public required string FechaPublicacionRaw { get; init; }    
    public required string EstrellasRaw { get; init; }           

   
    public required string TituloResenaRaw { get; init; }        

    public required string CuerpoResenaRaw { get; init; }        
    public required DateTime FechaCargaMeta { get; init; }       
}
