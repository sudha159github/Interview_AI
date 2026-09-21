using System.ComponentModel.DataAnnotations;

namespace InterviewAi.Api.Services;

/// <summary>
/// JWT settings, read from the "Jwt" configuration section.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    // Secret: stored in User Secrets (development) or environment variables (production)
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Range(5, 1440)]
    public int AccessTokenMinutes { get; init; } = 60;
}