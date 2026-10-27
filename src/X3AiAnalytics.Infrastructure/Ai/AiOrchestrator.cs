using System.Text.Json;
using X3AiAnalytics.Application.Ai.Contracts;
using X3AiAnalytics.Application.Ai.Providers;
using X3AiAnalytics.Infrastructure.Contracts;
using X3AiAnalytics.Infrastructure.Queries;

namespace X3AiAnalytics.Infrastructure.Ai;

public record AskResponse
{
    public string Answer { get; init; } = string.Empty;
    public object? Data { get; init; }
    public AskTrace Trace { get; init; } = new();
    public string Confidence { get; init; } = "low";
    public bool OutOfScope { get; init; }
}

public record AskTrace
{
    public string? QueryUsed { get; init; }
    public Dictionary<string, string?> Parameters { get; init; } = new();
    public int RowCount { get; init; }
    public long ExecutionMs { get; init; }
    public string Provider { get; init; } = string.Empty;
}

public class AiOrchestrator
{
    private readonly IAiProvider _ai;
    private readonly QueryCatalog _catalog;

    public AiOrchestrator(IAiProvider ai, QueryCatalog catalog)
    {
        _ai = ai;
        _catalog = catalog;
    }

    public async Task<AskResponse> AskAsync(string question, CancellationToken ct = default)
    {
        var intent = await _ai.ClassifyAsync(question, ct);

        if (!intent.IsInScope)
        {
            return OutOfScope(_ai.Name);
        }

        var plan = await _ai.PlanAsync(question, _catalog.ToPromptDescription(), ct);

        if (!plan.IsInScope || string.IsNullOrWhiteSpace(plan.QueryName))
        {
            return OutOfScope(_ai.Name);
        }

        if (!_catalog.TryGet(plan.QueryName, out var definition) || definition is null)
        {
            return OutOfScope(_ai.Name);
        }

        var result = await definition.ExecuteAsync(new QueryParameters(plan.Parameters), ct);

        if (result.RowCount == 0)
        {
            return new AskResponse
            {
                Answer = "Não encontrei dados para responder a essa pergunta com os filtros informados.",
                Data = Array.Empty<object>(),
                Confidence = "high",
                Trace = new AskTrace
                {
                    QueryUsed = result.QueryName,
                    Parameters = result.Parameters,
                    RowCount = 0,
                    ExecutionMs = result.ExecutionMs,
                    Provider = _ai.Name
                }
            };
        }

        var json = JsonSerializer.Serialize(result.Rows);
        var composed = await _ai.ComposeAsync(question, json, ct);

        return new AskResponse
        {
            Answer = composed.Answer,
            Data = result.Rows,
            Confidence = result.RowCount >= 3 ? "high" : "medium",
            Trace = new AskTrace
            {
                QueryUsed = result.QueryName,
                Parameters = result.Parameters,
                RowCount = result.RowCount,
                ExecutionMs = result.ExecutionMs,
                Provider = _ai.Name
            }
        };
    }

    private static AskResponse OutOfScope(string provider) => new()
    {
        Answer = "Não tenho dados para responder essa pergunta. Posso responder sobre faturamento, títulos a receber, contas a pagar e ordens de produção.",
        OutOfScope = true,
        Confidence = "high",
        Trace = new AskTrace { Provider = provider }
    };
}