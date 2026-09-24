using InterviewAi.Api.DTOs;
using InterviewAi.Api.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterviewAi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Create an account. Returns an access token (registering also logs you in).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);

        return result.Succeeded ? Ok(result.Response) : ToProblem(result);
    }

    /// <summary>Log in with email and password. Returns an access token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);

        return result.Succeeded ? Ok(result.Response) : ToProblem(result);
    }

    /// <summary>Get the currently logged-in user.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await authService.GetUserAsync(currentUser.UserId);

        return user is null ? Unauthorized() : Ok(user);
    }

    /// <summary>Renew the access token for an active session.</summary>
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        var sessionStartedAt =
            currentUser.SessionStartedAt ?? DateTimeOffset.UtcNow;

        var result = await authService.RefreshAsync(
            currentUser.UserId,
            sessionStartedAt);

        return result.Succeeded ? Ok(result.Response) : ToProblem(result);
    }

    // Turns a failed AuthResult into a standard error response (ProblemDetails)
    private ObjectResult ToProblem(AuthResult result)
    {
        var (status, title) = result.ErrorType switch
        {
            AuthErrorType.Conflict =>
                (StatusCodes.Status409Conflict, "Account already exists"),

            AuthErrorType.InvalidCredentials =>
                (StatusCodes.Status401Unauthorized, "Login failed"),

            _ =>
                (StatusCodes.Status400BadRequest, "Invalid request")
        };

        return Problem(
            statusCode: status,
            title: title,
            detail: string.Join(" ", result.Errors));
    }
}