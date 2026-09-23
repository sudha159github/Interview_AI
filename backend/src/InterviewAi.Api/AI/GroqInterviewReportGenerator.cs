using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using Polly.CircuitBreaker;
using Polly.Timeout;

namespace InterviewAi.Api.AI;

// ---------- Groq API shapes (OpenAI-compatible chat completions) ----------

internal sealed record GroqRequest(
    string Model,
    IReadOnlyList<GroqMessage> Messages,
    GroqResponseFormat ResponseFormat,
    double Temperature = 0.3);

internal sealed record GroqMessage(
    string Role,
    string? Content);

internal sealed record GroqResponseFormat(
    string Type);

internal sealed record GroqResponse(
    IReadOnlyList<GroqChoice>? Choices,
    GroqUsage? Usage);

internal sealed record GroqChoice(
    GroqMessage? Message,
    string? FinishReason);

internal sealed record GroqUsage(
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens);

/// <summary>
/// Generates interview reports with Groq (OpenAI-compatible chat completions, JSON mode).
/// </summary>
public class GroqInterviewReportGenerator(
    HttpClient httpClient,
    IOptions<GroqOptions> options,
    ILogger<GroqInterviewReportGenerator> logger) : IInterviewReportGenerator
{
    private static readonly JsonSerializerOptions ApiJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,   // Groq uses snake_case
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions ReportJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GroqOptions _options = options.Value;

    public async Task<AiInterviewReportResult> GenerateAsync(
        string jobDescription,
        string? resumeText,
        string? selfDescription,
        CancellationToken cancellationToken)
    {
        // 1. Build the request. JSON mode requires the word "JSON" in the prompt,
        //    so we append an explicit shape description to the system rules.
        var request = new GroqRequest(
            Model: _options.Model,
            Messages:
            [
                new GroqMessage("system", GeminiPrompts.SystemInstruction + "\n\n" + GroqPrompts.JsonShape),
                new GroqMessage("user", GeminiPrompts.BuildUserPrompt(jobDescription, resumeText, selfDescription))
            ],
            ResponseFormat: new GroqResponseFormat("json_object"));

        var started = Stopwatch.GetTimestamp();

        // 2. Call Groq (timeouts / retries / circuit breaker come from the resilience handler)
        GroqResponse? body;
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "chat/completions", request, ApiJsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);

                logger.LogWarning(
                    "Groq returned HTTP {StatusCode} for model {Model}: {Detail}",
                    (int)response.StatusCode, _options.Model, detail);

                throw new AiGenerationException($"The AI service returned HTTP {(int)response.StatusCode}.");
            }

            body = await response.Content.ReadFromJsonAsync<GroqResponse>(ApiJsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            logger.LogWarning(ex, "Groq call failed for model {Model}", _options.Model);
            throw new AiGenerationException("The AI service is unavailable right now.");
        }

        // 3. Take the JSON text out of the response
        var choice = body?.Choices?.FirstOrDefault();
        var text = choice?.Message?.Content;

        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning("Groq returned no content. Finish reason: {FinishReason}", choice?.FinishReason);
            throw new AiGenerationException("The AI service returned no content.");
        }

        // 4. Parse the report. Reasoning models sometimes wrap JSON in prose or code fences,
        //    so take the outermost { ... } block rather than assuming the whole string is JSON.
        AiInterviewReportResult? result;
        var json = ExtractJson(text);

        try
        {
            result = JsonSerializer.Deserialize<AiInterviewReportResult>(json, ReportJsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Groq returned JSON that doesn't match the expected shape (first 200 characters: {Preview})",
                json.Length <= 200 ? json : json[..200]);

            throw new AiGenerationException("The AI service returned an unreadable report.");
        }

        // 5. Make sure no section is missing (detailed checks happen in InterviewReportService)
        if (result is null ||
            result.TechnicalQuestions is null ||
            result.BehavioralQuestions is null ||
            result.SkillGaps is null ||
            result.PreparationPlan is null ||
            result.PreparationPlan.Any(day => day.Tasks is null))
        {
            throw new AiGenerationException("The AI report is missing required sections.");
        }

        // 6. Log timing and usage only — never the prompt or the full output (personal data)
        logger.LogInformation(
            "Groq generated a report in {ElapsedMs} ms using {Model} ({PromptTokens} prompt tokens, {OutputTokens} output tokens)",
            (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            _options.Model,
            body?.Usage?.PromptTokens,
            body?.Usage?.CompletionTokens);

        return result;
    }

    /// <summary>Returns the outermost JSON object in the text, ignoring any surrounding prose.</summary>
    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }
}