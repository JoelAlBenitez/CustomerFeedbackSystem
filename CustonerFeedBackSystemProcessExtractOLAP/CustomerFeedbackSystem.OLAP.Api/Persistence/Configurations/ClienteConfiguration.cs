using CustomerFeedbackSystem.OLAP.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerFeedbackSystem.OLAP.Api.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> entity)
    {
        entity.ToTable("Clientes", "dbo");
        entity.HasKey(e => e.IdCliente);
        entity.Property(e => e.Nombre).HasColumnType("varchar(50)").IsRequired();
        entity.Property(e => e.Email).HasColumnType("varchar(254)").IsRequired();
    }
}
