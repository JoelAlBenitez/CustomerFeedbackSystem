
using CustomerFeedbackSystem.Data.Context;
using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace CustomerFeedbackSystem.Data.Context.Configurations
{
    public partial class ComentarioConfiguration : IEntityTypeConfiguration<Comentario>
    {
        public void Configure(EntityTypeBuilder<Comentario> entity)
        {
            entity.HasKey(e => e.IdComentario).HasName("PK__Comentar__DDBEFBF9BC2E1EC7");

            entity.Property(e => e.Comentarios).IsRequired();

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Comentario> entity);
    }
}
