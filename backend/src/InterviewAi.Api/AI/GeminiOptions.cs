using System.ComponentModel.DataAnnotations;

namespace InterviewAi.Api.AI;

/// <summary>
/// Gemini settings, read from the "Gemini" configuration section.
/// </summary>
public class GeminiOptions
{
    public const string SectionName = "Gemini";

    // Secret: stored in User Secrets (development) or environment variables (production)
    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string Model { get; init; } = "gemini-3.8-flash";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta/";
}