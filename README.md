🤖 X3AiAnalytics

Camada de inteligência conversacional sobre dados de ERP — sem alucinação.
Projeto de desenvolvimento desenvolvido por Silvana Siqueira para a KETHER — Soluções em Inteligência Artificial.

🎯 O problema que este projeto resolve

IA generativa é excelente em conversar e extremamente confiável quando o assunto é número de negócio.

Pergunte a um chatbot genérico “quanto o cliente X me deve?” e ele pode inventar um valor plausível, misturado meses, confundir receita com receitas ou responder com confiança algo que não existe.

Em um ERP, isso é inaceitável. Um número errado em uma conversa com o cliente é um problema comercial. Um número errado em um relatório fiscal é um problema jurídico.

💡 A tese deste projeto: um LLM pode conversar com o dado, mas nunca deve calcular o dado. Ele escolhe qual consultar rodar. O código executável. O banco devolve o número. Só então o LLM redija a resposta — usando exclusivamente o que veio do banco.

O fluxo é sempre: pergunta → classificação de escopo → planejamento da consulta → execução determinística no banco → composição da resposta a partir do JSON retornado.

✨ O que está implementado

Camada O que faz
🧱 Domain Entidades do ERP: clientes, títulos a receber, contas a pagar, ordens de produção
📋 Query Catalog 8 consultas de negócio declaradas com nome, domínio, descrição e parâmetros tipados
🧠 AI Layer IAiProvider com dois provedores: OpenAiProvider (com chave) e DeterministicAiProvider (fallback local)
⚙️ Orchestrator Orquestra o fluxo classificação → e → executar → compor
🌐 API Endpoints REST: catálogo, execução direta e pergunta em linguagem natural
✅ Testes 42 testes cobrindo roteamento, limites, escopo e garantia anti-alucinação

🏗️ Arquitetura

O projeto é dividido em quatro camadas, com dependências apontando sempre para dentro:

X3AiAnalytics.Domain — entidades e enumerações. Zero dependência externa.
X3AiAnalytics.Application — contratos de IA, orquestrador e compositor.
X3AiAnalytics.Infraestrutura — EF Core, SQLite, catálogo de consultas e implementação de provedores.
X3AiAnalytics.Api — terminais REST.
X3AiAnalytics.Tests — xUnit cobrindo unitários, catálogo, orquestrador e HTTP.

Princípio: o Domínio não conhece o EF Core, a Aplicação não conhece SQLite, a Infraestrutura implementada ou que a Aplicação declara e a Api só orquestra. Trocar SQLite por SQL Server ou PostgreSQL é uma linha no Program.cs.

📚 Catálogo de consultas

Cada consulta é declarada uma vez, com metadados que o LLM usa para decidir quando chamá-la.

Consulta Domínio Uso
faturamento_por_cliente_periodo 💰 Vendas Quanto cada cliente faturou, ranking, maior cliente
ticket_medio_por_regiao 💰 Vendas Ticket médio, valor médio por venda, comparação regional
titulos_vencidos_por_cliente 📥 Receitas Inadimplência, clientes em atraso, contas a receber
evolucao_receita_mensal 📥 Receitas Tendência de receita, comparativo mês a mês
concentracao_receita_por_cliente 📥 Receitas Risco de dependência, participação percentual
contas_pagar_vencidas_por_centro_custo 📤 Despesas Despesas em atraso, gastos por área
evolucao_contas_pagar 📤 Despesas Tendência de despesas, comparativo mensal
ordens_producao_atrasadas 🏭 Produção Produção atrasada, valor previsto em risco

Os quatro domínios cobrem as demandas mais citadas por clientes de ERP: vendas, receitas, despesas e produção.

🔒 Uma garantia de não-alucinação

Este é o coração do projeto. Três mecanismos independentes.

Recusa explícita fora do domínio. Quando a pergunta não pertence ao domínio de dados do ERP, nenhum LLM é chamado, nenhum dado é tocado e a recusa é determinística. A resposta retorna outOfScope: true com data: null e queryUsed: null na trilha.

Execução isolada da composição. O LLM que redige a resposta nunca tem acesso ao banco. Ele recebe apenas o JSON já retornado pela consulta. Não existe caminho técnico para ele inventar um número — ele não tem de onde tirar outro.

Testes que provam a garantia. O teste Ask_StructuredData_ShouldMatch_DirectQueryExecution compara o resultado do orquestrador contra a execução direta da mesma consulta, valor por valor, ordenado. Se um único decimal for adicionado, omitido ou alterado, o teste quebra.

🚀 Como rodar

