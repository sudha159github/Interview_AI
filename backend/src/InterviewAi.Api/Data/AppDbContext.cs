using InterviewAi.Api.Models;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InterviewAi.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    public DbSet<InterviewReport> InterviewReports => Set<InterviewReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity's own tables (users, logins, claims, tokens) must be configured first
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}