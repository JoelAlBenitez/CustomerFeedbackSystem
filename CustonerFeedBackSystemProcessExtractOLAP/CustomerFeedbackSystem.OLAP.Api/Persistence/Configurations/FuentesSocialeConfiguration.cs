using CustomerFeedbackSystem.OLAP.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerFeedbackSystem.OLAP.Api.Persistence.Configurations;

public sealed class FuentesSocialeConfiguration : IEntityTypeConfiguration<FuentesSociale>
{
    public void Configure(EntityTypeBuilder<FuentesSociale> entity)
    {
        entity.ToTable("FuentesSociales", "dbo");
        entity.HasKey(e => e.IdFuenteSocial);
        entity.Property(e => e.Nombre).HasColumnType("nvarchar(50)").IsRequired();
    }
}
