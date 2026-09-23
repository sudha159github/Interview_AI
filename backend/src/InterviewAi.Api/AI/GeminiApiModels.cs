namespace InterviewAi.Api.AI;

// ---------- Request: POST {BaseUrl}models/{model}:generateContent ----------

internal sealed record GeminiRequest(
    GeminiContent SystemInstruction,
    IReadOnlyList<GeminiContent> Contents,
    GeminiGenerationConfig GenerationConfig);

internal sealed record GeminiContent(
    string? Role,
    IReadOnlyList<GeminiPart> Parts);

internal sealed record GeminiPart(
    string? Text,
    bool? Thought = null);

internal sealed record GeminiGenerationConfig(
    string ResponseMimeType,
    object ResponseSchema);

// ---------- Response ----------

internal sealed record GeminiResponse(
    IReadOnlyList<GeminiCandidate>? Candidates,
    GeminiUsageMetadata? UsageMetadata,
    GeminiPromptFeedback? PromptFeedback);

internal sealed record GeminiCandidate(
    GeminiContent? Content,
    string? FinishReason);

internal sealed record GeminiUsageMetadata(
    int? PromptTokenCount,
    int? CandidatesTokenCount,
    int? TotalTokenCount);

internal sealed record GeminiPromptFeedback(
    string? BlockReason);