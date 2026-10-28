using X3AiAnalytics.Application.Ai.Providers;
using X3AiAnalytics.Infrastructure.Ai;
using X3AiAnalytics.Infrastructure.Data;
using X3AiAnalytics.Infrastructure.Queries;
using X3AiAnalytics.Infrastructure.Queries.Definitions;

namespace X3AiAnalytics.Tests;

public class OrchestratorTests
{
    private static (AiOrchestrator orchestrator, AppDbContext db) Create()
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

    // ---------------------------------------------------------------------
    // Escopo
    // ---------------------------------------------------------------------

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

    [Theory]
    [InlineData("Qual a previsão do tempo em Curitiba?")]
    [InlineData("Qual a cotação do dólar hoje?")]
    [InlineData("Me conte uma piada")]
    [InlineData("Quem ganhou o jogo ontem?")]
    public async Task Ask_OutOfScope_ShouldAlways_FlagOutOfScope(string question)
    {
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync(question);

            Assert.True(response.OutOfScope);
            Assert.Null(response.Data);
            Assert.Equal("high", response.Confidence);
        }
    }

    // ---------------------------------------------------------------------
    // Roteamento e parâmetros
    // ---------------------------------------------------------------------

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

    [Fact]
    public async Task Ask_ShouldRoute_ToTitulos_NotFaturamento_WhenQuestionMentionsOverdue()
    {
        // Regressão do bug de empate de pontuação: "cliente" não pode vencer "vencido".
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Quais clientes têm títulos vencidos há mais de 60 dias?");

            Assert.Equal("titulos_vencidos_por_cliente", response.Trace.QueryUsed);
            Assert.NotEqual("faturamento_por_cliente_periodo", response.Trace.QueryUsed);
        }
    }

    // ---------------------------------------------------------------------
    // Garantia anti-alucinação — núcleo do projeto
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Ask_StructuredData_ShouldMatch_DirectQueryExecution()
    {
        // A fonte de verdade é o Data estruturado. Ele deve ser idêntico ao resultado
        // da execução direta da consulta, sem adição, omissão ou alteração de valores.
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Quais clientes têm títulos vencidos há mais de 60 dias?");

            Assert.False(response.OutOfScope);
            Assert.NotNull(response.Data);

            var rows = (IEnumerable<Dictionary<string, object?>>)response.Data!;

            var expected = await new TitulosVencidosPorClienteQuery(db)
                .ExecuteAsync(new QueryParameters(new() { ["diasMinimosAtraso"] = "60" }));

            Assert.Equal(expected.RowCount, rows.Count());

            var esperados = expected.Rows
                .SelectMany(r => r.Values)
                .OfType<decimal>()
                .Select(v => Math.Round(v, 2))
                .OrderBy(v => v)
                .ToList();

            var obtidos = rows
                .SelectMany(r => r.Values)
                .OfType<decimal>()
                .Select(v => Math.Round(v, 2))
                .OrderBy(v => v)
                .ToList();

            Assert.Equal(esperados, obtidos);
        }
    }

    [Fact]
    public async Task Ask_StructuredData_ShouldPreserveRowCount()
    {
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Quais clientes têm títulos vencidos há mais de 60 dias?");

            var rows = (IEnumerable<Dictionary<string, object?>>)response.Data!;

            Assert.Equal(response.Trace.RowCount, rows.Count());
        }
    }

    [Fact]
    public async Task Ask_ShouldNever_ReturnMoreThanMaxRows()
    {
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Quais clientes têm títulos vencidos?");

            var rows = (IEnumerable<Dictionary<string, object?>>)response.Data!;
            Assert.True(rows.Count() <= 200, "A resposta ultrapassou o limite de 200 linhas.");
        }
    }

    [Fact]
    public async Task Ask_StructuredData_ShouldHave_NoNegativeMonetaryValues()
    {
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Quais clientes têm títulos vencidos há mais de 60 dias?");

            var rows = (IEnumerable<Dictionary<string, object?>>)response.Data!;

            var negativos = rows
                .SelectMany(r => r)
                .Where(kv => kv.Value is decimal d && d < 0)
                .Select(kv => $"{kv.Key}={kv.Value}")
                .ToList();

            Assert.Empty(negativos);
        }
    }

    // ---------------------------------------------------------------------
    // Composição determinística
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Ask_Answer_ShouldBeHumanReadable_NotRawJson()
    {
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Quais clientes têm títulos vencidos há mais de 60 dias?");

            Assert.False(string.IsNullOrWhiteSpace(response.Answer));
            Assert.DoesNotContain("[{", response.Answer);
            Assert.DoesNotContain("\"cliente\":", response.Answer);
            Assert.Contains("cliente", response.Answer, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Ask_Answer_ShouldMentionRowCount()
    {
        var (orchestrator, db) = Create();
        using (db)
        {
            var response = await orchestrator.AskAsync("Quais clientes têm títulos vencidos há mais de 60 dias?");

            Assert.Contains(response.Trace.RowCount.ToString(), response.Answer);
        }
    }
}