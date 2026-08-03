using CustomerFeedbackSystem.OLAP.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerFeedbackSystem.OLAP.Api.Persistence.Configurations;

public sealed class ComentarioConfiguration : IEntityTypeConfiguration<Comentario>
{
    public void Configure(EntityTypeBuilder<Comentario> entity)
    {
        entity.ToTable("Comentarios", "dbo");
        entity.HasKey(e => e.IdComentario);
        entity.Property(e => e.Comentarios).HasColumnType("nvarchar(max)").IsRequired();
    }
}
