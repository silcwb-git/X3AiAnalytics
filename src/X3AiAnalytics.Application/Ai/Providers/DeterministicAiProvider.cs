using X3AiAnalytics.Application.Ai.Contracts;

namespace X3AiAnalytics.Application.Ai.Providers;

public class DeterministicAiProvider : IAiProvider
{
    public string Name => "deterministic-fallback";

    // Palavras de peso 3 = altamente específicas do domínio da consulta.
    // Pesos maiores vencem empates: "vencido" aponta para títulos, "cliente" não basta sozinha.
    private static readonly (string Query, string[] Strong, string[] Weak)[] Rules =
    {
        ("titulos_vencidos_por_cliente",
            new[] { "vencido", "vencidos", "inadimpl", "inadimplência", "inadimplencia", "título vencido", "titulo vencido" },
            new[] { "atraso", "atras", "receber", "cliente" }),

        ("contas_pagar_vencidas_por_centro_custo",
            new[] { "contas a pagar", "conta a pagar", "centro de custo", "fornecedor" },
            new[] { "pagar", "despesa", "gasto" }),

        ("ordens_producao_atrasadas",
            new[] { "ordem de produção", "ordem de producao", "ordens de produção", "ordens de producao", "ordem atrasada", "linha de produção", "linha de producao" },
            new[] { "produ", "ordem", "ordens", "atrasada", "atrasadas" }),

        ("ticket_medio_por_regiao",
            new[] { "ticket médio", "ticket medio", "valor médio", "valor medio" },
            new[] { "ticket", "médio", "medio", "região", "regiao" }),

        ("concentracao_receita_por_cliente",
            new[] { "concentração", "concentracao", "participação", "participacao", "dependência de cliente", "dependencia de cliente" },
            new[] { "percentual", "dependência", "dependencia" }),

        ("evolucao_receita_mensal",
            new[] { "evolução da receita", "evolucao da receita", "receita mês a mês", "receita mes a mes", "tendência de receita", "tendencia de receita" },
            new[] { "evolu", "receita", "mensal", "tendência", "tendencia" }),

        ("evolucao_contas_pagar",
            new[] { "evolução de despesa", "evolucao de despesa", "evolução de gasto", "evolucao de gasto", "tendência de despesa", "tendencia de despesa" },
            new[] { "evolução", "evolucao", "gastos", "custos" }),

        ("faturamento_por_cliente_periodo",
            new[] { "faturamento por cliente", "ranking de cliente", "maior cliente", "quanto cada cliente", "faturou" },
            new[] { "faturamento", "fatura", "vendas", "vendeu", "cliente" })
    };

    private static readonly string[] OutOfScopeKeywords =
    {
        "previsão do tempo", "previsao do tempo", "clima", "cotação", "cotacao",
        "dólar", "dolar", "eleição", "eleicao", "notícia", "noticia", "piada",
        "receita de bolo", "futebol", "quem foi", "quem é o presidente", "quem e o presidente"
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

        var best = Resolve(lower);
        return Task.FromResult(new IntentResult
        {
            IsInScope = best != null,
            Domain = best != null ? "Erp" : "Unknown",
            Reason = best != null ? $"Consulta identificada: {best}." : "Nenhuma consulta do catálogo corresponde."
        });
    }

    public Task<PlanResult> PlanAsync(string question, string catalogDescription, CancellationToken ct = default)
    {
        var lower = question.ToLowerInvariant();
        var query = Resolve(lower);

        if (query == null)
        {
            return Task.FromResult(new PlanResult
            {
                IsInScope = false,
                Reason = "Nenhuma consulta do catálogo corresponde à pergunta."
            });
        }

        var parameters = new Dictionary<string, string?>();
        var numero = ExtractNumber(lower);

        if (query is "titulos_vencidos_por_cliente" or "contas_pagar_vencidas_por_centro_custo")
        {
            if (numero.HasValue) parameters["diasMinimosAtraso"] = numero.Value.ToString();
        }
        else if (query == "concentracao_receita_por_cliente")
        {
            if (numero.HasValue) parameters["topN"] = numero.Value.ToString();
        }

        return Task.FromResult(new PlanResult
        {
            IsInScope = true,
            QueryName = query,
            Parameters = parameters,
            Reason = "Consulta selecionada por regra determinística com pontuação ponderada."
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

    /// <summary>
    /// Pontuação ponderada: palavra forte vale 3, fraca vale 1.
    /// Desempate explícito pela ordem da lista (mais específica primeiro).
    /// </summary>
    private static string? Resolve(string lower)
    {
        string? best = null;
        var bestScore = 0;

        foreach (var (query, strong, weak) in Rules)
        {
            var score = strong.Count(k => lower.Contains(k)) * 3
                      + weak.Count(k => lower.Contains(k));

            if (score > bestScore)
            {
                bestScore = score;
                best = query;
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