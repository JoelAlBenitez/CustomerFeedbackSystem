
using CustomerFeedbackSystem.Data.Context;
using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace CustomerFeedbackSystem.Data.Context.Configurations
{
    public partial class ProductoConfiguration : IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> entity)
        {
            entity.HasKey(e => e.IdProducto).HasName("PK__Producto__09889210A2986332");

            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Producto> entity);
    }
}