Pré-requisitos: .NET 10 SDK. Nada mais — o banco é SQLite em arquivo local.

git clone https://github.com/silcwb/X3AiAnalytics.git
cd X3AiAnalytics
dotnet build
dotnet run --project src/X3AiAnalytics.Api

A API sobe em http://localhost:5212 e o Swagger em http://localhost:5212/swagger .

Pontos finais

Método Rota Descrição
GET /api/v1/health Status da aplicação
GET /api/v1/queries Catálogo completo de consultas
POST /api/v1/queries/{name}/run Executa uma consulta direta, sem IA
POST /api/v1/ask Pergunta em linguagem natural

exemplos

Listar o amigo:

curl http://localhost:5212/api/v1/queries

Executar consulta can:

curl -X POST http://localhost:5212/api/v1/queries/titulos_vencidos_por_cliente/run -H “Content-Type: application/json” -d “{"diasMinimosAtraso":"30"}”

Pergunte em linguagem natural:

curl -X POST http://localhost:5212/api/v1/ask -H “Content-Type: application/json” -d “{"question":"Quais clientes têm títulos vencidos há mais de 60 dias?"}”

A resposta traz resposta (texto legível), dados (registros estruturados), rastreamento (consulta usada, parâmetros, contagem de linhas, tempo de execução e provedor) e outOfScope.

🧪 Testes

teste dotnet

Suíte O que prova
DeterministicProviderTests Roteia as 8 consultas corretamente + congelamento regressões de roteamento
QueryCatalogTests Filtros, ordenação, limite de 200 linhas, percentuais
OrchestratorTests Fora de escopo nunca devolve dado; dados batem com execução direta
ApiTests 200/400/404 nos endpoints, catálogo com 8 itens

Resultado atual: 42 aprovados, 0 reprovados.

🧭 Decisões técnicas

O LLM não executa SQL. O provedor escolhe qual consultar rodar a partir de um catálogo fechado. Não gera SQL, não inventa tabela, não tem acesso ao DbContext. O espaço de ações é finito e auditável.

Dois provedores, um contrato. OpenAiProvider para produção, DeterministicAiProvider como fallback sem chave de API. O fallback não é um brinquedo: usa pontuação ponderada de palavras-chave e compositor próprio, e funciona 100% offline — o que permite testar e demonstrar sem custo.

🐛 Bug real encontrado em desenvolvimento: empate de classificação. A pergunta “Quais clientes têm títulos vencidos há mais de 60 dias?” rotei para faturamento_por_cliente_periodo. Causa: “cliente” e “vencido” valiam 1 ponto cada, dava empate, e o dicionário devolvia a primeira chave inserida. Correção: pontuação ponderada (termos específicos valem 3, genéricos valem 1) e desempate explícito por ordem de especificidade. O bug está congelado em teste de regressão — se alguém reverter, o CI quebra.

Limite de 200 linhas por consulta. Toda consulta com respeito MaxRows. Protege o contexto do LLM, a latência e evita resposta ilegível. Testado automaticamente.

SQLite em desenvolvimento. Zero configuração para rodar. O catálogo usa LINQ traduzível e nenhuma consulta depende do dialeto específico.

⚠️ Limite conhecido: estado compartilhado no compositor. O DeterministicAiProvider guarda a última consulta programada na memória para escolher o modelo de formatação. Funciona no contexto monousuário de demonstração; em produção, o nome da consulta seria propagado como parâmetro, eliminando o estado compartilhado.

⚠️ Cálculo de dias de atraso em memória. (hoje - dataVencimento).Days dentro de agregação não é traduzido pelo EF Core com SQLite. Diagnóstico: erro de runtime não pôde ser traduzido. Solução: agregar com Min(dataVencimento) no banco e calcular os dias em memória após o ToListAsync.

🗺️ Roteiro

Autenticação e multiusuário
Migrações em vez de GaranteCreated para produção
Painel Angular consumindo a API conversacional
Consultas encontradas sobre documentos (RAG) complementando as consultas estruturadas
Conectores nativos para REST e SOAP do Sage X3
Observabilidade: conclusões por consulta, latência e taxas de recusa

👩‍💻 Autora

Silvana Siqueira — KETHER — Soluções em Inteligência Artificial

Mais de 20 anos em TI e desenvolvimento. Atuação na implantação do Sage X3 no Brasil (2016), com Adonix/Eclipse, telas, relatórios fiscais, fiscais e financeiros, e integrações. Posteriormente, tecnologias Microsoft, Node.js e microsserviços Java/Spring Boot.

📄 Licença

MIT.