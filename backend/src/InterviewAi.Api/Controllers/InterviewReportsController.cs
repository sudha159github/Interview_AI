using InterviewAi.Api.DTOs;
using InterviewAi.Api.Services;

using Microsoft.AspNetCore.Mvc;

namespace InterviewAi.Api.Controllers;

[ApiController]
[Route("api/interview-reports")]
public class InterviewReportsController(InterviewReportService service) : ControllerBase
{
    /// <summary>Generate a new interview report.</summary>
    [HttpPost]
    [ProducesResponseType<InterviewReportDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InterviewReportDto>> Create(
        CreateInterviewReportRequest request,
        CancellationToken cancellationToken)
    {
        var report = await service.GenerateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
    }

    /// <summary>List the current user's reports, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<InterviewReportSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InterviewReportSummaryDto>>> List(
        CancellationToken cancellationToken)
    {
        var reports = await service.ListAsync(cancellationToken);

        return Ok(reports);
    }

    /// <summary>Get one report by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<InterviewReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InterviewReportDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var report = await service.GetByIdAsync(id, cancellationToken);

        return report is null ? NotFound() : Ok(report);
    }
}