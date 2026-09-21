using Microsoft.AspNetCore.Identity;

namespace InterviewAi.Api.Models;

/// <summary>
/// A registered user of the application.
/// Inherits Id, UserName, Email, PasswordHash, lockout fields, etc. from IdentityUser.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; }
}