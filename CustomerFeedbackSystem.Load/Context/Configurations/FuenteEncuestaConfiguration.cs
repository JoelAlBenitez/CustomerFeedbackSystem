
using CustomerFeedbackSystem.Data.Context;
using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace CustomerFeedbackSystem.Data.Context.Configurations
{
    public partial class FuenteEncuestaConfiguration : IEntityTypeConfiguration<FuenteEncuesta>
    {
        public void Configure(EntityTypeBuilder<FuenteEncuesta> entity)
        {
            entity.HasKey(e => e.IdFuenteEncuestas).HasName("PK__FuenteEn__51C51D3CD5C7A12D");

            entity.Property(e => e.Fuente)
                .IsRequired()
                .HasMaxLength(50);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<FuenteEncuesta> entity);
    }
}
