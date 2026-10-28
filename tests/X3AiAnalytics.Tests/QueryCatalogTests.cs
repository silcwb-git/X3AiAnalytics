using X3AiAnalytics.Infrastructure.Queries;
using X3AiAnalytics.Infrastructure.Queries.Definitions;

namespace X3AiAnalytics.Tests;

public class QueryCatalogTests
{
    [Fact]
    public async Task TitulosVencidos_ShouldRespect_DiasMinimosAtraso()
    {
        using var db = TestDbFactory.Create();
        var query = new TitulosVencidosPorClienteQuery(db);

        var result = await query.ExecuteAsync(new QueryParameters(new()
        {
            ["diasMinimosAtraso"] = "60"
        }));

        Assert.True(result.RowCount > 0);
        Assert.All(result.Rows, row =>
        {
            var dias = Convert.ToInt32(row["diasAtrasoMaximo"]);
            Assert.True(dias >= 60, $"Linha com {dias} dias não deveria aparecer com filtro de 60.");
        });
    }

    [Fact]
    public async Task TitulosVencidos_ShouldFilter_MoreStrictly_WithHigherThreshold()
    {
        using var db = TestDbFactory.Create();

        var trinta = await new TitulosVencidosPorClienteQuery(db)
            .ExecuteAsync(new QueryParameters(new() { ["diasMinimosAtraso"] = "30" }));

        var noventа = await new TitulosVencidosPorClienteQuery(db)
            .ExecuteAsync(new QueryParameters(new() { ["diasMinimosAtraso"] = "90" }));

        Assert.True(trinta.RowCount >= noventа.RowCount,
            "Filtro de 30 dias não pode devolver menos linhas que filtro de 90 dias.");
    }

    [Fact]
    public async Task OrdensProducao_ShouldReturn_OrderedByOldestFirst()
    {
        using var db = TestDbFactory.Create();
        var result = await new OrdensProducaoAtrasadasQuery(db)
            .ExecuteAsync(new QueryParameters(new()));

        Assert.True(result.RowCount > 0);

        var atrasos = result.Rows.Select(r => Convert.ToInt32(r["diasAtraso"])).ToList();
        var ordenado = atrasos.OrderByDescending(d => d).ToList();

        Assert.Equal(ordenado, atrasos);
    }

    [Fact]
    public async Task AllQueries_ShouldNot_ExceedMaxRows()
    {
        using var db = TestDbFactory.Create();

        var queries = new IQueryDefinition[]
        {
            new FaturamentoPorClientePeriodoQuery(db),
            new TitulosVencidosPorClienteQuery(db),
            new EvolucaoReceitaMensalQuery(db),
            new ContasPagarVencidasPorCentroCustoQuery(db),
            new OrdensProducaoAtrasadasQuery(db),
            new TicketMedioPorRegiaoQuery(db),
            new ConcentracaoReceitaPorClienteQuery(db),
            new EvolucaoContasPagarQuery(db)
        };

        foreach (var q in queries)
        {
            var result = await q.ExecuteAsync(new QueryParameters(new()));
            Assert.True(result.RowCount <= 200,
                $"{q.Name} devolveu {result.RowCount} linhas, acima do limite de 200.");
        }
    }

    [Fact]
    public async Task ConcentracaoReceita_ShouldSum_To100Percent_OrLess()
    {
        using var db = TestDbFactory.Create();
        var result = await new ConcentracaoReceitaPorClienteQuery(db)
            .ExecuteAsync(new QueryParameters(new() { ["topN"] = "10" }));

        var soma = result.Rows.Sum(r => Convert.ToDecimal(r["percentualParticipacao"]));

        Assert.True(soma <= 100.01m, $"Soma dos percentuais deu {soma}%, deveria ser no máximo 100%.");
    }

    [Fact]
    public async Task EvolucaoReceita_ShouldReturn_ChronologicalOrder()
    {
        using var db = TestDbFactory.Create();
        var result = await new EvolucaoReceitaMensalQuery(db)
            .ExecuteAsync(new QueryParameters(new() { ["meses"] = "12" }));

        Assert.True(result.RowCount > 0);

        var datas = result.Rows
            .Select(r => (Convert.ToInt32(r["ano"]), Convert.ToInt32(r["mes"])))
            .ToList();

        var ordenado = datas.OrderBy(d => d.Item1).ThenBy(d => d.Item2).ToList();
        Assert.Equal(ordenado, datas);
    }
}