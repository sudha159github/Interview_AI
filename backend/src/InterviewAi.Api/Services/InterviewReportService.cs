using InterviewAi.Api.AI;
using InterviewAi.Api.Data;
using InterviewAi.Api.DTOs;
using InterviewAi.Api.Models;

using Microsoft.EntityFrameworkCore;

namespace InterviewAi.Api.Services;

/// <summary>
/// Business logic for generating and reading interview reports.
/// </summary>
public class InterviewReportService(
    AppDbContext db,
    IInterviewReportGenerator generator,
    ICurrentUser currentUser,
    ILogger<InterviewReportService> logger)
{
    // ------------------------------------------------------------------
    // Generate a new report
    // ------------------------------------------------------------------
    public async Task<InterviewReportDto> GenerateAsync(
        CreateInterviewReportRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Ask the AI
        var aiResult = await generator.GenerateAsync(
            request.JobDescription,
            resumeText: null,
            request.SelfDescription,
            cancellationToken);

        // 2. Never trust AI output: check it before saving
        ValidateAiResult(aiResult);

        // 3. Build the database entity
        var now = DateTimeOffset.UtcNow;

        var report = new InterviewReport
        {
            OwnerId = currentUser.UserId,
            Title = aiResult.Title.Trim(),
            JobDescription = request.JobDescription,
            SelfDescription = request.SelfDescription,
            MatchScore = aiResult.MatchScore,
            CreatedAt = now,
            UpdatedAt = now,
            Questions =
            [
                .. ToQuestions(aiResult.TechnicalQuestions, QuestionType.Technical),
                .. ToQuestions(aiResult.BehavioralQuestions, QuestionType.Behavioral)
            ],
            SkillGaps =
            [
                .. aiResult.SkillGaps.Select((gap, index) => new SkillGap
                {
                    Skill = gap.Skill.Trim(),
                    Severity = ParseSeverity(gap.Severity),
                    DisplayOrder = index
                })
            ],
            PreparationPlan =
            [
                .. aiResult.PreparationPlan.Select(day => new PreparationDay
                {
                    DayNumber = day.Day,
                    Focus = day.Focus.Trim(),
                    Tasks = [.. day.Tasks.Select(task => task.Trim())]
                })
            ]
        };

        // 4. Save the report and all its children in one go
        db.InterviewReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Generated interview report {ReportId} for user {UserId} with match score {MatchScore}",
            report.Id, report.OwnerId, report.MatchScore);

        // 5. Return the DTO, not the entity
        return ToDto(report);
    }

    // ------------------------------------------------------------------
    // Get one report (only if it belongs to the current user)
    // ------------------------------------------------------------------
    public async Task<InterviewReportDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var report = await db.InterviewReports
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Questions)
            .Include(r => r.SkillGaps)
            .Include(r => r.PreparationPlan)
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerId == userId, cancellationToken);

        return report is null ? null : ToDto(report);
    }

    // ------------------------------------------------------------------
    // List the current user's reports, newest first
    // ------------------------------------------------------------------
    public async Task<IReadOnlyList<InterviewReportSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        return await db.InterviewReports
            .AsNoTracking()
            .Where(r => r.OwnerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new InterviewReportSummaryDto(r.Id, r.Title, r.MatchScore, r.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    // ==================================================================
    // Private helpers
    // ==================================================================

    private static void ValidateAiResult(AiInterviewReportResult result)
    {
        var errors = new List<string>();

        if (!IsValidText(result.Title, 200))
            errors.Add("Title is missing or too long.");

        if (result.MatchScore is < 0 or > 100)
            errors.Add($"Match score {result.MatchScore} is outside 0-100.");

        if (result.TechnicalQuestions.Count is < 1 or > 15)
            errors.Add("Expected 1-15 technical questions.");

        if (result.BehavioralQuestions.Count is < 1 or > 15)
            errors.Add("Expected 1-15 behavioral questions.");

        foreach (var q in result.TechnicalQuestions.Concat(result.BehavioralQuestions))
        {
            if (!IsValidText(q.Question, 1000) ||
                !IsValidText(q.Intention, 1000) ||
                !IsValidText(q.SuggestedAnswer, 4000))
            {
                errors.Add("A question has missing or too-long text.");
                break;
            }
        }

        if (result.SkillGaps.Count > 15)
            errors.Add("Too many skill gaps.");

        foreach (var gap in result.SkillGaps)
        {
            if (!IsValidText(gap.Skill, 200))
                errors.Add("A skill gap has a missing or too-long name.");

            if (TryParseSeverity(gap.Severity) is null)
                errors.Add($"Unknown skill gap severity '{gap.Severity}'.");
        }

        if (result.PreparationPlan.Count is < 1 or > 30)
            errors.Add("Expected a preparation plan of 1-30 days.");

        for (var i = 0; i < result.PreparationPlan.Count; i++)
        {
            var day = result.PreparationPlan[i];

            if (day.Day != i + 1)
                errors.Add("Preparation days must be numbered 1, 2, 3... in order.");

            if (!IsValidText(day.Focus, 300) || day.Tasks.Count == 0)
                errors.Add($"Day {day.Day} has a missing focus or no tasks.");
        }

        if (errors.Count > 0)
            throw new AiGenerationException("AI returned an invalid report: " + string.Join(" ", errors));
    }

    private static bool IsValidText(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maxLength;

    private static SkillGapSeverity? TryParseSeverity(string value) =>
        Enum.TryParse<SkillGapSeverity>(value, ignoreCase: true, out var severity) && Enum.IsDefined(severity)
            ? severity
            : null;

    private static SkillGapSeverity ParseSeverity(string value) =>
        TryParseSeverity(value) ?? throw new AiGenerationException($"Unknown severity '{value}'.");

    private static IEnumerable<InterviewQuestion> ToQuestions(IReadOnlyList<AiQuestion> questions, QuestionType type) =>
        questions.Select((q, index) => new InterviewQuestion
        {
            Type = type,
            DisplayOrder = index,
            Question = q.Question.Trim(),
            Intention = q.Intention.Trim(),
            SuggestedAnswer = q.SuggestedAnswer.Trim()
        });

    private static InterviewReportDto ToDto(InterviewReport report) => new(
        report.Id,
        report.Title,
        report.MatchScore,
        report.CreatedAt,
        TechnicalQuestions: ToQuestionDtos(report, QuestionType.Technical),
        BehavioralQuestions: ToQuestionDtos(report, QuestionType.Behavioral),
        SkillGaps:
        [
            .. report.SkillGaps
                .OrderBy(g => g.DisplayOrder)
                .Select(g => new SkillGapDto(g.Skill, g.Severity.ToString()))
        ],
        PreparationPlan:
        [
            .. report.PreparationPlan
                .OrderBy(d => d.DayNumber)
                .Select(d => new PreparationDayDto(d.DayNumber, d.Focus, d.Tasks))
        ]);

    private static IReadOnlyList<InterviewQuestionDto> ToQuestionDtos(InterviewReport report, QuestionType type) =>
    [
        .. report.Questions
            .Where(q => q.Type == type)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => new InterviewQuestionDto(q.Question, q.Intention, q.SuggestedAnswer))
    ];
}