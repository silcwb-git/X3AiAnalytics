using Microsoft.EntityFrameworkCore;
using X3AiAnalytics.Domain.Enums;
using X3AiAnalytics.Infrastructure.Data;
using X3AiAnalytics.Infrastructure.Queries;

namespace X3AiAnalytics.Infrastructure.Queries.Definitions;

public class OrdensProducaoAtrasadasQuery : QueryBase
{
    public OrdensProducaoAtrasadasQuery(AppDbContext db) : base(db) { }

    public override string Name => "ordens_producao_atrasadas";
    public override string Description =>
        "Lista ordens de produção atrasadas com o valor previsto em risco. Use quando o usuário perguntar sobre produção atrasada, ordens pendentes, impacto no faturamento previsto ou status da linha de produção.";
    public override string Domain => "Producao";

    public override IReadOnlyList<QueryParameter> Parameters => Array.Empty<QueryParameter>();

    protected override async Task<List<Dictionary<string, object?>>> RunAsync(
        QueryParameters parameters, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow;

        var dados = await Db.OrdensProducao
            .Where(o => o.Status == StatusProducao.Atrasada)
            .OrderBy(o => o.DataPrevisao)
            .Select(o => new
            {
                Ordem = o.Numero,
                Produto = o.Produto,
                Linha = o.Linha,
                DataPrevisao = o.DataPrevisao,
                Quantidade = o.Quantidade,
                ValorPrevisto = o.ValorPrevisto
            })
            .Take(MaxRows)
            .ToListAsync(ct);

        return dados.Select(d => new Dictionary<string, object?>
        {
            ["ordem"] = d.Ordem,
            ["produto"] = d.Produto,
            ["linha"] = d.Linha,
            ["dataPrevisao"] = d.DataPrevisao.ToString("yyyy-MM-dd"),
            ["diasAtraso"] = (hoje - d.DataPrevisao).Days,
            ["quantidade"] = d.Quantidade,
            ["valorPrevisto"] = d.ValorPrevisto
        }).ToList();
    }
}