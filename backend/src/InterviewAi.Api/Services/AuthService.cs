using InterviewAi.Api.DTOs;
using InterviewAi.Api.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace InterviewAi.Api.Services;

/// <summary>Why an authentication operation failed.</summary>
public enum AuthErrorType
{
    None = 0,
    Conflict = 1,
    Validation = 2,
    InvalidCredentials = 3
}

/// <summary>Outcome of a register, login or refresh attempt.</summary>
public record AuthResult(AuthResponse? Response, AuthErrorType ErrorType, IReadOnlyList<string> Errors)
{
    public bool Succeeded => Response is not null;

    public static AuthResult Success(AuthResponse response) =>
        new(response, AuthErrorType.None, []);

    public static AuthResult Failure(AuthErrorType errorType, params string[] errors) =>
        new(null, errorType, errors);
}

/// <summary>
/// Registration, login and session renewal, built on ASP.NET Core Identity.
/// </summary>
public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    TokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        // 1. Email and username must be unique
        if (await userManager.FindByEmailAsync(request.Email) is not null ||
            await userManager.FindByNameAsync(request.UserName) is not null)
        {
            return AuthResult.Failure(
                AuthErrorType.Conflict,
                "An account with this email or username already exists.");
        }

        // 2. Create the user; Identity hashes the password and checks the password rules
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return AuthResult.Failure(
                AuthErrorType.Validation,
                [.. result.Errors.Select(e => e.Description)]);
        }

        logger.LogInformation("User {UserId} registered", user.Id);

        // 3. Registering also logs the user in
        return AuthResult.Success(CreateResponse(user));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            // Same message as a wrong password: don't reveal which emails are registered
            return AuthResult.Failure(AuthErrorType.InvalidCredentials, "Invalid email or password.");
        }

        // Checks the password hash and counts failed attempts (lockout)
        var check = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (check.IsLockedOut)
        {
            logger.LogWarning("User {UserId} is locked out after repeated failed logins", user.Id);

            return AuthResult.Failure(
                AuthErrorType.InvalidCredentials,
                "Too many failed attempts. Please try again later.");
        }

        if (!check.Succeeded)
        {
            logger.LogInformation("Failed login attempt for user {UserId}", user.Id);

            return AuthResult.Failure(AuthErrorType.InvalidCredentials, "Invalid email or password.");
        }

        logger.LogInformation("User {UserId} logged in", user.Id);

        return AuthResult.Success(CreateResponse(user));
    }

    /// <summary>
    /// Issues a fresh access token for an already-authenticated request,
    /// as long as the session has not exceeded its maximum length.
    /// </summary>
    public async Task<AuthResult> RefreshAsync(Guid userId, DateTimeOffset sessionStartedAt)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return AuthResult.Failure(AuthErrorType.InvalidCredentials, "This session is no longer valid.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("Refresh refused for locked-out user {UserId}", user.Id);

            return AuthResult.Failure(AuthErrorType.InvalidCredentials, "This account is locked.");
        }

        var sessionAge = DateTimeOffset.UtcNow - sessionStartedAt;

        if (sessionAge > TimeSpan.FromHours(_jwt.MaxSessionHours))
        {
            logger.LogInformation("Session for user {UserId} reached its maximum length", user.Id);

            return AuthResult.Failure(
                AuthErrorType.InvalidCredentials,
                "Your session has reached its maximum length. Please sign in again.");
        }

        // Keep the original session start so the maximum length still applies
        var token = tokenService.CreateAccessToken(user, sessionStartedAt);

        return AuthResult.Success(new AuthResponse(token.Token, token.ExpiresAt, ToUserDto(user)));
    }

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        return user is null ? null : ToUserDto(user);
    }

    private AuthResponse CreateResponse(ApplicationUser user)
    {
        var token = tokenService.CreateAccessToken(user);

        return new AuthResponse(token.Token, token.ExpiresAt, ToUserDto(user));
    }

    private static UserDto ToUserDto(ApplicationUser user) =>
        new(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty);
}