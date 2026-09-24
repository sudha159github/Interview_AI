using InterviewAi.Api.AI;
using InterviewAi.Api.Data;
using InterviewAi.Api.DTOs;
using InterviewAi.Api.Models;

using Microsoft.EntityFrameworkCore;
namespace InterviewAi.Api.Services;
/// <summary>Practice answers and the AI feedback on them.</summary>
public class MockInterviewService(
AppDbContext db,
IAnswerFeedbackGenerator feedbackGenerator,
ICurrentUser currentUser,
ILogger<MockInterviewService> logger)
{
    /// <summary>
    /// Scores one practice answer and saves it.
    /// Returns null when the report does not exist or does not belong to the current user.
    /// </summary>
    public async Task<MockAnswerDto?> SubmitAnswerAsync(
    Guid reportId,
    SubmitMockAnswerRequest request,
    CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var report = await db.InterviewReports
        .Include(r => r.Questions)
        .FirstOrDefaultAsync(r => r.Id == reportId && r.OwnerId == userId, cancellationToken);
        if (report is null)
        {
            return null;
        }
        var type = Enum.Parse<QuestionType>(request.QuestionType, ignoreCase: true);
        var question = report.Questions
        .FirstOrDefault(q => q.Type == type && q.DisplayOrder == request.QuestionOrder);
        if (question is null)
        {
            return null;
        }
        // 1. Ask the AI for feedback
        var feedback = await feedbackGenerator.GenerateAsync(
        new AnswerFeedbackRequest(
        report.JobDescription,
        question.Question,
        question.Intention,
        question.SuggestedAnswer,
        request.AnswerText.Trim()),
        cancellationToken);
        // 2. Never trust AI output: check it before saving
        ValidateFeedback(feedback);
        // 3. Save
        var answer = new MockAnswer
        {
            InterviewReportId = report.Id,
            QuestionType = type,
            QuestionOrder = question.DisplayOrder,
            QuestionText = question.Question,
            AnswerText = request.AnswerText.Trim(),
            Score = feedback.Score,
            StarAssessment = feedback.StarAssessment.Trim(),
            Strengths = [.. feedback.Strengths.Select(s => s.Trim())],
            Improvements = [.. feedback.Improvements.Select(s => s.Trim())],
            MissingKeywords = [.. feedback.MissingKeywords.Select(s => s.Trim())],
            CreatedAt = DateTimeOffset.UtcNow,
        };
        report.MockAnswers.Add(answer);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
        "Scored a practice answer for report {ReportId} (question {Type} {Order}, score {Score})",
        report.Id, type, question.DisplayOrder, answer.Score);
        return ToDto(answer);
    }
    /// <summary>All practice answers for a report, oldest first.</summary>
    public async Task<IReadOnlyList<MockAnswerDto>?> ListAnswersAsync(
    Guid reportId,
    CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var reportExists = await db.InterviewReports
        .AnyAsync(r => r.Id == reportId && r.OwnerId == userId, cancellationToken);
        if (!reportExists)
        {
            return null;
        }
        var answers = await db.Set<MockAnswer>()
        .AsNoTracking()
        .Where(a => a.InterviewReportId == reportId)
        .OrderBy(a => a.CreatedAt)
        .ToListAsync(cancellationToken);
        return [.. answers.Select(ToDto)];
    }
    // ---------- private helpers ----------
    private static void ValidateFeedback(AiAnswerFeedback feedback)
    {
        var errors = new List<string>();
        if (feedback.Score is < 0 or > 100)
        {
            errors.Add($"Score {feedback.Score} is outside 0-100.");
        }
        if (string.IsNullOrWhiteSpace(feedback.StarAssessment) || feedback.StarAssessment.Length > 500)
        {
            errors.Add("The structure assessment is missing or too long.");
        }
        if (feedback.Strengths.Count is < 1 or > 5 || feedback.Improvements.Count is < 1 or > 5)
        {
            errors.Add("Expected 1-5 strengths and 1-5 improvements.");
        }
        if (feedback.MissingKeywords.Count > 10)
        {
            errors.Add("Too many missing keywords.");
        }
        if (feedback.Strengths.Concat(feedback.Improvements).Concat(feedback.MissingKeywords)
        .Any(item => string.IsNullOrWhiteSpace(item) || item.Length > 300))
        {
            errors.Add("A feedback point is empty or too long.");
        }
        if (errors.Count > 0)
        {
            throw new AiGenerationException("AI returned invalid feedback: " + string.Join(" ", errors));
        }
    }
    private static MockAnswerDto ToDto(MockAnswer answer) => new(
    answer.Id,
    answer.QuestionType.ToString(),
    answer.QuestionOrder,
    answer.QuestionText,
    answer.AnswerText,
    answer.Score,
    answer.StarAssessment,
    answer.Strengths,
    answer.Improvements,
    answer.MissingKeywords,
    answer.CreatedAt);
}