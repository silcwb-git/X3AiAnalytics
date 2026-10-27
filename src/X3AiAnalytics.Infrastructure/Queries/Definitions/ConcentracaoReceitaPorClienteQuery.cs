using Microsoft.EntityFrameworkCore;
using X3AiAnalytics.Infrastructure.Data;
using X3AiAnalytics.Infrastructure.Queries;

namespace X3AiAnalytics.Infrastructure.Queries.Definitions;

public class ConcentracaoReceitaPorClienteQuery : QueryBase
{
    public ConcentracaoReceitaPorClienteQuery(AppDbContext db) : base(db) { }

    public override string Name => "concentracao_receita_por_cliente";
    public override string Description =>
        "Retorna os clientes que mais concentram receita, com o percentual de participação no total. Use quando o usuário perguntar sobre concentração de receita, risco de dependência de poucos clientes ou participação percentual.";
    public override string Domain => "Receitas";

    public override IReadOnlyList<QueryParameter> Parameters => new[]
    {
        new QueryParameter("topN", "int", "Quantidade de clientes a retornar (padrão 10)")
    };

    protected override async Task<List<Dictionary<string, object?>>> RunAsync(
        QueryParameters parameters, CancellationToken ct)
    {
        var topN = parameters.GetInt("topN") ?? 10;

        var total = await Db.Faturas.SumAsync(f => f.Valor, ct);

        var dados = await Db.Faturas
            .GroupBy(f => new { f.ClienteId, f.Cliente!.Nome })
            .Select(g => new
            {
                Cliente = g.Key.Nome,
                Receita = g.Sum(x => x.Valor)
            })
            .OrderByDescending(x => x.Receita)
            .Take(topN)
            .ToListAsync(ct);

        return dados.Select(d => new Dictionary<string, object?>
        {
            ["cliente"] = d.Cliente,
            ["receita"] = d.Receita,
            ["percentualParticipacao"] = total > 0
                ? Math.Round(d.Receita / total * 100, 2)
                : 0m
        }).ToList();
    }
}