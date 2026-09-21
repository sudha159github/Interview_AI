namespace InterviewAi.Api.Models;

public class SkillGap
{
    public int Id { get; set; }

    public Guid InterviewReportId { get; set; }

    public required string Skill { get; set; }

    public SkillGapSeverity Severity { get; set; }

    public int DisplayOrder { get; set; }
}