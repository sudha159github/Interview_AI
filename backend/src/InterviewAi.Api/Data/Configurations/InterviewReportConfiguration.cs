using InterviewAi.Api.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InterviewAi.Api.Data.Configurations;

public class InterviewReportConfiguration : IEntityTypeConfiguration<InterviewReport>
{
    public void Configure(EntityTypeBuilder<InterviewReport> builder)
    {
        // Table name + CHECK constraints
        builder.ToTable("InterviewReports", table =>
        {
            table.HasCheckConstraint(
                "CK_InterviewReports_MatchScore",
                "[MatchScore] BETWEEN 0 AND 100");

            table.HasCheckConstraint(
                "CK_InterviewReports_ResumeOrSelfDescription",
                "[ResumeText] IS NOT NULL OR [SelfDescription] IS NOT NULL");

            table.HasCheckConstraint(
                "CK_InterviewReports_Status",
                "[Status] IN ('Planned', 'Applied', 'Interviewing', 'Offer', 'Rejected')");
        });

        // Primary key
        builder.HasKey(r => r.Id);

        // Text lengths
        builder.Property(r => r.Title)
               .HasMaxLength(200);

        builder.Property(r => r.SelfDescription)
               .HasMaxLength(3000);

        // Application tracking
        builder.Property(r => r.CompanyName)
               .HasMaxLength(200);

        builder.Property(r => r.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(ApplicationStatus.Planned);

        // JobDescription and ResumeText stay nvarchar(max)

        // Concurrency protection
        builder.Property(r => r.RowVersion)
               .IsRowVersion();

        // Fast "my reports, newest first"
        builder.HasIndex(r => new { r.OwnerId, r.CreatedAt })
               .IsDescending(false, true);

        // Upcoming interviews, soonest first
        builder.HasIndex(r => new { r.OwnerId, r.InterviewDate });

        // Owner: every report belongs to a real user.
        // Deleting a user deletes their reports.
        builder.HasOne<ApplicationUser>()
               .WithMany()
               .HasForeignKey(r => r.OwnerId)
               .OnDelete(DeleteBehavior.Cascade);

        // Children: one report has many,
        // deleted together with the report.
        builder.HasMany(r => r.Questions)
               .WithOne()
               .HasForeignKey(q => q.InterviewReportId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.SkillGaps)
               .WithOne()
               .HasForeignKey(g => g.InterviewReportId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.PreparationPlan)
               .WithOne()
               .HasForeignKey(d => d.InterviewReportId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.MockAnswers)
               .WithOne()
               .HasForeignKey(a => a.InterviewReportId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}