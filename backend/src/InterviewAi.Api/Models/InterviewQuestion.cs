namespace InterviewAi.Api.Models;

public class InterviewQuestion
{
    public int Id { get; set; }

    public Guid InterviewReportId { get; set; }

    public QuestionType Type { get; set; }

    public int DisplayOrder { get; set; }

    public required string Question { get; set; }

    public required string Intention { get; set; }

    public required string SuggestedAnswer { get; set; }
}