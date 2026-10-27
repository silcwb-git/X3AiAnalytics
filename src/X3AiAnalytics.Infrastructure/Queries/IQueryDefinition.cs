using X3AiAnalytics.Infrastructure.Contracts;

namespace X3AiAnalytics.Infrastructure.Queries;

public interface IQueryDefinition
{
    string Name { get; }
    string Description { get; }
    string Domain { get; }
    IReadOnlyList<QueryParameter> Parameters { get; }
    Task<QueryResult> ExecuteAsync(QueryParameters parameters, CancellationToken ct = default);
}

public record QueryParameter(string Name, string Type, string Description);

public record QueryParameters(Dictionary<string, string?> Values)
{
    public int? GetInt(string name)
        => Values.TryGetValue(name, out var v) && int.TryParse(v, out var i) ? i : null;

    public DateTime? GetDate(string name)
        => Values.TryGetValue(name, out var v) && DateTime.TryParse(v, out var d) ? d : null;

    public string? GetString(string name)
        => Values.TryGetValue(name, out var v) ? v : null;
}