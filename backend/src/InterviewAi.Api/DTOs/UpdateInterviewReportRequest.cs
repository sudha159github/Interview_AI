using System.ComponentModel.DataAnnotations;
namespace InterviewAi.Api.DTOs;
/// <summary>What the client sends to update the tracking details of a report.</summary>
public record UpdateInterviewReportRequest
{
    [StringLength(200)]
    public string? CompanyName { get; init; }
    public DateTimeOffset? InterviewDate { get; init; }
    [Required]
    [RegularExpression("^(Planned|Applied|Interviewing|Offer|Rejected)$",
    ErrorMessage = "Status must be Planned, Applied, Interviewing, Offer or Rejected.")]
    public string Status { get; init; } = "Planned";
}