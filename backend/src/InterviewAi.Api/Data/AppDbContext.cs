using InterviewAi.Api.Models;

using Microsoft.EntityFrameworkCore;

namespace InterviewAi.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<InterviewReport> InterviewReports => Set<InterviewReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}