using System.Globalization;
using System.Security.Claims;
using System.Text;

using InterviewAi.Api.Models;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
namespace InterviewAi.Api.Services;
/// <summary>A signed access token and when it expires.</summary>
public record AccessToken(string Token, DateTimeOffset ExpiresAt);
/// <summary>
/// Creates signed JWT access tokens for authenticated users.
/// </summary>
public class TokenService(IOptions<JwtOptions> options)
{
    /// <summary>Custom claim: when this session originally started (unix seconds).</summary>
    public const string SessionStartClaim = "sst";
    private readonly JwtOptions _jwt = options.Value;
    public AccessToken CreateAccessToken(ApplicationUser user, DateTimeOffset? sessionStartedAt = null)
    {
        var sessionStart = sessionStartedAt ?? DateTimeOffset.UtcNow;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var claims = new[]
        {
new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
new Claim(
SessionStartClaim,
sessionStart.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
};
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
        };
        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessToken(token, expiresAt);
    }
}