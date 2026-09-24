using System.ComponentModel.DataAnnotations;
namespace InterviewAi.Api.DTOs;
/// <summary>What the client sends when submitting a practice answer.</summary>
public record SubmitMockAnswerRequest
{
    [Required]
    [RegularExpression("^(Technical|Behavioral)$",
    ErrorMessage = "Question type must be Technical or Behavioral.")]
    public string QuestionType { get; init; } = "Technical";
    [Range(0, 50)]
    public int QuestionOrder { get; init; }
    [Required]
    [StringLength(5000, MinimumLength = 20,
    ErrorMessage = "Please write at least 20 characters so the answer can be scored.")]
    public string AnswerText { get; init; } = string.Empty;
}
/// <summary>A saved practice answer with its feedback.</summary>
public record MockAnswerDto(
int Id,
string QuestionType,
int QuestionOrder,
string QuestionText,
string AnswerText,
int Score,
string StarAssessment,
IReadOnlyList<string> Strengths,
IReadOnlyList<string> Improvements,
IReadOnlyList<string> MissingKeywords,
DateTimeOffset CreatedAt);