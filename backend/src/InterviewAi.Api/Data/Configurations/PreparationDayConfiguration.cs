using InterviewAi.Api.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InterviewAi.Api.Data.Configurations;

public class PreparationDayConfiguration : IEntityTypeConfiguration<PreparationDay>
{
    public void Configure(EntityTypeBuilder<PreparationDay> builder)
    {
        builder.ToTable("PreparationDays", table =>
        {
            table.HasCheckConstraint(
                "CK_PreparationDays_DayNumber",
                "[DayNumber] >= 1");
        });

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Focus).HasMaxLength(300);

        // Tasks (List<string>) is stored automatically as a JSON column, no code needed

        // A report can't have two "Day 3" entries
        builder.HasIndex(d => new { d.InterviewReportId, d.DayNumber })
               .IsUnique();
    }
}