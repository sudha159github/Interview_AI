namespace InterviewAi.Api.AI;

/// <summary>
/// Thrown when the AI returns a result that fails validation.
/// </summary>
public class AiGenerationException(string message) : Exception(message);