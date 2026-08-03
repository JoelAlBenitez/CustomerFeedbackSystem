using CustomerFeedbackSystem.OLAP.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerFeedbackSystem.OLAP.Api.Persistence.Configurations;

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> entity)
    {
        entity.ToTable("Productos", "dbo");
        entity.HasKey(e => e.IdProducto);
        entity.Property(e => e.Nombre).HasColumnType("nvarchar(100)").IsRequired();
    }
}
