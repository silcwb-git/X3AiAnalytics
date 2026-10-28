using X3AiAnalytics.Application.Ai.Providers;
using X3AiAnalytics.Infrastructure.Ai;
using X3AiAnalytics.Infrastructure.Queries;
using X3AiAnalytics.Infrastructure.Queries.Definitions;

namespace X3AiAnalytics.Tests;

public class OrchestratorTests
{
    private static (AiOrchestrator orchestrator, IDisposable db) Create()
    {
        var db = TestDbFactory.Create();

        var catalog = new QueryCatalog(new IQueryDefinition[]
        {
            new FaturamentoPorClientePeriodoQuery(db),
            new TitulosVencidosPorClienteQuery(db),
            new EvolucaoReceitaMensalQuery(db),
            new ContasPagarVencidasPorCentroCustoQuery(db),
            new OrdensProducaoAtrasadasQuery(db),
            new TicketMedioPorRegiaoQuery(db),
            new ConcentracaoReceitaPorClienteQuery(db),
            new EvolucaoContasPagarQuery(db)
        });

        return (new AiOrchestrator(new DeterministicAiProvider(), catalog), db);
    }

    [Fact]
    public async Task Ask_OutOfScope_ShouldNever_ReturnData()
    {
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Qual a previsão do tempo em Curitiba?");

            Assert.True(response.OutOfScope);
            Assert.Null(response.Data);
            Assert.Null(response.Trace.QueryUsed);
        }
    }

    [Fact]
    public async Task Ask_InScope_ShouldReturn_DataAndQueryName()
    {
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Quais clientes têm títulos vencidos há mais de 60 dias?");

            Assert.False(response.OutOfScope);
            Assert.Equal("titulos_vencidos_por_cliente", response.Trace.QueryUsed);
            Assert.Equal("60", response.Trace.Parameters["diasMinimosAtraso"]);
            Assert.True(response.Trace.RowCount > 0);
            Assert.NotNull(response.Data);
        }
    }

    [Fact]
    public async Task Ask_ShouldNever_CiteValue_AbsentFromData()
    {
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Quais clientes têm títulos vencidos há mais de 60 dias?");

            Assert.False(response.OutOfScope);
            Assert.NotNull(response.Data);

            // O Data é List<Dictionary<string, object?>). Extrai os valores monetários do dataset.
            var rows = (IEnumerable<Dictionary<string, object?>>)response.Data!;

            var valoresNoDataset = rows
                .SelectMany(r => r.Values)
                .Where(v => v is decimal)
                .Select(v => ((decimal)v).ToString("F2"))
                .ToList();

            Assert.NotEmpty(valoresNoDataset);

            // A resposta do fallback contém o JSON serializado; nenhum valor monetário
            // pode aparecer nele sem existir no dataset de origem.
            var numerosNaResposta = System.Text.RegularExpressions.Regex
                .Matches(response.Answer, @"\d+[\.,]\d{2}")
                .Select(m => m.Value.Replace(',', '.'))
                .ToList();

            foreach (var numero in numerosNaResposta)
            {
                Assert.Contains(valoresNoDataset, v => v.Replace(',', '.') == numero);
            }
        }
    }

    [Fact]
    public async Task Ask_ShouldRoute_ToFaturamento_WhenQuestionIsAboutRevenue()
    {
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Qual o ranking de faturamento por cliente?");

            Assert.False(response.OutOfScope);
            Assert.Equal("faturamento_por_cliente_periodo", response.Trace.QueryUsed);
        }
    }
}