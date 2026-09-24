using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using Polly.CircuitBreaker;
using Polly.Timeout;
namespace InterviewAi.Api.AI;
/// <summary>
/// Scores practice answers with Groq, reusing the same chat-completions shapes
/// as GroqInterviewReportGenerator.
/// </summary>
public class GroqAnswerFeedbackGenerator(
HttpClient httpClient,
IOptions<GroqOptions> options,
ILogger<GroqAnswerFeedbackGenerator> logger) : IAnswerFeedbackGenerator
{
    private static readonly JsonSerializerOptions ApiJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    private static readonly JsonSerializerOptions FeedbackJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly GroqOptions _options = options.Value;
    public async Task<AiAnswerFeedback> GenerateAsync(
    AnswerFeedbackRequest request,
    CancellationToken cancellationToken)
    {
        var payload = new GroqRequest(
        Model: _options.Model,
        Messages:
        [
        new GroqMessage("system", FeedbackPrompts.SystemInstruction + "\n\n" + FeedbackPrompts.JsonShape),
new GroqMessage("user", FeedbackPrompts.BuildUserPrompt(request))
        ],
        ResponseFormat: new GroqResponseFormat("json_object"));
        GroqResponse? body;
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
            "chat/completions", payload, ApiJsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                "Groq feedback returned HTTP {StatusCode} for model {Model}: {Detail}",
                (int)response.StatusCode, _options.Model, detail);
                throw new AiGenerationException($"The AI service returned HTTP {(int)response.StatusCode}.");
            }
            body = await response.Content.ReadFromJsonAsync<GroqResponse>(ApiJsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            logger.LogWarning(ex, "Groq feedback call failed for model {Model}", _options.Model);
            throw new AiGenerationException("The AI service is unavailable right now.");
        }
        var text = body?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new AiGenerationException("The AI service returned no feedback.");
        }
        AiAnswerFeedback? feedback;
        try
        {
            feedback = JsonSerializer.Deserialize<AiAnswerFeedback>(ExtractJson(text), FeedbackJsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Groq returned feedback JSON that doesn't match the expected shape");
            throw new AiGenerationException("The AI service returned unreadable feedback.");
        }
        if (feedback is null ||
        feedback.Strengths is null ||
        feedback.Improvements is null ||
        feedback.MissingKeywords is null ||
        string.IsNullOrWhiteSpace(feedback.StarAssessment))
        {
            throw new AiGenerationException("The AI feedback is missing required sections.");
        }
        return feedback;
    }
    /// <summary>Returns the outermost JSON object in the text, ignoring any surrounding prose.</summary>
    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }
}