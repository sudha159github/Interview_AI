using System.ComponentModel.DataAnnotations;

namespace InterviewAi.Api.DTOs;

/// <summary>What the client sends to create an account.</summary>
public record RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_.-]+$",
        ErrorMessage = "Username may only contain letters, numbers, '.', '_' and '-'.")]
    public string UserName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}

/// <summary>What the client sends to log in.</summary>
public record LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

/// <summary>Returned after a successful register or login.</summary>
public record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserDto User);

/// <summary>Public information about a user.</summary>
public record UserDto(
    Guid Id,
    string UserName,
    string Email);