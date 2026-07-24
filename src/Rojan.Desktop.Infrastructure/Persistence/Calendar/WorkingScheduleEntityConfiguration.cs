using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rojan.Desktop.Infrastructure.Persistence.Calendar;

/// <summary>EF Core mapping for <see cref="WorkingScheduleEntity"/> - applied by <see cref="RojanDbContext.OnModelCreating"/>.</summary>
internal sealed class WorkingScheduleEntityConfiguration : IEntityTypeConfiguration<WorkingScheduleEntity>
{
    public void Configure(EntityTypeBuilder<WorkingScheduleEntity> builder)
    {
        builder.ToTable("WorkingSchedules");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).IsRequired();
        builder.Property(entity => entity.SpecialistId).IsRequired();
        builder.Property(entity => entity.SpecialistName).IsRequired();

        // CalendarQueryService's own read pattern: every schedule lookup
        // filters by SpecialistId first, then (for a single day) by
        // DayOfWeek within that specialist's own schedules.
        builder.HasIndex(entity => new { entity.SpecialistId, entity.DayOfWeek });
    }
}
