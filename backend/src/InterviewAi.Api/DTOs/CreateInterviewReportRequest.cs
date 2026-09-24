using System.ComponentModel.DataAnnotations;
namespace InterviewAi.Api.DTOs;
/// <summary>
/// What the client sends to generate a new interview report.
/// A job description is always required, plus a resume file and/or a self-description.
/// </summary>
public record CreateInterviewReportRequest : IValidatableObject
{
    /// <summary>Largest resume file we accept (5 MB).</summary>
    public const long MaxResumeBytes = 5 * 1024 * 1024;
    [Required]
    [StringLength(5000, MinimumLength = 50)]
    public string JobDescription { get; init; } = string.Empty;
    [StringLength(3000, MinimumLength = 20)]
    public string? SelfDescription { get; init; }
    /// <summary>Optional resume upload (PDF or DOCX).</summary>
    public IFormFile? Resume { get; init; }
    // ---------- application tracking ----------
    [StringLength(200)]
    public string? CompanyName { get; init; }
    public DateTimeOffset? InterviewDate { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Resume is null && string.IsNullOrWhiteSpace(SelfDescription))
        {
            yield return new ValidationResult(
            "Please upload your resume or describe yourself in 'About you'.",
            [nameof(Resume), nameof(SelfDescription)]);
        }
        if (Resume is { Length: 0 })
        {
            yield return new ValidationResult("The uploaded file is empty.", [nameof(Resume)]);
        }
        if (Resume is not null && Resume.Length > MaxResumeBytes)
        {
            yield return new ValidationResult("The resume must be 5 MB or smaller.", [nameof(Resume)]);
        }
        if (InterviewDate is not null)
        {
            var date = InterviewDate.Value.UtcDateTime.Date;
            if (date < DateTime.UtcNow.Date)
            {
                yield return new ValidationResult(
                "The interview date cannot be in the past.", [nameof(InterviewDate)]);
            }
            if (date > DateTime.UtcNow.Date.AddYears(1))
            {
                yield return new ValidationResult(
                "The interview date must be within the next year.", [nameof(InterviewDate)]);
            }
        }
    }
}