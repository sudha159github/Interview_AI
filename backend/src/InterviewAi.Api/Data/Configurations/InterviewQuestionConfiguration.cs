using InterviewAi.Api.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InterviewAi.Api.Data.Configurations;

public class InterviewQuestionConfiguration : IEntityTypeConfiguration<InterviewQuestion>
{
    public void Configure(EntityTypeBuilder<InterviewQuestion> builder)
    {
        builder.ToTable("InterviewQuestions", table =>
        {
            table.HasCheckConstraint(
                "CK_InterviewQuestions_Type",
                "[Type] IN ('Technical', 'Behavioral')");
        });

        builder.HasKey(q => q.Id);

        // Store the enum as readable text ("Technical") instead of a number (1)
        builder.Property(q => q.Type)
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(q => q.Question).HasMaxLength(1000);
        builder.Property(q => q.Intention).HasMaxLength(1000);
        builder.Property(q => q.SuggestedAnswer).HasMaxLength(4000);
    }
}