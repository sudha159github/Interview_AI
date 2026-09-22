using Microsoft.IdentityModel.JsonWebTokens;

namespace InterviewAi.Api.Services;

/// <summary>
/// Reads the current user's id from the validated JWT on the current request.
/// </summary>
public class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User
                .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new InvalidOperationException("No authenticated user on the current request.");
        }
    }
}
