namespace InterviewAi.Api.AI;

/// <summary>
/// Generates an interview preparation report from a job description and the candidate's profile.
/// </summary>
public interface IInterviewReportGenerator
{
    Task<AiInterviewReportResult> GenerateAsync(
        string jobDescription,
        string? resumeText,
        string? selfDescription,
        CancellationToken cancellationToken);
}