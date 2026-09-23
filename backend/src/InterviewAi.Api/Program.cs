using System.Text;

using InterviewAi.Api.AI;
using InterviewAi.Api.Data;
using InterviewAi.Api.Documents;
using InterviewAi.Api.Models;
using InterviewAi.Api.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database ----------

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------- JWT settings (validated when the app starts) ----------

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

if (string.IsNullOrWhiteSpace(jwt.SigningKey))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. Set it with 'dotnet user-secrets set'.");
}

// ---------- Identity (users, password hashing, lockout) ----------

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

// ---------- Authentication (who are you?) + Authorization (what may you do?) ----------

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names exactly as in the token ("sub", "email")
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ---------- AI provider: Groq, Gemini, or a deterministic fake ----------

var aiProvider = builder.Configuration["Ai:Provider"] ?? "Fake";

switch (aiProvider.ToLowerInvariant())
{
    case "groq":
        builder.Services.AddOptions<GroqOptions>()
            .Bind(builder.Configuration.GetSection(GroqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddHttpClient<IInterviewReportGenerator, GroqInterviewReportGenerator>((serviceProvider, client) =>
            {
                var groq = serviceProvider.GetRequiredService<IOptions<GroqOptions>>().Value;

                client.BaseAddress = new Uri(groq.BaseUrl);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", groq.ApiKey);

                client.Timeout = Timeout.InfiniteTimeSpan;   // the resilience handler controls timeouts
            })
            .AddStandardResilienceHandler(ConfigureAiResilience);
        break;

    case "gemini":
        builder.Services.AddOptions<GeminiOptions>()
            .Bind(builder.Configuration.GetSection(GeminiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddHttpClient<IInterviewReportGenerator, GeminiInterviewReportGenerator>((serviceProvider, client) =>
            {
                var gemini = serviceProvider.GetRequiredService<IOptions<GeminiOptions>>().Value;

                client.BaseAddress = new Uri(gemini.BaseUrl);

                // API key in a header, never in the URL (URLs end up in logs)
                client.DefaultRequestHeaders.Add("x-goog-api-key", gemini.ApiKey);

                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler(ConfigureAiResilience);
        break;

    default:
        builder.Services.AddScoped<IInterviewReportGenerator, FakeInterviewReportGenerator>();
        break;
}

// ---------- Application services ----------

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IResumeTextExtractor, ResumeTextExtractor>();
builder.Services.AddScoped<InterviewReportService>();

builder.Services.AddControllers();

// Generate the OpenAPI description of our endpoints
builder.Services.AddOpenApi();

// ---------- Request pipeline ----------

var app = builder.Build();

// Swagger only in Development: never expose API documentation in production
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "Interview AI API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();   // 1. read and verify the token → who is this?
app.UseAuthorization();    // 2. check [Authorize] → are they allowed?

app.MapControllers();

app.Run();

// AI calls are slow and occasionally fail: same policy for every provider.
// Retries wait a few seconds because free tiers limit requests per second.
static void ConfigureAiResilience(Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions resilience)
{
    resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
    resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(150);

    // Must be at least twice the attempt timeout
    resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(150);

    resilience.Retry.MaxRetryAttempts = 1;
    resilience.Retry.Delay = TimeSpan.FromSeconds(3);
    resilience.Retry.UseJitter = true;
}