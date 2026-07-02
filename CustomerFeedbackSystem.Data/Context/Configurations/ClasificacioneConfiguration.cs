
using CustomerFeedbackSystem.Data.Context;
using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace CustomerFeedbackSystem.Data.Context.Configurations
{
    public partial class ClasificacioneConfiguration : IEntityTypeConfiguration<Clasificacione>
    {
        public void Configure(EntityTypeBuilder<Clasificacione> entity)
        {
            entity.HasKey(e => e.IdClasificacion).HasName("PK__Clasific__4CABC778045D1935");

            entity.Property(e => e.Clasificacion)
                .IsRequired()
                .HasMaxLength(50);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Clasificacione> entity);
    }
}
