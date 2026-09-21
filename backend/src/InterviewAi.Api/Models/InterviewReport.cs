namespace InterviewAi.Api.Models;

public class InterviewReport
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public required string Title { get; set; }

    public required string JobDescription { get; set; }

    public string? ResumeText { get; set; }

    public string? SelfDescription { get; set; }

    public int MatchScore { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Children (one report has many of each)
    public List<InterviewQuestion> Questions { get; set; } = [];

    public List<SkillGap> SkillGaps { get; set; } = [];

    public List<PreparationDay> PreparationPlan { get; set; } = [];
}