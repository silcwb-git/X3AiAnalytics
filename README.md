# 🤖 X3AiAnalytics

Camada de inteligência conversacional sobre dados de ERP — **sem alucinação**.  
Projeto desenvolvido por **Silvana Siqueira** para a **KETHER — Soluções em Inteligência Artificial**.

---

### 🎯 O problema que este projeto resolve

A inteligência artificial generativa é excelente em conversar, mas extremamente não confiável quando o assunto envolve números de negócio estruturados. 

Se você perguntar a um chatbot genérico *“quanto o cliente X me deve?”*, ele pode inventar um valor plausível, misturar meses, confundir receitas com despesas ou responder com confiança algo que sequer existe. 

Em um ambiente de ERP, isso é inaceitável:
* Um número errado em uma conversa com o cliente gera um **problema comercial**.
* Um número errado em um relatório fiscal gera um **problema jurídico**.

💡 **A tese deste projeto:** Um LLM pode conversar com o dado, mas **nunca deve calcular o dado**. Ele escolhe qual consulta rodar (o código executável), o banco de dados devolve o número exato e só então o LLM redige a resposta — usando exclusivamente o que veio do banco.

Pergunta ➔ Classificação de Escopo ➔ Planejamento da Consulta ➔ Execução Determinística no Banco ➔ Composição da Resposta via JSON

markdown
# 🤖 X3AiAnalytics

Camada de inteligência conversacional sobre dados de ERP — **sem alucinação**.  
Projeto desenvolvido por **Silvana Siqueira** para a **KETHER — Soluções em Inteligência Artificial**.

---

### 🎯 O problema que este projeto resolve

A inteligência artificial generativa é excelente em conversar, mas extremamente não confiável quando o assunto envolve números de negócio estruturados. 

Se você perguntar a um chatbot genérico *“quanto o cliente X me deve?”*, ele pode inventar um valor plausível, misturar meses, confundir receitas com despesas ou responder com confiança algo que sequer existe. 

Em um ambiente de ERP, isso é inaceitável:
* Um número errado em uma conversa com o cliente gera um **problema comercial**.
* Um número errado em um relatório fiscal gera um **problema jurídico**.

💡 **A tese deste projeto:** Um LLM pode conversar com o dado, mas **nunca deve calcular o dado**. Ele escolhe qual consulta rodar (o código executável), o banco de dados devolve o número exato e só então o LLM redige a resposta — usando exclusivamente o que veio do banco.

Use o código com cuidado.
Pergunta ➔ Classificação de Escopo ➔ Planejamento da Consulta ➔ Execução Determinística no Banco ➔ Composição da Resposta via JSON

---

### ✨ O que está implementado

| Camada | O que faz |
| :--- | :--- |
| **🧱 Domain** | Entidades do ERP: clientes, títulos a receber, contas a pagar, ordens de produção. |
| **📋 Query Catalog** | 8 consultas de negócio declaradas com nome, domínio, descrição e parâmetros tipados. |
| **🧠 AI Layer** | `IAiProvider` com dois provedores: `OpenAiProvider` (com chave) e `DeterministicAiProvider` (fallback local). |
| **⚙️ Orchestrator** | Orquestra o fluxo dinâmico: classificação → planejar → executar → compor. |
| **🌐 API** | Endpoints REST: catálogo, execução direta e pergunta em linguagem natural. |
| **✅ Testes** | 42 testes automatizados cobrindo roteamento, limites, escopo e garantia anti-alucinação. |

---

### 🏗️ Arquitetura

O projeto é dividido em quatro camadas principais, seguindo os princípios de baixo acoplamento onde as dependências apontam **sempre para dentro**:

* **`X3AiAnalytics.Domain`** — Entidades e enumerações. Possui zero dependência externa.
* **`X3AiAnalytics.Application`** — Contratos de IA, orquestrador e compositor de respostas.
* **`X3AiAnalytics.Infraestrutura`** — EF Core, SQLite, catálogo de consultas e implementação dos provedores de IA.
* **`X3AiAnalytics.Api`** — Terminais (endpoints) REST que realizam a orquestração de entrada.
* **`X3AiAnalytics.Tests`** — Suíte xUnit cobrindo testes unitários, catálogo, orquestrador e chamadas HTTP.

