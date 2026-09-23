namespace InterviewAi.Api.AI;

/// <summary>
/// Groq JSON mode guarantees valid JSON but takes no schema,
/// so the required shape is described in the prompt.
/// </summary>
internal static class GroqPrompts
{
    public const string JsonShape =
        """
        Respond with a single JSON object, and nothing else, in exactly this shape:

        {
          "title": "string",
          "matchScore": 0,
          "technicalQuestions": [
            { "question": "string", "intention": "string", "suggestedAnswer": "string" }
          ],
          "behavioralQuestions": [
            { "question": "string", "intention": "string", "suggestedAnswer": "string" }
          ],
          "skillGaps": [
            { "skill": "string", "severity": "Low" }
          ],
          "preparationPlan": [
            { "day": 1, "focus": "string", "tasks": ["string"] }
          ]
        }

        Use exactly these property names. "matchScore" and "day" are integers.
        "severity" must be exactly "Low", "Medium" or "High".
        """;
}