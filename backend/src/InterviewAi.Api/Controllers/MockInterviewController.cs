using InterviewAi.Api.AI;
using InterviewAi.Api.DTOs;
using InterviewAi.Api.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace InterviewAi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/interview-reports/{reportId:guid}/mock-answers")]
public class MockInterviewController(
MockInterviewService service,
ILogger<MockInterviewController> logger) : ControllerBase
{
    /// <summary>Submit a practice answer and get AI feedback.</summary>
    [HttpPost]
    [ProducesResponseType<MockAnswerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MockAnswerDto>> Submit(
    Guid reportId,
    SubmitMockAnswerRequest request,
    CancellationToken cancellationToken)
    {
        try
        {
            var answer = await service.SubmitAnswerAsync(reportId, request, cancellationToken);
            return answer is null ? NotFound() : StatusCode(StatusCodes.Status201Created, answer);
        }
        catch (AiGenerationException ex)
        {
            logger.LogWarning(ex, "Scoring a practice answer failed");
            return Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "AI service problem",
            detail: "We couldn't score your answer right now. Please try again in a moment.");
        }
    }
    /// <summary>All practice answers for this report.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MockAnswerDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MockAnswerDto>>> List(
    Guid reportId,
    CancellationToken cancellationToken)
    {
        var answers = await service.ListAnswersAsync(reportId, cancellationToken);
        return answers is null ? NotFound() : Ok(answers);
    }
}