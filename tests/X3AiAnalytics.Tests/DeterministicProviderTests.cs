using X3AiAnalytics.Application.Ai.Providers;

namespace X3AiAnalytics.Tests;

public class DeterministicProviderTests
{
    private readonly DeterministicAiProvider _ai = new();

    [Theory]
    [InlineData("Quais clientes têm títulos vencidos há mais de 60 dias?", "titulos_vencidos_por_cliente")]
    [InlineData("Qual o ranking de faturamento por cliente?", "faturamento_por_cliente_periodo")]
    [InlineData("Quanto cada cliente faturou no semestre?", "faturamento_por_cliente_periodo")]
    [InlineData("Quais contas a pagar estão vencidas por centro de custo?", "contas_pagar_vencidas_por_centro_custo")]
    [InlineData("Quais ordens de produção estão atrasadas?", "ordens_producao_atrasadas")]
    [InlineData("Qual o ticket médio por região?", "ticket_medio_por_regiao")]
    [InlineData("Qual a concentração de receita por cliente?", "concentracao_receita_por_cliente")]
    [InlineData("Como evoluiu a receita mês a mês?", "evolucao_receita_mensal")]
    public async Task Plan_ShouldRoute_ToCorrectQuery(string question, string expectedQuery)
    {
        var plan = await _ai.PlanAsync(question, string.Empty);

        Assert.True(plan.IsInScope);
        Assert.Equal(expectedQuery, plan.QueryName);
    }

    [Fact]
    public async Task Plan_ShouldNot_SelectFaturamento_WhenQuestionIsAboutOverdueTitles()
    {
        // Regressão do bug de empate de pontuação encontrado em desenvolvimento.
        // "cliente" sozinha não pode vencer "vencido", que é específica de títulos.
        var plan = await _ai.PlanAsync("Quais clientes têm títulos vencidos há mais de 60 dias?", string.Empty);

        Assert.Equal("titulos_vencidos_por_cliente", plan.QueryName);
        Assert.NotEqual("faturamento_por_cliente_periodo", plan.QueryName);
    }

    [Fact]
    public async Task Plan_ShouldExtract_DiasMinimosAtraso_FromQuestion()
    {
        var plan = await _ai.PlanAsync("Quais clientes têm títulos vencidos há mais de 60 dias?", string.Empty);

        Assert.Equal("titulos_vencidos_por_cliente", plan.QueryName);
        Assert.Equal("60", plan.Parameters["diasMinimosAtraso"]);
    }

    [Fact]
    public async Task Plan_ShouldExtract_TopN_FromQuestion()
    {
        var plan = await _ai.PlanAsync("Qual a concentração de receita dos 5 maiores clientes?", string.Empty);

        Assert.Equal("concentracao_receita_por_cliente", plan.QueryName);
        Assert.Equal("5", plan.Parameters["topN"]);
    }

    [Theory]
    [InlineData("Qual a previsão do tempo em Curitiba amanhã?")]
    [InlineData("Qual a cotação do dólar hoje?")]
    [InlineData("Quem ganhou o jogo de futebol ontem?")]
    [InlineData("Me conte uma piada")]
    [InlineData("Qual a receita de bolo de cenoura?")]
    public async Task Classify_ShouldReject_OutOfScopeQuestions(string question)
    {
        var intent = await _ai.ClassifyAsync(question);

        Assert.False(intent.IsInScope);
        Assert.Equal("Unknown", intent.Domain);
    }

    [Fact]
    public async Task Classify_ShouldAccept_InScopeQuestion()
    {
        var intent = await _ai.ClassifyAsync("Quais clientes estão inadimplentes?");

        Assert.True(intent.IsInScope);
        Assert.Equal("Erp", intent.Domain);
    }

    [Fact]
    public async Task Compose_ShouldSignalEmpty_WhenDataIsEmptyArray()
    {
        var result = await _ai.ComposeAsync("Qual o faturamento?", "[]");

        Assert.True(result.DataWasEmpty);
        Assert.Contains("Não encontrei dados", result.Answer);
    }
}