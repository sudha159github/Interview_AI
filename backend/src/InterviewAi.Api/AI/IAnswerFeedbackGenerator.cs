namespace InterviewAi.Api.AI;
/// <summary>What the AI is given when scoring a practice answer.</summary>
public record AnswerFeedbackRequest(
string JobDescription,
string Question,
string Intention,
string ModelAnswer,
string CandidateAnswer);
/// <summary>Raw feedback from the AI. Validated before it is saved.</summary>
public record AiAnswerFeedback(
int Score,
string StarAssessment,
IReadOnlyList<string> Strengths,
IReadOnlyList<string> Improvements,
IReadOnlyList<string> MissingKeywords);
/// <summary>Scores a candidate's practice answer.</summary>
public interface IAnswerFeedbackGenerator
{
    Task<AiAnswerFeedback> GenerateAsync(
    AnswerFeedbackRequest request,
    CancellationToken cancellationToken);
}