using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using Polly.CircuitBreaker;
using Polly.Timeout;

namespace InterviewAi.Api.AI;

/// <summary>
/// Generates interview reports with Google Gemini (structured JSON output).
/// </summary>
public class GeminiInterviewReportGenerator(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiInterviewReportGenerator> logger) : IInterviewReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

    private readonly GeminiOptions _options = options.Value;

    public async Task<AiInterviewReportResult> GenerateAsync(
        string jobDescription,
        string? resumeText,
        string? selfDescription,
        int planDays,
        CancellationToken cancellationToken)
    {
        // 1. Build the request: rules + data + required output shape
        var request = new GeminiRequest(
            SystemInstruction: new GeminiContent(
                null,
                [
                    new GeminiPart(
                        GeminiPrompts.SystemInstruction)
                ]),

            Contents:
            [
                new GeminiContent(
                    "user",
                    [
                        new GeminiPart(
                            GeminiPrompts.BuildUserPrompt(
                                jobDescription,
                                resumeText,
                                selfDescription,
                                planDays))
                    ])
            ],

            GenerationConfig: new GeminiGenerationConfig(
                "application/json",
                GeminiPrompts.ResponseSchema));

        var started = Stopwatch.GetTimestamp();

        // 2. Call Gemini
        // Timeouts / retries / circuit breaker come from the resilience handler.
        GeminiResponse? body;

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"models/{_options.Model}:generateContent",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Gemini returned HTTP {StatusCode} for model {Model}",
                    (int)response.StatusCode,
                    _options.Model);

                throw new AiGenerationException(
                    $"The AI service returned HTTP {(int)response.StatusCode}.");
            }

            body = await response.Content.ReadFromJsonAsync<GeminiResponse>(
                JsonOptions,
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is HttpRequestException
            or TimeoutRejectedException
            or BrokenCircuitException)
        {
            logger.LogWarning(
                ex,
                "Gemini call failed for model {Model}",
                _options.Model);

            throw new AiGenerationException(
                "The AI service is unavailable right now.");
        }

        // 3. Extract the JSON text
        // Skip "thought" parts from thinking models.
        var candidate = body?.Candidates?.FirstOrDefault();

        var text = string.Concat(
            candidate?.Content?.Parts?
                .Where(p => p.Thought != true)
                .Select(p => p.Text)
            ?? []);

        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning(
                "Gemini returned no text. Finish reason: {FinishReason}. " +
                "Block reason: {BlockReason}",
                candidate?.FinishReason,
                body?.PromptFeedback?.BlockReason);

            throw new AiGenerationException(
                "The AI service returned no content.");
        }

        // 4. Parse the report JSON
        AiInterviewReportResult? result;

        try
        {
            result = JsonSerializer.Deserialize<AiInterviewReportResult>(
                text,
                JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Gemini returned JSON that doesn't match the expected shape");

            throw new AiGenerationException(
                "The AI service returned an unreadable report.");
        }

        // 5. Make sure no section is missing
        // Detailed checks happen in InterviewReportService.
        if (result is null ||
            result.TechnicalQuestions is null ||
            result.BehavioralQuestions is null ||
            result.SkillGaps is null ||
            result.PreparationPlan is null ||
            result.PreparationPlan.Any(day => day.Tasks is null))
        {
            throw new AiGenerationException(
                "The AI report is missing required sections.");
        }

        // 6. Log timing and usage only.
        // Never log the prompt or the output because they may contain
        // personal candidate data.
        logger.LogInformation(
            "Gemini generated a report in {ElapsedMs} ms using {Model} " +
            "({PromptTokens} prompt tokens, {OutputTokens} output tokens)",
            (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            _options.Model,
            body?.UsageMetadata?.PromptTokenCount,
            body?.UsageMetadata?.CandidatesTokenCount);

        return result;
    }
}