namespace X3AiAnalytics.Application.Ai.Contracts;

public record IntentResult
{
    public bool IsInScope { get; init; }
    public string Domain { get; init; } = "Unknown";
    public string Reason { get; init; } = string.Empty;
}

public record PlanResult
{
    public bool IsInScope { get; init; }
    public string? QueryName { get; init; }
    public Dictionary<string, string?> Parameters { get; init; } = new();
    public string Reason { get; init; } = string.Empty;
}

public record ComposeResult
{
    public string Answer { get; init; } = string.Empty;
    public bool DataWasEmpty { get; init; }
}