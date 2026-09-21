namespace InterviewAi.Api.DTOs;

/// <summary>Full report, returned when viewing a single report.</summary>
public record InterviewReportDto(
    Guid Id,
    string Title,
    int MatchScore,
    DateTimeOffset CreatedAt,
    IReadOnlyList<InterviewQuestionDto> TechnicalQuestions,
    IReadOnlyList<InterviewQuestionDto> BehavioralQuestions,
    IReadOnlyList<SkillGapDto> SkillGaps,
    IReadOnlyList<PreparationDayDto> PreparationPlan);

/// <summary>Short version, returned in the "my reports" list.</summary>
public record InterviewReportSummaryDto(
    Guid Id,
    string Title,
    int MatchScore,
    DateTimeOffset CreatedAt);

public record InterviewQuestionDto(
    string Question,
    string Intention,
    string SuggestedAnswer);

public record SkillGapDto(
    string Skill,
    string Severity);

public record PreparationDayDto(
    int DayNumber,
    string Focus,
    IReadOnlyList<string> Tasks);