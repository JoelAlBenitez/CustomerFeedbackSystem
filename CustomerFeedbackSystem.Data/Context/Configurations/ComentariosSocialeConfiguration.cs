
using CustomerFeedbackSystem.Data.Context;
using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace CustomerFeedbackSystem.Data.Context.Configurations
{
    public partial class ComentariosSocialeConfiguration : IEntityTypeConfiguration<ComentariosSociale>
    {
        public void Configure(EntityTypeBuilder<ComentariosSociale> entity)
        {
            entity.HasKey(e => e.IdComentarioSocial).HasName("PK__Comentar__20E14C03CAC9314E");

            entity.Property(e => e.Fecha).HasColumnType("datetime");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<ComentariosSociale> entity);
    }
}
