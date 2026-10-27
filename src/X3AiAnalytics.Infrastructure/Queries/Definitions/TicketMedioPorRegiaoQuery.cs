using Microsoft.EntityFrameworkCore;
using X3AiAnalytics.Infrastructure.Data;
using X3AiAnalytics.Infrastructure.Queries;

namespace X3AiAnalytics.Infrastructure.Queries.Definitions;

public class TicketMedioPorRegiaoQuery : QueryBase
{
    public TicketMedioPorRegiaoQuery(AppDbContext db) : base(db) { }

    public override string Name => "ticket_medio_por_regiao";
    public override string Description =>
        "Retorna o ticket médio das faturas por região do cliente. Use quando o usuário perguntar sobre ticket médio, valor médio por venda ou comparação entre regiões.";
    public override string Domain => "Vendas";

    public override IReadOnlyList<QueryParameter> Parameters => Array.Empty<QueryParameter>();

    protected override async Task<List<Dictionary<string, object?>>> RunAsync(
        QueryParameters parameters, CancellationToken ct)
    {
        var dados = await Db.Faturas
            .GroupBy(f => f.Cliente!.Regiao)
            .Select(g => new
            {
                Regiao = g.Key,
                TicketMedio = g.Average(x => x.Valor),
                QuantidadeFaturas = g.Count()
            })
            .OrderByDescending(x => x.TicketMedio)
            .ToListAsync(ct);

        return dados.Select(d => new Dictionary<string, object?>
        {
            ["regiao"] = d.Regiao,
            ["ticketMedio"] = Math.Round(d.TicketMedio, 2),
            ["quantidadeFaturas"] = d.QuantidadeFaturas
        }).ToList();
    }
}