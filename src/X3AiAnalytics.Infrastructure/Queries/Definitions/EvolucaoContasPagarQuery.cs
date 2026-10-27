using Microsoft.EntityFrameworkCore;
using X3AiAnalytics.Infrastructure.Data;
using X3AiAnalytics.Infrastructure.Queries;

namespace X3AiAnalytics.Infrastructure.Queries.Definitions;

public class EvolucaoContasPagarQuery : QueryBase
{
    public EvolucaoContasPagarQuery(AppDbContext db) : base(db) { }

    public override string Name => "evolucao_contas_pagar";
    public override string Description =>
        "Retorna a evolução mensal das contas a pagar. Use quando o usuário pedir tendência de despesas, evolução de gastos ou comparativo mensal de custos.";
    public override string Domain => "Despesas";

    public override IReadOnlyList<QueryParameter> Parameters => new[]
    {
        new QueryParameter("meses", "int", "Quantidade de meses a considerar (padrão 12)")
    };

    protected override async Task<List<Dictionary<string, object?>>> RunAsync(
        QueryParameters parameters, CancellationToken ct)
    {
        var meses = parameters.GetInt("meses") ?? 12;
        var inicio = DateTime.UtcNow.AddMonths(-meses);

        var dados = await Db.ContasPagar
            .Where(c => c.DataVencimento >= inicio)
            .GroupBy(c => new { c.DataVencimento.Year, c.DataVencimento.Month })
            .Select(g => new
            {
                Ano = g.Key.Year,
                Mes = g.Key.Month,
                TotalDespesas = g.Sum(x => x.Valor),
                QuantidadeContas = g.Count()
            })
            .OrderBy(x => x.Ano).ThenBy(x => x.Mes)
            .ToListAsync(ct);

        return dados.Select(d => new Dictionary<string, object?>
        {
            ["ano"] = d.Ano,
            ["mes"] = d.Mes,
            ["totalDespesas"] = d.TotalDespesas,
            ["quantidadeContas"] = d.QuantidadeContas
        }).ToList();
    }
}