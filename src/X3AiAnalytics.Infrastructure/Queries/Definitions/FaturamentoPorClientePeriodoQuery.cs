using Microsoft.EntityFrameworkCore;
using X3AiAnalytics.Infrastructure.Data;
using X3AiAnalytics.Infrastructure.Queries;

namespace X3AiAnalytics.Infrastructure.Queries.Definitions;

public class FaturamentoPorClientePeriodoQuery : QueryBase
{
    public FaturamentoPorClientePeriodoQuery(AppDbContext db) : base(db) { }

    public override string Name => "faturamento_por_cliente_periodo";
    public override string Description =>
        "Retorna o faturamento total por cliente em um período. Use quando o usuário perguntar quanto cada cliente faturou, qual o maior cliente em receita, ou ranking de clientes por vendas.";
    public override string Domain => "Vendas";

    public override IReadOnlyList<QueryParameter> Parameters => new[]
    {
        new QueryParameter("dataInicio", "date", "Data inicial do período (yyyy-MM-dd)"),
        new QueryParameter("dataFim", "date", "Data final do período (yyyy-MM-dd)")
    };

    protected override async Task<List<Dictionary<string, object?>>> RunAsync(
        QueryParameters parameters, CancellationToken ct)
    {
        var inicio = parameters.GetDate("dataInicio") ?? DateTime.UtcNow.AddMonths(-6);
        var fim = parameters.GetDate("dataFim") ?? DateTime.UtcNow;

        var dados = await Db.Faturas
            .Where(f => f.DataEmissao >= inicio && f.DataEmissao <= fim)
            .GroupBy(f => new { f.ClienteId, f.Cliente!.Nome })
            .Select(g => new
            {
                Cliente = g.Key.Nome,
                TotalFaturado = g.Sum(x => x.Valor),
                QuantidadeFaturas = g.Count()
            })
            .OrderByDescending(x => x.TotalFaturado)
            .Take(MaxRows)
            .ToListAsync(ct);

        return dados.Select(d => new Dictionary<string, object?>
        {
            ["cliente"] = d.Cliente,
            ["totalFaturado"] = d.TotalFaturado,
            ["quantidadeFaturas"] = d.QuantidadeFaturas
        }).ToList();
    }
}