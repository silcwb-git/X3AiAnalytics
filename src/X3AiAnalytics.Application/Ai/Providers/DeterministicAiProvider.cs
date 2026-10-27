using System.Text.Json;
using X3AiAnalytics.Application.Ai.Contracts;

namespace X3AiAnalytics.Application.Ai.Providers;

public class DeterministicAiProvider : IAiProvider
{
    public string Name => "deterministic-fallback";

    private static readonly Dictionary<string, string[]> DomainKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["faturamento_por_cliente_periodo"] = new[] { "faturamento", "faturou", "cliente", "vendas", "vendeu" },
        ["titulos_vencidos_por_cliente"] = new[] { "vencido", "inadimpl", "atraso", "atras", "receber" },
        ["evolucao_receita_mensal"] = new[] { "evolu", "receita", "mensal", "tendência", "tendencia", "mês", "mes" },
        ["contas_pagar_vencidas_por_centro_custo"] = new[] { "contas a pagar", "pagar", "despesa", "centro de custo" },
        ["ordens_producao_atrasadas"] = new[] { "produ", "ordem", "ordens", "atrasada", "linha" },
        ["ticket_medio_por_regiao"] = new[] { "ticket", "médio", "medio", "região", "regiao" },
        ["concentracao_receita_por_cliente"] = new[] { "concentra", "participa", "dependência", "dependencia" },
        ["evolucao_contas_pagar"] = new[] { "evolução", "evolucao", "gastos", "custos" }
    };

    private static readonly string[] OutOfScopeKeywords =
    {
        "tempo", "clima", "previsão do tempo", "previsao do tempo", "cotação", "cotacao",
        "dólar", "dolar", "eleição", "eleicao", "notícia", "noticia", "piada",
        "quem é", "quem e", "quem foi", "receita de bolo", "futebol"
    };

    public Task<IntentResult> ClassifyAsync(string question, CancellationToken ct = default)
    {
        var lower = question.ToLowerInvariant();

        if (OutOfScopeKeywords.Any(k => lower.Contains(k)))
        {
            return Task.FromResult(new IntentResult
            {
                IsInScope = false,
                Domain = "Unknown",
                Reason = "Pergunta fora do domínio de dados do ERP."
            });
        }

        var matched = ResolveQuery(lower);
        return Task.FromResult(new IntentResult
        {
            IsInScope = matched != null,
            Domain = matched != null ? "Erp" : "Unknown",
            Reason = matched != null ? "Consulta identificada." : "Nenhuma consulta do catálogo corresponde."
        });
    }

    public Task<PlanResult> PlanAsync(string question, string catalogDescription, CancellationToken ct = default)
    {
        var lower = question.ToLowerInvariant();
        var matched = ResolveQuery(lower);

        if (matched == null)
        {
            return Task.FromResult(new PlanResult
            {
                IsInScope = false,
                Reason = "Nenhuma consulta do catálogo corresponde à pergunta."
            });
        }

        var parameters = new Dictionary<string, string?>();
        var numero = ExtractNumber(lower);

        if (matched == "titulos_vencidos_por_cliente" || matched == "contas_pagar_vencidas_por_centro_custo")
        {
            if (numero.HasValue) parameters["diasMinimosAtraso"] = numero.Value.ToString();
        }
        else if (matched == "concentracao_receita_por_cliente")
        {
            if (numero.HasValue) parameters["topN"] = numero.Value.ToString();
        }

        return Task.FromResult(new PlanResult
        {
            IsInScope = true,
            QueryName = matched,
            Parameters = parameters,
            Reason = "Consulta selecionada por regra determinística."
        });
    }

    public Task<ComposeResult> ComposeAsync(string question, string jsonData, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jsonData) || jsonData.Trim() is "[]" or "{}")
        {
            return Task.FromResult(new ComposeResult
            {
                Answer = "Não encontrei dados para responder a essa pergunta com os filtros informados.",
                DataWasEmpty = true
            });
        }

        return Task.FromResult(new ComposeResult
        {
            Answer = $"Resultados encontrados: {jsonData}",
            DataWasEmpty = false
        });
    }

    private static string? ResolveQuery(string lower)
    {
        string? best = null;
        var bestScore = 0;

        foreach (var (queryName, keywords) in DomainKeywords)
        {
            var score = keywords.Count(k => lower.Contains(k));
            if (score > bestScore)
            {
                bestScore = score;
                best = queryName;
            }
        }

        return bestScore > 0 ? best : null;
    }

    private static int? ExtractNumber(string lower)
    {
        var digits = new string(lower.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) && n > 0 ? n : null;
    }
}