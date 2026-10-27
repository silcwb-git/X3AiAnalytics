using Microsoft.AspNetCore.Mvc;
using X3AiAnalytics.Infrastructure.Queries;

namespace X3AiAnalytics.Api.Controllers;

[ApiController]
[Route("api/v1/queries")]
public class QueriesController : ControllerBase
{
    private readonly QueryCatalog _catalog;

    public QueriesController(QueryCatalog catalog)
    {
        _catalog = catalog;
    }

    [HttpPost("{name}/run")]
    public async Task<IActionResult> Run(
        string name,
        [FromBody] Dictionary<string, string?>? parameters,
        CancellationToken ct)
    {
        if (!_catalog.TryGet(name, out var definition) || definition is null)
        {
            return NotFound(new { error = $"Consulta '{name}' não existe no catálogo." });
        }

        var result = await definition.ExecuteAsync(new QueryParameters(parameters ?? new()), ct);
        return Ok(result);
    }
}