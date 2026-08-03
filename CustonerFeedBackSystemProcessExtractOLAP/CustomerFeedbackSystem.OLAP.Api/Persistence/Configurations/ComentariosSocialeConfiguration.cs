using CustomerFeedbackSystem.OLAP.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerFeedbackSystem.OLAP.Api.Persistence.Configurations;

public sealed class ComentariosSocialeConfiguration : IEntityTypeConfiguration<ComentariosSociale>
{
    public void Configure(EntityTypeBuilder<ComentariosSociale> entity)
    {
        entity.ToTable("ComentariosSociales", "dbo");
        entity.HasKey(e => e.IdComentarioSocial);
        entity.Property(e => e.Fecha).HasColumnType("datetime");
    }
}
