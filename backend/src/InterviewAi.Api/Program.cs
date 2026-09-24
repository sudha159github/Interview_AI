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
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------- JWT settings ----------

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwt = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "The 'Jwt' configuration section is missing.");

if (string.IsNullOrWhiteSpace(jwt.SigningKey))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. Set it with 'dotnet user-secrets set'.");
}

// ---------- Identity ----------

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

// ---------- Authentication + Authorization ----------

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names exactly as they appear in the token.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.SigningKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ---------- AI provider: Groq or Gemini ----------

var aiProvider = builder.Configuration["Ai:Provider"];

if (string.IsNullOrWhiteSpace(aiProvider))
{
    throw new InvalidOperationException(
        "Ai:Provider is not configured. Set it to 'Groq' or 'Gemini'.");
}

switch (aiProvider.ToLowerInvariant())
{
    case "groq":

        builder.Services.AddOptions<GroqOptions>()
            .Bind(builder.Configuration.GetSection(GroqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddHttpClient<IInterviewReportGenerator, GroqInterviewReportGenerator>(
                (serviceProvider, client) =>
                {
                    var groq = serviceProvider
                        .GetRequiredService<IOptions<GroqOptions>>()
                        .Value;

                    client.BaseAddress = new Uri(groq.BaseUrl);

                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue(
                            "Bearer",
                            groq.ApiKey);

                    // Resilience handler controls the actual timeout.
                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
            .AddStandardResilienceHandler(ConfigureAiResilience);

        break;

    case "gemini":

        builder.Services.AddOptions<GeminiOptions>()
            .Bind(builder.Configuration.GetSection(GeminiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddHttpClient<IInterviewReportGenerator, GeminiInterviewReportGenerator>(
                (serviceProvider, client) =>
                {
                    var gemini = serviceProvider
                        .GetRequiredService<IOptions<GeminiOptions>>()
                        .Value;

                    client.BaseAddress = new Uri(gemini.BaseUrl);

                    // API key is sent in a header, not in the URL.
                    client.DefaultRequestHeaders.Add(
                        "x-goog-api-key",
                        gemini.ApiKey);

                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
            .AddStandardResilienceHandler(ConfigureAiResilience);

        break;

    default:

        throw new InvalidOperationException(
            $"Unknown AI provider '{aiProvider}'. " +
            "Use 'Groq' or 'Gemini'.");
}

// ---------- Application services ----------

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IResumeTextExtractor, ResumeTextExtractor>();
builder.Services.AddScoped<InterviewReportService>();

builder.Services.AddControllers();

// ---------- OpenAPI ----------

builder.Services.AddOpenApi();

// ---------- Request pipeline ----------

var app = builder.Build();

// Swagger/OpenAPI only in Development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Interview AI API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// ---------- AI resilience ----------

static void ConfigureAiResilience(
    Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions resilience)
{
    // Maximum time for one AI attempt.
    resilience.AttemptTimeout.Timeout =
        TimeSpan.FromSeconds(60);

    // Maximum time for the complete request including retry.
    resilience.TotalRequestTimeout.Timeout =
        TimeSpan.FromSeconds(150);

    // Circuit breaker sampling period.
    resilience.CircuitBreaker.SamplingDuration =
        TimeSpan.FromSeconds(150);

    // One retry after a short delay.
    resilience.Retry.MaxRetryAttempts = 1;
    resilience.Retry.Delay = TimeSpan.FromSeconds(3);
    resilience.Retry.UseJitter = true;
}