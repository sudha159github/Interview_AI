using InterviewAi.Api.AI;
using InterviewAi.Api.Data;
using InterviewAi.Api.Services;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------

// Read the connection string from appsettings; stop immediately if it's missing
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

// Register AppDbContext so EF Core can connect to SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// TEMPORARY: every request is the demo user until login exists (Phase 6)
builder.Services.AddScoped<ICurrentUser, DemoCurrentUser>();

// TEMPORARY: fake AI until Gemini is connected (Phase 8)
builder.Services.AddScoped<IInterviewReportGenerator, FakeInterviewReportGenerator>();

// Business logic
builder.Services.AddScoped<InterviewReportService>();

builder.Services.AddControllers();

// Generate the OpenAPI description of our endpoints
builder.Services.AddOpenApi();

// ---------- Request pipeline ----------

var app = builder.Build();

// Swagger only in Development: never expose API documentation in production
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();   // serves the description at /openapi/v1.json
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "Interview AI API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();