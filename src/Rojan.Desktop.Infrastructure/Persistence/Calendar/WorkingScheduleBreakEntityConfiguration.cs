using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Calendar;

/// <summary>EF Core mapping for <see cref="WorkingScheduleBreakEntity"/> - applied by <see cref="RojanDbContext.OnModelCreating"/>.</summary>
internal sealed class WorkingScheduleBreakEntityConfiguration : IEntityTypeConfiguration<WorkingScheduleBreakEntity>
{
    public void Configure(EntityTypeBuilder<WorkingScheduleBreakEntity> builder)
    {
        builder.ToTable("WorkingScheduleBreaks");

        // No synthetic id - see this entity's own doc comment. The
        // composite key's leading column (WorkingScheduleId) already
        // serves as an efficient index for "every break for this
        // schedule" lookups (leftmost-prefix rule), so no separate index
        // is needed.
        builder.HasKey(entity => new { entity.WorkingScheduleId, entity.Start, entity.End });

        // A break can never outlive the working schedule it belongs to -
        // a genuine within-module parent-child relationship, unlike
        // Bookings' cross-slice references (see WorkingScheduleBreakEntity's
        // own doc comment).
        builder.HasOne<WorkingScheduleEntity>()
            .WithMany()
            .HasForeignKey(entity => entity.WorkingScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
