using CustomerFeedbackSystem.OLAP.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomerFeedbackSystem.OLAP.Api.Persistence;

public sealed class OltpReadDbContext : DbContext
{
    public OltpReadDbContext(DbContextOptions<OltpReadDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<ComentariosSociale> ComentariosSociales => Set<ComentariosSociale>();

    public DbSet<Comentario> Comentarios => Set<Comentario>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<FuentesSociale> FuentesSociales => Set<FuentesSociale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OltpReadDbContext).Assembly);
    }
}
