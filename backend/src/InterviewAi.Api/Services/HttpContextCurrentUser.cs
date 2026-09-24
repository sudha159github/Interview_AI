using System.Globalization;

using Microsoft.IdentityModel.JsonWebTokens;
namespace InterviewAi.Api.Services;
/// <summary>
/// Reads the current user from the validated JWT on the current request.
/// </summary>
public class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var value = Claim(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("No authenticated user on the current request.");
        }
    }
    public DateTimeOffset? SessionStartedAt
    {
        get
        {
            var value = Claim(TokenService.SessionStartClaim);
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
        }
    }
    private string? Claim(string type) =>
    httpContextAccessor.HttpContext?.User.FindFirst(type)?.Value;
}