namespace InterviewAi.Api.Models;

public class PreparationDay
{
    public int Id { get; set; }

    public Guid InterviewReportId { get; set; }

    public int DayNumber { get; set; }

    public required string Focus { get; set; }

    public List<string> Tasks { get; set; } = [];
}