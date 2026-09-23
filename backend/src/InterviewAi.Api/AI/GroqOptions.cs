using System.ComponentModel.DataAnnotations;

namespace InterviewAi.Api.AI;

/// <summary>
/// Groq settings, read from the "Groq" configuration section.
/// </summary>
public class GroqOptions
{
    public const string SectionName = "Groq";

    // Secret: stored in User Secrets (development) or environment variables (production)
    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string Model { get; init; } = "llama-3.3-70b-versatile";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = "https://api.groq.com/openai/v1/";
}