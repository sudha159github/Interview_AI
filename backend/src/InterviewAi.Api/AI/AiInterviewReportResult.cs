namespace InterviewAi.Api.AI;

/// <summary>
/// The raw result produced by an AI generator.
/// Treated as untrusted input: it is validated before being saved.
/// </summary>
public record AiInterviewReportResult(
    string Title,
    int MatchScore,
    IReadOnlyList<AiQuestion> TechnicalQuestions,
    IReadOnlyList<AiQuestion> BehavioralQuestions,
    IReadOnlyList<AiSkillGap> SkillGaps,
    IReadOnlyList<AiPreparationDay> PreparationPlan);

public record AiQuestion(
    string Question,
    string Intention,
    string SuggestedAnswer);

public record AiSkillGap(
    string Skill,
    string Severity);

public record AiPreparationDay(
    int Day,
    string Focus,
    IReadOnlyList<string> Tasks);