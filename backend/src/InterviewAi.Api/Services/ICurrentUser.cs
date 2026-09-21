namespace InterviewAi.Api.Services;

/// <summary>
/// Provides the id of the user making the current request.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
}