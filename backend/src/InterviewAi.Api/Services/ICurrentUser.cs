namespace InterviewAi.Api.Services;
/// <summary>
/// Provides information about the user making the current request.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    /// <summary>When the current session started, taken from the token's "sst" claim.</summary>
    DateTimeOffset? SessionStartedAt { get; }
}