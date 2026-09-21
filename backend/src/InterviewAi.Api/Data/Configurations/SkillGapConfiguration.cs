using InterviewAi.Api.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InterviewAi.Api.Data.Configurations;

public class SkillGapConfiguration : IEntityTypeConfiguration<SkillGap>
{
    public void Configure(EntityTypeBuilder<SkillGap> builder)
    {
        builder.ToTable("SkillGaps", table =>
        {
            table.HasCheckConstraint(
                "CK_SkillGaps_Severity",
                "[Severity] IN ('Low', 'Medium', 'High')");
        });

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Skill).HasMaxLength(200);

        // Store the enum as readable text ("High") instead of a number (3)
        builder.Property(g => g.Severity)
               .HasConversion<string>()
               .HasMaxLength(10);
    }
}