using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
#nullable disable

namespace CustomerFeedbackSystem.Data.Context;

public partial class CustomerReviewSystemDataContext : DbContext
{
    public CustomerReviewSystemDataContext(DbContextOptions<CustomerReviewSystemDataContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<Clasificacione> Clasificaciones { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Comentario> Comentarios { get; set; }

    public virtual DbSet<ComentariosSociale> ComentariosSociales { get; set; }

    public virtual DbSet<Encuesta> Encuestas { get; set; }

    public virtual DbSet<FuenteEncuesta> FuenteEncuestas { get; set; }

    public virtual DbSet<FuentesDato> FuentesDatos { get; set; }

    public virtual DbSet<FuentesSociale> FuentesSociales { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Reseña> Reseñas { get; set; }

    public virtual DbSet<TipoFuentesDato> TipoFuentesDatos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.CategoriaConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ClasificacioneConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ComentarioConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ComentariosSocialeConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.EncuestaConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.FuenteEncuestaConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.FuentesDatoConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.FuentesSocialeConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ProductoConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ReseñaConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TipoFuentesDatoConfiguration());

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
