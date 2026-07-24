using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Calendar;

/// <summary>EF Core mapping for <see cref="ReservedSlotEntity"/> - applied by <see cref="RojanDbContext.OnModelCreating"/>.</summary>
internal sealed class ReservedSlotEntityConfiguration : IEntityTypeConfiguration<ReservedSlotEntity>
{
    public void Configure(EntityTypeBuilder<ReservedSlotEntity> builder)
    {
        builder.ToTable("ReservedSlots");

        // No synthetic id - see this entity's own doc comment. The
        // composite key's leading column (SpecialistId) already serves as
        // an efficient index for "every reserved slot for this specialist"
        // lookups (leftmost-prefix rule, exactly what GetBookedSlotsAsync
        // needs), so no separate index is required.
        builder.HasKey(entity => new { entity.SpecialistId, entity.Start, entity.End });
    }
}
