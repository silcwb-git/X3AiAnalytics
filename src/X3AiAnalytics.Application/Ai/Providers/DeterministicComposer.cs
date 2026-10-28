using System.Text.Json;

namespace X3AiAnalytics.Application.Ai.Providers;

/// <summary>
/// Compositor determinístico: transforma o JSON de resultado em texto legível,
/// sem chamar LLM. Usado quando não há OPENAI_API_KEY configurada.
/// </summary>
public static class DeterministicComposer
{
    public static string Compose(string queryName, string jsonData)
    {
        if (string.IsNullOrWhiteSpace(jsonData) || jsonData.Trim() is "[]" or "{}")
        {
            return "Não encontrei dados para responder a essa pergunta com os filtros informados.";
        }

        using var doc = JsonDocument.Parse(jsonData);
        var rows = doc.RootElement;

        if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() == 0)
        {
            return "Não encontrei dados para responder a essa pergunta com os filtros informados.";
        }

        var total = rows.GetArrayLength();

        return queryName switch
        {
            "titulos_vencidos_por_cliente" => Titulos(rows, total),
            "faturamento_por_cliente_periodo" => Faturamento(rows, total),
            "ordens_producao_atrasadas" => Ordens(rows, total),
            "contas_pagar_vencidas_por_centro_custo" => ContasPagar(rows, total),
            "concentracao_receita_por_cliente" => Concentracao(rows, total),
            "ticket_medio_por_regiao" => TicketMedio(rows, total),
            "evolucao_receita_mensal" => Evolucao(rows, total, "receita"),
            "evolucao_contas_pagar" => Evolucao(rows, total, "totalDespesas"),
            _ => $"Encontrei {total} registro(s) para essa consulta."
        };
    }

    private static string Titulos(JsonElement rows, int total)
    {
        var primeiro = rows[0];
        var cliente = primeiro.GetProperty("cliente").GetString();
        var valor = Money(primeiro.GetProperty("totalVencido"));
        var dias = primeiro.GetProperty("diasAtrasoMaximo").GetInt32();

        var soma = rows.EnumerateArray()
            .Sum(r => r.GetProperty("totalVencido").GetDecimal());

        return $"{total} cliente(s) com títulos vencidos, somando {Money(soma)} em atraso. " +
               $"O maior é {cliente}, com {valor} e atraso máximo de {dias} dias.";
    }

    private static string Faturamento(JsonElement rows, int total)
    {
        var primeiro = rows[0];
        var cliente = primeiro.GetProperty("cliente").GetString();
        var valor = Money(primeiro.GetProperty("totalFaturado"));

        return $"{total} cliente(s) faturaram no período consultado. " +
               $"O maior é {cliente}, com {valor}.";
    }

    private static string Ordens(JsonElement rows, int total)
    {
        var primeiro = rows[0];
        var quantidade = primeiro.GetProperty("diasAtraso").GetInt32();

        var somaRisco = rows.EnumerateArray()
            .Sum(r => r.GetProperty("valorPrevisto").GetDecimal());

        return $"{total} ordem(ns) de produção atrasada(s), com {Money(somaRisco)} em valor previsto em risco. " +
               $"A mais antiga tem {quantidade} dias de atraso.";
    }

    private static string ContasPagar(JsonElement rows, int total)
    {
        var soma = rows.EnumerateArray()
            .Sum(r => r.GetProperty("totalVencido").GetDecimal());

        var primeiro = rows[0];
        var centro = primeiro.GetProperty("centroCusto").GetString();

        return $"{total} centro(s) de custo com contas a pagar vencidas, totalizando {Money(soma)}. " +
               $"O maior é {centro}.";
    }

    private static string Concentracao(JsonElement rows, int total)
    {
        var primeiro = rows[0];
        var cliente = primeiro.GetProperty("cliente").GetString();
        var percentual = primeiro.GetProperty("percentualParticipacao").GetDecimal();

        return $"Os {total} maiores cliente(s) em receita. " +
               $"O primeiro é {cliente}, com {percentual:F2}% de participação.";
    }

    private static string TicketMedio(JsonElement rows, int total)
    {
        var primeiro = rows[0];
        var regiao = primeiro.GetProperty("regiao").GetString();
        var ticket = Money(primeiro.GetProperty("ticketMedio"));

        return $"Ticket médio calculado em {total} região(ões). " +
               $"A maior é {regiao}, com ticket médio de {ticket}.";
    }

    private static string Evolucao(JsonElement rows, int total, string campo)
    {
        var soma = rows.EnumerateArray().Sum(r => r.GetProperty(campo).GetDecimal());
        var primeiro = rows[0];
        var ultimo = rows[total - 1];

        var mesInicio = $"{primeiro.GetProperty("mes").GetInt32():00}/{primeiro.GetProperty("ano").GetInt32()}";
        var mesFim = $"{ultimo.GetProperty("mes").GetInt32():00}/{ultimo.GetProperty("ano").GetInt32()}";

        return $"Evolução em {total} mês(es), de {mesInicio} a {mesFim}, " +
               $"somando {Money(soma)} no total.";
    }

    private static string Money(JsonElement el) => Money(el.GetDecimal());

    private static string Money(decimal valor) => $"R$ {valor:N2}";
}