namespace X3AiAnalytics.Infrastructure.Contracts;

public record QueryResult
{
    public string QueryName { get; init; } = string.Empty;
    public Dictionary<string, string?> Parameters { get; init; } = new();
    public List<Dictionary<string, object?>> Rows { get; init; } = new();
    public int RowCount => Rows.Count;
    public long ExecutionMs { get; init; }
}