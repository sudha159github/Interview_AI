using System.ComponentModel.DataAnnotations;

namespace InterviewAi.Api.DTOs;

/// <summary>
/// What the client sends to generate a new interview report.
/// </summary>
public record CreateInterviewReportRequest
{
    [Required]
    [StringLength(5000, MinimumLength = 50)]
    public string JobDescription { get; init; } = string.Empty;

    [Required]
    [StringLength(3000, MinimumLength = 20)]
    public string SelfDescription { get; init; } = string.Empty;
}