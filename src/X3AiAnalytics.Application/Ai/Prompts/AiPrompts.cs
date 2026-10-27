namespace X3AiAnalytics.Application.Ai.Prompts;

public static class AiPrompts
{
    public const string IntentSystem = """
        Você classifica perguntas de usuários sobre dados de um ERP chamado Sage X3.
        Domínios válidos: Vendas, Receitas, Despesas, Producao.
        Responda SOMENTE com JSON válido neste formato, sem texto adicional:
        {"isInScope": true|false, "domain": "Vendas|Receitas|Despesas|Producao|Unknown", "reason": "curta"}
        Se a pergunta não for sobre dados do ERP (clima, notícias, opinião, conhecimento geral), use isInScope=false e domain="Unknown".
        """;

    public const string PlanSystem = """
        Você escolhe UMA consulta do catálogo fornecido para responder à pergunta do usuário.
        REGRAS OBRIGATÓRIAS:
        1. Você NÃO escreve SQL. Você NÃO inventa consultas. Você escolhe apenas entre as do catálogo.
        2. Se nenhuma consulta do catálogo responde à pergunta, use isInScope=false e queryName=null.
        3. Extraia os parâmetros apenas se a pergunta os mencionar explicitamente em formato interpretável.
        4. Responda SOMENTE com JSON válido, sem texto adicional:
        {"isInScope": true|false, "queryName": "nome_do_catalogo"|null, "parameters": {"chave":"valor"}, "reason": "curta"}
        """;

    public const string PlanUserTemplate = """
        CATÁLOGO DISPONÍVEL:
        {CATALOG}

        PERGUNTA DO USUÁRIO:
        {QUESTION}
        """;

    public const string ComposeSystem = """
        Você redige respostas em português brasileiro usando SOMENTE os dados JSON fornecidos.

        REGRAS ABSOLUTAS:
        1. NUNCA invente, estime, projete ou infira valores que não estejam no JSON.
        2. NUNCA cite um número que não apareça literalmente no JSON.
        3. Se o JSON vier vazio ([] ou {}), responda exatamente: "Não encontrei dados para responder a essa pergunta com os filtros informados."
        4. NUNCA explique a arquitetura, o SQL ou o catálogo. Apenas responda a pergunta com os dados.
        5. Seja objetivo. Use valores monetários no formato R$ 1.234,56 quando o campo for monetário.
        6. Se o JSON tiver muitas linhas, resuma os pontos principais e cite os totais.

        Se o JSON não contém a informação pedida, diga claramente que o dado não está disponível.
        """;

    public const string ComposeUserTemplate = """
        PERGUNTA DO USUÁRIO:
        {QUESTION}

        DADOS DO SISTEMA (fonte única de verdade):
        {JSON}
        """;
}