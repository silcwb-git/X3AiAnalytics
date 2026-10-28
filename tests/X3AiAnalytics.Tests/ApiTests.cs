using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace X3AiAnalytics.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ShouldReturn_Ok()
    {
        var response = await _client.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Queries_ShouldReturn_AllEight()
    {
        var response = await _client.GetAsync("/api/v1/queries");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(8, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task QueryRun_WithUnknownName_ShouldReturn_NotFound()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/queries/consulta_que_nao_existe/run",
            new Dictionary<string, string?>());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task QueryRun_WithKnownName_ShouldReturn_Rows()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/queries/ordens_producao_atrasadas/run",
            new Dictionary<string, string?>());

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetProperty("rowCount").GetInt32() > 0);
    }

    [Fact]
    public async Task Ask_WithEmptyQuestion_ShouldReturn_BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/ask", new { question = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ask_OutOfScope_ShouldMark_OutOfScopeFlag()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/ask",
            new { question = "Qual a previsão do tempo em Curitiba amanhã?" });

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetProperty("outOfScope").GetBoolean());
    }
}