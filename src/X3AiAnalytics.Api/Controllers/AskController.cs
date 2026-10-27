using Microsoft.AspNetCore.Mvc;
using X3AiAnalytics.Infrastructure.Ai;

namespace X3AiAnalytics.Api.Controllers;

public record AskRequest(string Question);

[ApiController]
[Route("api/v1/ask")]
public class AskController : ControllerBase
{
    private readonly AiOrchestrator _orchestrator;

    public AskController(AiOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { error = "A pergunta não pode ser vazia." });
        }

        var response = await _orchestrator.AskAsync(request.Question, ct);
        return Ok(response);
    }
}