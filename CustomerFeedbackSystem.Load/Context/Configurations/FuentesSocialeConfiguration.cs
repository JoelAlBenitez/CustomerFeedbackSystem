
using CustomerFeedbackSystem.Data.Context;
using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace CustomerFeedbackSystem.Data.Context.Configurations
{
    public partial class FuentesSocialeConfiguration : IEntityTypeConfiguration<FuentesSociale>
    {
        public void Configure(EntityTypeBuilder<FuentesSociale> entity)
        {
            entity.HasKey(e => e.IdFuenteSocial).HasName("PK__FuentesS__FC3E688A0A8A40B1");

            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(50);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<FuentesSociale> entity);
    }
}
