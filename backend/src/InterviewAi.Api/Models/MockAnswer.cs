namespace InterviewAi.Api.Models;
/// <summary>One practice answer, with the AI feedback it received.</summary>
public class MockAnswer
{
    public int Id { get; set; }
    public Guid InterviewReportId { get; set; }
    /// <summary>Which question was answered.</summary>
    public QuestionType QuestionType { get; set; }
    public int QuestionOrder { get; set; }
    /// <summary>Copied at the time of answering, so history stays readable.</summary>
    public required string QuestionText { get; set; }
    public required string AnswerText { get; set; }
    public int Score { get; set; }
    public required string StarAssessment { get; set; }
    public List<string> Strengths { get; set; } = [];
    public List<string> Improvements { get; set; } = [];
    public List<string> MissingKeywords { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
}