using InterviewAi.Api.AI;
using InterviewAi.Api.Data;
using InterviewAi.Api.Documents;
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
    IResumeTextExtractor resumeTextExtractor,
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
        // 1. Read the resume, if one was uploaded
        string? resumeText = null;

        if (request.Resume is not null)
        {
            await using var stream = request.Resume.OpenReadStream();

            resumeText = await resumeTextExtractor.ExtractTextAsync(
                stream,
                request.Resume.FileName,
                cancellationToken);
        }

        // 2. Ask the AI, sized to the time available before the interview
        var planDays = CalculatePlanDays(request.InterviewDate);

        var aiResult = await generator.GenerateAsync(
            request.JobDescription,
            resumeText,
            request.SelfDescription,
            planDays,
            cancellationToken);

        // 3. Never trust AI output: check it before saving
        ValidateAiResult(aiResult);

        // 4. Build the database entity
        var now = DateTimeOffset.UtcNow;

        var report = new InterviewReport
        {
            OwnerId = currentUser.UserId,
            Title = aiResult.Title.Trim(),
            JobDescription = request.JobDescription,
            ResumeText = resumeText,
            SelfDescription = request.SelfDescription,

            // Application tracking
            CompanyName = string.IsNullOrWhiteSpace(request.CompanyName)
                ? null
                : request.CompanyName.Trim(),

            InterviewDate = request.InterviewDate,
            Status = ApplicationStatus.Planned,

            MatchScore = aiResult.MatchScore,
            CreatedAt = now,
            UpdatedAt = now,

            Questions =
            [
                .. ToQuestions(
                    aiResult.TechnicalQuestions,
                    QuestionType.Technical),

                .. ToQuestions(
                    aiResult.BehavioralQuestions,
                    QuestionType.Behavioral)
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

        // 5. Save the report and all its children in one go
        db.InterviewReports.Add(report);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Generated interview report {ReportId} for user {UserId} " +
            "with match score {MatchScore} (resume uploaded: {HasResume})",
            report.Id,
            report.OwnerId,
            report.MatchScore,
            resumeText is not null);

        // 6. Return the DTO, not the entity
        return ToDto(report);
    }

    // ------------------------------------------------------------------
    // Get one report (only if it belongs to the current user)
    // ------------------------------------------------------------------
    public async Task<InterviewReportDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var report = await db.InterviewReports
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Questions)
            .Include(r => r.SkillGaps)
            .Include(r => r.PreparationPlan)
            .FirstOrDefaultAsync(
                r => r.Id == id && r.OwnerId == userId,
                cancellationToken);

        return report is null ? null : ToDto(report);
    }

    // ------------------------------------------------------------------
    // Update tracking details
    // (only if the report belongs to the current user)
    // ------------------------------------------------------------------
    public async Task<InterviewReportDto?> UpdateAsync(
        Guid id,
        UpdateInterviewReportRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Tracked query (no AsNoTracking):
        // we are about to change this row.
        var report = await db.InterviewReports
            .AsSplitQuery()
            .Include(r => r.Questions)
            .Include(r => r.SkillGaps)
            .Include(r => r.PreparationPlan)
            .FirstOrDefaultAsync(
                r => r.Id == id && r.OwnerId == userId,
                cancellationToken);

        if (report is null)
        {
            return null;
        }

        report.CompanyName = string.IsNullOrWhiteSpace(request.CompanyName)
            ? null
            : request.CompanyName.Trim();

        report.InterviewDate = request.InterviewDate;

        report.Status = Enum.Parse<ApplicationStatus>(
            request.Status,
            ignoreCase: true);

        report.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated tracking details for report {ReportId} (status {Status})",
            report.Id,
            report.Status);

        return ToDto(report);
    }

    // ------------------------------------------------------------------
    // List the current user's reports, newest first
    // ------------------------------------------------------------------
    public async Task<IReadOnlyList<InterviewReportSummaryDto>> ListAsync(
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Countdown is calculated in C#, not SQL.
        var rows = await db.InterviewReports
            .AsNoTracking()
            .Where(r => r.OwnerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.CompanyName,
                r.MatchScore,
                r.CreatedAt,
                r.InterviewDate,
                r.Status,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(r => new InterviewReportSummaryDto(
                r.Id,
                r.Title,
                r.CompanyName,
                r.MatchScore,
                r.CreatedAt,
                r.InterviewDate,
                r.Status.ToString(),
                DaysUntil(r.InterviewDate)))
        ];
    }

    // ==================================================================
    // Private helpers
    // ==================================================================

    private static int CalculatePlanDays(DateTimeOffset? interviewDate)
    {
        if (interviewDate is null)
        {
            return 5;
        }

        var days =
            (int)Math.Ceiling(
                (
                    interviewDate.Value.UtcDateTime.Date
                    - DateTime.UtcNow.Date
                ).TotalDays);

        return Math.Clamp(days, 1, 7);
    }

    /// <summary>
    /// Whole days until the interview; negative once it has passed.
    /// </summary>
    private static int? DaysUntil(DateTimeOffset? interviewDate) =>
        interviewDate is null
            ? null
            : (int)Math.Ceiling(
                (
                    interviewDate.Value.UtcDateTime.Date
                    - DateTime.UtcNow.Date
                ).TotalDays);

    private static void ValidateAiResult(
        AiInterviewReportResult result)
    {
        var errors = new List<string>();

        if (!IsValidText(result.Title, 200))
        {
            errors.Add("Title is missing or too long.");
        }

        if (result.MatchScore is < 0 or > 100)
        {
            errors.Add(
                $"Match score {result.MatchScore} is outside 0-100.");
        }

        if (result.TechnicalQuestions.Count is < 1 or > 15)
        {
            errors.Add("Expected 1-15 technical questions.");
        }

        if (result.BehavioralQuestions.Count is < 1 or > 15)
        {
            errors.Add("Expected 1-15 behavioral questions.");
        }

        foreach (var q in result.TechnicalQuestions
                     .Concat(result.BehavioralQuestions))
        {
            if (!IsValidText(q.Question, 1000) ||
                !IsValidText(q.Intention, 1000) ||
                !IsValidText(q.SuggestedAnswer, 4000))
            {
                errors.Add(
                    "A question has missing or too-long text.");

                break;
            }
        }

        if (result.SkillGaps.Count > 15)
        {
            errors.Add("Too many skill gaps.");
        }

        foreach (var gap in result.SkillGaps)
        {
            if (!IsValidText(gap.Skill, 200))
            {
                errors.Add(
                    "A skill gap has a missing or too-long name.");
            }

            if (TryParseSeverity(gap.Severity) is null)
            {
                errors.Add(
                    $"Unknown skill gap severity '{gap.Severity}'.");
            }
        }

        if (result.PreparationPlan.Count is < 1 or > 30)
        {
            errors.Add(
                "Expected a preparation plan of 1-30 days.");
        }

        for (var i = 0; i < result.PreparationPlan.Count; i++)
        {
            var day = result.PreparationPlan[i];

            if (day.Day != i + 1)
            {
                errors.Add(
                    "Preparation days must be numbered 1, 2, 3... in order.");
            }

            if (!IsValidText(day.Focus, 300) ||
                day.Tasks.Count == 0)
            {
                errors.Add(
                    $"Day {day.Day} has a missing focus or no tasks.");
            }
        }

        if (errors.Count > 0)
        {
            throw new AiGenerationException(
                "AI returned an invalid report: " +
                string.Join(" ", errors));
        }
    }

    private static bool IsValidText(
        string? value,
        int maxLength) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= maxLength;

    private static SkillGapSeverity? TryParseSeverity(
        string value) =>
        Enum.TryParse<SkillGapSeverity>(
            value,
            ignoreCase: true,
            out var severity)
            && Enum.IsDefined(severity)
                ? severity
                : null;

    private static SkillGapSeverity ParseSeverity(
        string value) =>
        TryParseSeverity(value)
        ?? throw new AiGenerationException(
            $"Unknown severity '{value}'.");

    private static IEnumerable<InterviewQuestion> ToQuestions(
        IReadOnlyList<AiQuestion> questions,
        QuestionType type) =>
        questions.Select((q, index) => new InterviewQuestion
        {
            Type = type,
            DisplayOrder = index,
            Question = q.Question.Trim(),
            Intention = q.Intention.Trim(),
            SuggestedAnswer = q.SuggestedAnswer.Trim()
        });

    private static InterviewReportDto ToDto(
        InterviewReport report) =>
        new(
            report.Id,
            report.Title,
            report.CompanyName,
            report.MatchScore,
            report.CreatedAt,
            report.InterviewDate,
            report.Status.ToString(),
            DaysUntil(report.InterviewDate),

            TechnicalQuestions:
            ToQuestionDtos(
                report,
                QuestionType.Technical),

            BehavioralQuestions:
            ToQuestionDtos(
                report,
                QuestionType.Behavioral),

            SkillGaps:
            [
                .. report.SkillGaps
                    .OrderBy(g => g.DisplayOrder)
                    .Select(g =>
                        new SkillGapDto(
                            g.Skill,
                            g.Severity.ToString()))
            ],

            PreparationPlan:
            [
                .. report.PreparationPlan
                    .OrderBy(d => d.DayNumber)
                    .Select(d =>
                        new PreparationDayDto(
                            d.DayNumber,
                            d.Focus,
                            d.Tasks))
            ]);

    private static IReadOnlyList<InterviewQuestionDto> ToQuestionDtos(
        InterviewReport report,
        QuestionType type) =>
    [
        .. report.Questions
            .Where(q => q.Type == type)
            .OrderBy(q => q.DisplayOrder)
            .Select(q =>
                new InterviewQuestionDto(
                    q.Question,
                    q.Intention,
                    q.SuggestedAnswer))
    ];
}