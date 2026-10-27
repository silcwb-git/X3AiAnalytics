using Microsoft.EntityFrameworkCore;
using X3AiAnalytics.Infrastructure.Data;
using X3AiAnalytics.Infrastructure.Queries;

namespace X3AiAnalytics.Infrastructure.Queries.Definitions;

public class EvolucaoReceitaMensalQuery : QueryBase
{
    public EvolucaoReceitaMensalQuery(AppDbContext db) : base(db) { }

    public override string Name => "evolucao_receita_mensal";
    public override string Description =>
        "Retorna a evolução da receita faturada mês a mês. Use quando o usuário pedir tendência de receita, evolução do faturamento, comparativo mês a mês ou crescimento ao longo do tempo.";
    public override string Domain => "Receitas";

    public override IReadOnlyList<QueryParameter> Parameters => new[]
    {
        new QueryParameter("meses", "int", "Quantidade de meses a considerar (padrão 12)")
    };

    protected override async Task<List<Dictionary<string, object?>>> RunAsync(
        QueryParameters parameters, CancellationToken ct)
    {
        var meses = parameters.GetInt("meses") ?? 12;
        var inicio = DateTime.UtcNow.AddMonths(-meses);

        var dados = await Db.Faturas
            .Where(f => f.DataEmissao >= inicio)
            .GroupBy(f => new { f.DataEmissao.Year, f.DataEmissao.Month })
            .Select(g => new
            {
                Ano = g.Key.Year,
                Mes = g.Key.Month,
                Receita = g.Sum(x => x.Valor),
                QuantidadeFaturas = g.Count()
            })
            .OrderBy(x => x.Ano).ThenBy(x => x.Mes)
            .ToListAsync(ct);

        return dados.Select(d => new Dictionary<string, object?>
        {
            ["ano"] = d.Ano,
            ["mes"] = d.Mes,
            ["receita"] = d.Receita,
            ["quantidadeFaturas"] = d.QuantidadeFaturas
        }).ToList();
    }
}