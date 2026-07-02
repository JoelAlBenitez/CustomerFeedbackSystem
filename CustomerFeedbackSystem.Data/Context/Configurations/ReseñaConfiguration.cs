
using CustomerFeedbackSystem.Data.Context;
using CustomerFeedbackSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace CustomerFeedbackSystem.Data.Context.Configurations
{
    public partial class ReseñaConfiguration : IEntityTypeConfiguration<Reseña>
    {
        public void Configure(EntityTypeBuilder<Reseña> entity)
        {
            entity.HasKey(e => e.IdReview).HasName("PK__Reseñas__BB56047DF66B72A1");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Reseña> entity);
    }
}