> 🛠️ **Princípio de Design:** O Domínio não conhece o EF Core, a Aplicação não conhece o SQLite e a API apenas consome os contratos. Trocar o SQLite por SQL Server ou PostgreSQL exige a mudança de apenas **uma linha** no `Program.cs`.

---

### 📚 Catálogo de consultas

Cada consulta de negócio é declarada uma única vez, contendo os metadados necessários para o LLM decidir quando e como chamá-la.

| Consulta | Domínio | Uso / Casos de Negócio |
| :--- | :--- | :--- |
| `faturamento_por_cliente_periodo` | 💰 Vendas | Quanto cada cliente faturou, rankings e identificação do maior cliente. |
| `ticket_medio_por_regiao` | 💰 Vendas | Ticket médio, valor médio por venda e comparações regionais. |
| `titulos_vencidos_por_cliente` | 📥 Receitas | Análise de inadimplência, clientes em atraso e contas a receber. |
| `evolucao_receita_mensal` | 📥 Receitas | Tendência de faturamento e comparativos mês a mês. |
| `concentracao_receita_por_cliente` | 📥 Receitas | Risco de dependência comercial e participação percentual de clientes. |
| `contas_pagar_vencidas_por_centro_custo` | 📤 Despesas | Despesas em atraso e detalhamento de gastos por área/setor. |
| `evolucao_contas_pagar` | 📤 Despesas | Tendência de despesas estruturadas e comparativos mensais. |
| `ordens_producao_atrasadas` | 🏭 Produção | Gargalos em linhas de produção e valor previsto em risco de entrega. |

---

### 🔒 Uma garantia de não-alucinação

Este mecanismo representa o coração do projeto e funciona sob três pilares independentes:

1. **Recusa explícita fora do domínio:** Quando a pergunta do usuário não pertence ao universo do ERP, nenhum LLM de processamento é acionado. A recusa é determinística: a API retorna um payload contendo `outOfScope: true`, com `data: null` e `queryUsed: null`.
2. **Execução isolada da composição:** O modelo de IA responsável por redigir o texto amigável nunca tem acesso direto ao banco de dados. Ele recebe exclusivamente o JSON retornado pela consulta estruturada. Não há caminho técnico viável para a IA inventar um número, pois ela não possui outra fonte de informação.
3. **Testes automatizados de consistência:** O teste analítico `Ask_StructuredData_ShouldMatch_DirectQueryExecution` compara a resposta textual/estruturada gerada pelo orquestrador contra a execução crua da consulta no banco. Se um único decimal for modificado, omitido ou inventado, a suíte de testes quebra imediatamente no CI.

---

### 🚀 Como rodar

* **Pré-requisitos:** .NET 10 SDK instalado. O banco de dados roda em um arquivo SQLite local, dispensando configurações extras.

```bash
# Clonar o repositório
git clone https://github.com
cd X3AiAnalytics

# Compilar o projeto
dotnet build

# Executar a API
dotnet run --project src/X3AiAnalytics.Api
```

* A API ficará disponível em: `http://localhost:5212`
* O Swagger para testes de interface pode ser acessado em: `http://localhost:5212/swagger`

#### Pontos finais (Endpoints)

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| **GET** | `/api/v1/health` | Verifica o status de saúde da aplicação. |
| **GET** | `/api/v1/queries` | Retorna o catálogo completo de consultas e seus metadados. |
| **POST** | `/api/v1/queries/{name}/run` | Executa uma consulta direta de forma determinística (sem IA). |
| **POST** | `/api/v1/ask` | Processa perguntas do usuário em linguagem natural. |

#### Exemplos de chamadas via cURL

* **Listar catálogo:**
```bash
curl http://localhost:5212/api/v1/queries
```

* **Executar consulta analítica direta:**
```bash
curl -X POST http://localhost:5212/api/v1/queries/titulos_vencidos_por_cliente/run \
     -H "Content-Type: application/json" \
     -d "{\"diasMinimosAtraso\":\"30\"}"
```

