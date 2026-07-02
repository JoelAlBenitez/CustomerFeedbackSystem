
using CustomerFeedbackSystem.Data.Context;
using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace CustomerFeedbackSystem.Data.Context.Configurations
{
    public partial class TipoFuentesDatoConfiguration : IEntityTypeConfiguration<TipoFuentesDato>
    {
        public void Configure(EntityTypeBuilder<TipoFuentesDato> entity)
        {
            entity.HasKey(e => e.IdTipoFuentes).HasName("PK__TipoFuen__E542858BAB3DF679");

            entity.Property(e => e.TipoFuente)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<TipoFuentesDato> entity);
    }
}
