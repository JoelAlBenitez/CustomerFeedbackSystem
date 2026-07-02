using CustomerFeedbackSystem.Data.Context;
using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace CustomerFeedbackSystem.Data.Context.Configurations
{
    public partial class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
    {
        public void Configure(EntityTypeBuilder<Categoria> entity)
        {
            entity.HasKey(e => e.IdCategoria).HasName("PK__Categori__A3C02A10FB9814AC");

            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(50);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Categoria> entity);
    }
}