* **Perguntar em linguagem natural:**
```bash
curl -X POST http://localhost:5212/api/v1/ask \
     -H "Content-Type: application/json" \
     -d "{\"question\":\"Quais clientes têm títulos vencidos há mais de 60 dias?\"}"
```
*A resposta deste endpoint retornará unificada contendo: texto legível (`resposta`), registros estruturados (`dados`), rastreamento de telemetria (`queryUsed`, `parâmetros`, `tempo de execução`, `provedor`) e a flag `outOfScope`.*

---

### 🧪 Testes

```bash
dotnet test
```

| Suíte de Testes | O que prova |
| :--- | :--- |
| **`DeterministicProviderTests`** | Garante que as 8 consultas mapeadas roteiam corretamente e congela regressões de classificação. |
| **`QueryCatalogTests`** | Valida a aplicação correta de filtros, ordenações, cálculos percentuais e o teto limite de 200 linhas. |
| **`OrchestratorTests`** | Prova que entradas fora de escopo não tocam os dados e valida que as saídas batem 100% com o banco. |
| **`ApiTests`** | Assegura os retornos HTTP corretos (200, 400, 404) e a integridade dos endpoints. |

* **Resultado atual da suíte:** 42 aprovados, 0 reprovados.

---

### 🧭 Decisões técnicas

* **O LLM não gera nem executa SQL nativo:** O provedor de inteligência apenas mapeia a intenção da pergunta para uma rota de consulta existente em um catálogo estrito. Não há riscos de SQL Injection ou buscas fora do espaço de ações auditável.
* **Dois provedores sob o mesmo contrato:** O sistema utiliza `OpenAiProvider` para ambientes produtivos e o `DeterministicAiProvider` como mecanismo de fallback offline. Este fallback usa pontuação ponderada por palavras-chave e um compositor local, permitindo testes completos e demonstrações sem custos de infraestrutura de nuvem.
* **Bug real mitigado (Empate de classificação):** Durante o desenvolvimento, perguntas como *“Quais clientes têm títulos vencidos há mais de 60 dias?”* geravam empate de pesos entre as chaves "cliente" e "vencido", ativando erroneamente o faturamento. O algoritmo foi corrigido para adotar pesos ponderados (termos de negócio específicos valem 3, termos genéricos valem 1). Um teste de regressão foi criado para impedir que o erro retorne ao código.
* **Limite estrito de 200 linhas por consulta:** Evita estouro de contexto nos modelos de linguagem, economiza tokens, reduz a latência HTTP e impede respostas ilegíveis ao usuário final.
* **SQLite para ambiente local:** Configuração zero para rodar o ecossistema. O catálogo utiliza expressões LINQ totalmente traduzíveis, facilitando a migração futura para outros dialetos SQL.

#### ⚠️ Limites conhecidos e melhorias técnicas
* **Estado compartilhado no compositor:** O provedor determinístico guarda em memória o estado da última consulta processada para fins de formatação. Em ambientes de produção concorrentes, esse nome de consulta deve ser explicitamente propagado via parâmetros para garantir a thread-safety.
* **Cálculo de dias de atraso pós-banco:** A instrução de agregação `.Days` baseada na diferença de datas (`hoje - dataVencimento`) gerou falhas de tradução em tempo de execução no SQLite. Para contornar a limitação, a API executa a agregação trazendo a data mínima do banco e calcula o intervalo de dias em memória utilizando `.ToListAsync()`.

---

### 🗺️ Roteiro de evolução (Roadmap)

* [ ] Implementação de autenticação, controle de acessos (RBAC) e arquitetura multiusuário.
* [ ] Substituição do comando `EnsureCreated` por Migrations formais para ambientes de produção.
* [ ] Desenvolvimento de um painel visual em Angular para interagir com a API de IA.
* [ ] Acoplamento de buscas híbridas em documentos não estruturados (RAG) para complementar o catálogo do ERP.
* [ ] Criação de conectores de integração nativos para os barramentos REST e SOAP do Sage X3.
* [ ] Implementação de ferramentas de observabilidade (rastreamento de latência, volumetria de requisições e taxas de recusa por escopo).

---

### 👩‍💻 Autora

**Silvana Siqueira** — KETHER — Soluções em Inteligência Artificial  
*Mais de 20 anos de experiência em Tecnologia da Informação.*
