using Microsoft.EntityFrameworkCore;
using X3AiAnalytics.Domain.Enums;
using X3AiAnalytics.Infrastructure.Data;
using X3AiAnalytics.Infrastructure.Queries;

namespace X3AiAnalytics.Infrastructure.Queries.Definitions;

public class ContasPagarVencidasPorCentroCustoQuery : QueryBase
{
    public ContasPagarVencidasPorCentroCustoQuery(AppDbContext db) : base(db) { }

    public override string Name => "contas_pagar_vencidas_por_centro_custo";
    public override string Description =>
        "Retorna o total de contas a pagar vencidas agrupado por centro de custo. Use quando o usuário perguntar sobre despesas em atraso, contas a pagar vencidas ou gastos por área/departamento.";
    public override string Domain => "Despesas";

    public override IReadOnlyList<QueryParameter> Parameters => new[]
    {
        new QueryParameter("diasMinimosAtraso", "int", "Atraso mínimo em dias (padrão 1)")
    };

    protected override async Task<List<Dictionary<string, object?>>> RunAsync(
        QueryParameters parameters, CancellationToken ct)
    {
        var dias = parameters.GetInt("diasMinimosAtraso") ?? 1;
        var limite = DateTime.UtcNow.AddDays(-dias);

        var dados = await Db.ContasPagar
            .Where(c => c.Status == StatusTitulo.Vencido && c.DataVencimento <= limite)
            .GroupBy(c => new { c.CentroCustoId, c.CentroCusto!.Nome })
            .Select(g => new
            {
                CentroCusto = g.Key.Nome,
                TotalVencido = g.Sum(x => x.Valor),
                QuantidadeContas = g.Count()
            })
            .OrderByDescending(x => x.TotalVencido)
            .Take(MaxRows)
            .ToListAsync(ct);

        return dados.Select(d => new Dictionary<string, object?>
        {
            ["centroCusto"] = d.CentroCusto,
            ["totalVencido"] = d.TotalVencido,
            ["quantidadeContas"] = d.QuantidadeContas
        }).ToList();
    }
}