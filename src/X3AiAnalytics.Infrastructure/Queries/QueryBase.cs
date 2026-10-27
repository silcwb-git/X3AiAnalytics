using System.Diagnostics;
using X3AiAnalytics.Infrastructure.Contracts;
using X3AiAnalytics.Infrastructure.Data;

namespace X3AiAnalytics.Infrastructure.Queries;

public abstract class QueryBase : IQueryDefinition
{
    protected readonly AppDbContext Db;

    protected QueryBase(AppDbContext db)
    {
        Db = db;
    }

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Domain { get; }
    public abstract IReadOnlyList<QueryParameter> Parameters { get; }

    protected const int MaxRows = 200;

    protected abstract Task<List<Dictionary<string, object?>>> RunAsync(
        QueryParameters parameters,
        CancellationToken ct);

    public async Task<QueryResult> ExecuteAsync(QueryParameters parameters, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var rows = await RunAsync(parameters, ct);

        if (rows.Count > MaxRows)
        {
            rows = rows.Take(MaxRows).ToList();
        }

        sw.Stop();

        return new QueryResult
        {
            QueryName = Name,
            Parameters = parameters.Values,
            Rows = rows,
            ExecutionMs = sw.ElapsedMilliseconds
        };
    }
}