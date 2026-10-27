using X3AiAnalytics.Application.Ai.Contracts;

namespace X3AiAnalytics.Application.Ai.Providers;

public interface IAiProvider
{
    string Name { get; }
    Task<IntentResult> ClassifyAsync(string question, CancellationToken ct = default);
    Task<PlanResult> PlanAsync(string question, string catalogDescription, CancellationToken ct = default);
    Task<ComposeResult> ComposeAsync(string question, string jsonData, CancellationToken ct = default);
}