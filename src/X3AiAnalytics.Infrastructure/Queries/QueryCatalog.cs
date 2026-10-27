namespace X3AiAnalytics.Infrastructure.Queries;

public class QueryCatalog
{
    private readonly Dictionary<string, IQueryDefinition> _definitions;

    public QueryCatalog(IEnumerable<IQueryDefinition> definitions)
    {
        _definitions = definitions.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<IQueryDefinition> All => _definitions.Values;

    public bool TryGet(string name, out IQueryDefinition? definition)
        => _definitions.TryGetValue(name, out definition);

    public string ToPromptDescription()
    {
        var linhas = _definitions.Values.Select(d =>
        {
            var ps = d.Parameters.Count == 0
                ? "sem parâmetros"
                : string.Join(", ", d.Parameters.Select(p => $"{p.Name} ({p.Type}): {p.Description}"));

            return $"- {d.Name} [{d.Domain}]: {d.Description} Parâmetros: {ps}";
        });

        return string.Join("\n", linhas);
    }
}