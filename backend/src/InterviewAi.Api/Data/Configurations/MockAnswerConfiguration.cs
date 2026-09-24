using InterviewAi.Api.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace InterviewAi.Api.Data.Configurations;

public class MockAnswerConfiguration : IEntityTypeConfiguration<MockAnswer>
{
    public void Configure(EntityTypeBuilder<MockAnswer> builder)
    {
        builder.ToTable("MockAnswers", table =>
        {
            table.HasCheckConstraint(
            "CK_MockAnswers_Score",
            "[Score] BETWEEN 0 AND 100");
            table.HasCheckConstraint(
            "CK_MockAnswers_QuestionType",
            "[QuestionType] IN ('Technical', 'Behavioral')");
        });
        builder.HasKey(a => a.Id);
        builder.Property(a => a.QuestionType)
        .HasConversion<string>()
        .HasMaxLength(20);
        builder.Property(a => a.QuestionText).HasMaxLength(1000);
        builder.Property(a => a.AnswerText).HasMaxLength(5000);
        builder.Property(a => a.StarAssessment).HasMaxLength(500);
        // Strengths, Improvements and MissingKeywords are List<string>:
        // EF Core stores each one as a JSON column automatically.
        builder.HasIndex(a => new { a.InterviewReportId, a.CreatedAt });
    }
}