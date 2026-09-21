namespace InterviewAi.Api.Services;

/// <summary>
/// TEMPORARY (Phase 5): every request is treated as the same demo user.
/// Replaced by the real logged-in user in Phase 6.
/// </summary>
public class DemoCurrentUser : ICurrentUser
{
    public static readonly Guid DemoUserId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid UserId => DemoUserId;
}