using Microsoft.EntityFrameworkCore;
using X3AiAnalytics.Domain.Enums;
using X3AiAnalytics.Infrastructure.Data;
using X3AiAnalytics.Infrastructure.Queries;

namespace X3AiAnalytics.Infrastructure.Queries.Definitions;

public class TitulosVencidosPorClienteQuery : QueryBase
{
    public TitulosVencidosPorClienteQuery(AppDbContext db) : base(db) { }

    public override string Name => "titulos_vencidos_por_cliente";
    public override string Description =>
        "Lista clientes com títulos a receber vencidos, com o total em atraso e a quantidade de dias do atraso mais antigo. Use quando o usuário perguntar sobre inadimplência, clientes em atraso, títulos vencidos ou contas a receber em aberto.";
    public override string Domain => "Receitas";

    public override IReadOnlyList<QueryParameter> Parameters => new[]
    {
        new QueryParameter("diasMinimosAtraso", "int", "Atraso mínimo em dias (padrão 1)")
    };

    protected override async Task<List<Dictionary<string, object?>>> RunAsync(
        QueryParameters parameters, CancellationToken ct)
    {
        var dias = parameters.GetInt("diasMinimosAtraso") ?? 1;
        var limite = DateTime.UtcNow.AddDays(-dias);

        var dados = await Db.TitulosReceber
            .Where(t => t.Status == StatusTitulo.Vencido && t.DataVencimento <= limite)
            .GroupBy(t => new { t.ClienteId, t.Cliente!.Nome })
            .Select(g => new
            {
                Cliente = g.Key.Nome,
                TotalVencido = g.Sum(x => x.Valor),
                QuantidadeTitulos = g.Count(),
                DiasAtrasoMaximo = g.Max(x => (DateTime.UtcNow - x.DataVencimento).Days)
            })
            .OrderByDescending(x => x.TotalVencido)
            .Take(MaxRows)
            .ToListAsync(ct);

        return dados.Select(d => new Dictionary<string, object?>
        {
            ["cliente"] = d.Cliente,
            ["totalVencido"] = d.TotalVencido,
            ["quantidadeTitulos"] = d.QuantidadeTitulos,
            ["diasAtrasoMaximo"] = d.DiasAtrasoMaximo
        }).ToList();
    }
}