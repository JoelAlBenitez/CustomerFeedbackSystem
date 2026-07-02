
using CustomerFeedbackSystem.Data.Context;
using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace CustomerFeedbackSystem.Data.Context.Configurations
{
    public partial class FuentesDatoConfiguration : IEntityTypeConfiguration<FuentesDato>
    {
        public void Configure(EntityTypeBuilder<FuentesDato> entity)
        {
            entity.HasKey(e => e.IdFuenteDatos).HasName("PK__FuentesD__22549EE621C861D8");

            entity.Property(e => e.FechaCarga).HasColumnType("datetime");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<FuentesDato> entity);
    }
}
