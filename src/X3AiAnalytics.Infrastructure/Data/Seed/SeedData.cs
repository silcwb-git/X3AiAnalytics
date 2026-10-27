using X3AiAnalytics.Domain.Entities;
using X3AiAnalytics.Domain.Enums;

namespace X3AiAnalytics.Infrastructure.Data.Seed;

public static class SeedData
{
    // Seed fixa: o mesmo dataset em toda execução, em qualquer máquina.
    private const int Seed = 42;

    public static void Populate(AppDbContext db)
    {
        if (db.Clientes.Any())
        {
            return;
        }

        var rng = new Random(Seed);
        var hoje = new DateTime(2026, 10, 27);

        var clientes = GerarClientes(rng);
        db.Clientes.AddRange(clientes);

        var centrosCusto = GerarCentrosCusto();
        db.CentrosCusto.AddRange(centrosCusto);

        db.Faturas.AddRange(GerarFaturas(rng, clientes, hoje));
        db.TitulosReceber.AddRange(GerarTitulos(rng, clientes, hoje));
        db.ContasPagar.AddRange(GerarContasPagar(rng, centrosCusto, hoje));
        db.OrdensProducao.AddRange(GerarOrdensProducao(rng, hoje));

        db.SaveChanges();
    }

    private static List<Cliente> GerarClientes(Random rng)
    {
        string[] prefixos =
        {
            "Alfa", "Nova", "Grupo", "Metal", "Agro", "Comercial", "Indústria",
            "Distribuidora", "Transportes", "Tecno", "Log", "Brasil"
        };

        string[] sufixos =
        {
            "Vale", "Serra", "Norte", "Sul", "Aliança", "Progresso", "União",
            "Horizonte", "Central", "Primavera", "Atlântica", "Real"
        };

        string[] regioes = { "Sudeste", "Sul", "Nordeste", "Centro-Oeste", "Norte" };
        string[] segmentos = { "Indústria", "Varejo", "Atacado", "Serviços", "Agronegócio" };

        var lista = new List<Cliente>();
        for (var i = 1; i <= 80; i++)
        {
            lista.Add(new Cliente
            {
                Id = i,
                Codigo = $"CLI{i:D4}",
                Nome = $"{prefixos[rng.Next(prefixos.Length)]} {sufixos[rng.Next(sufixos.Length)]} {i:D2} Ltda",
                Regiao = regioes[rng.Next(regioes.Length)],
                Segmento = segmentos[rng.Next(segmentos.Length)],
                Ativo = rng.Next(100) < 92
            });
        }

        return lista;
    }

    private static List<CentroCusto> GerarCentrosCusto()
    {
        var nomes = new[]
        {
            ("CC01", "Administrativo"),
            ("CC02", "Comercial"),
            ("CC03", "Produção"),
            ("CC04", "Logística"),
            ("CC05", "Tecnologia da Informação"),
            ("CC06", "Recursos Humanos"),
            ("CC07", "Financeiro"),
            ("CC08", "Manutenção")
        };

        return nomes.Select((n, i) => new CentroCusto
        {
            Id = i + 1,
            Codigo = n.Item1,
            Nome = n.Item2
        }).ToList();
    }

    private static List<Fatura> GerarFaturas(Random rng, List<Cliente> clientes, DateTime hoje)
    {
        string[] produtos =
        {
            "Licença ERP", "Consultoria de Implantação", "Suporte Mensal",
            "Desenvolvimento Customizado", "Treinamento", "Integração de Sistemas",
            "Manutenção Evolutiva", "Migração de Dados"
        };

        var lista = new List<Fatura>();
        for (var i = 1; i <= 600; i++)
        {
            var cliente = clientes[rng.Next(clientes.Count)];
            var diasAtras = rng.Next(0, 365);

            lista.Add(new Fatura
            {
                Id = i,
                Numero = $"FT{i:D6}",
                ClienteId = cliente.Id,
                DataEmissao = hoje.AddDays(-diasAtras),
                Produto = produtos[rng.Next(produtos.Length)],
                Valor = Math.Round((decimal)(rng.NextDouble() * 48000 + 2000), 2)
            });
        }

        return lista;
    }

    private static List<TituloReceber> GerarTitulos(Random rng, List<Cliente> clientes, DateTime hoje)
    {
        var lista = new List<TituloReceber>();
        for (var i = 1; i <= 300; i++)
        {
            var cliente = clientes[rng.Next(clientes.Count)];
            var vencimento = hoje.AddDays(rng.Next(-180, 90));

            StatusTitulo status;
            if (vencimento < hoje)
            {
                status = rng.Next(100) < 55 ? StatusTitulo.Vencido : StatusTitulo.Pago;
            }
            else
            {
                status = StatusTitulo.Aberto;
            }

            if (rng.Next(100) < 3)
            {
                status = StatusTitulo.Cancelado;
            }

            lista.Add(new TituloReceber
            {
                Id = i,
                Numero = $"TR{i:D6}",
                ClienteId = cliente.Id,
                DataVencimento = vencimento,
                Valor = Math.Round((decimal)(rng.NextDouble() * 32000 + 800), 2),
                Status = status
            });
        }

        return lista;
    }

    private static List<ContaPagar> GerarContasPagar(Random rng, List<CentroCusto> centros, DateTime hoje)
    {
        string[] descricoes =
        {
            "Fornecedor de Infraestrutura", "Licenças de Software", "Serviços de Cloud",
            "Consultoria Externa", "Material de Escritório", "Energia Elétrica",
            "Aluguel", "Telecomunicações", "Seguros", "Manutenção Predial",
            "Marketing Digital", "Serviços Contábeis"
        };

        var lista = new List<ContaPagar>();
        for (var i = 1; i <= 200; i++)
        {
            var centro = centros[rng.Next(centros.Count)];
            var vencimento = hoje.AddDays(rng.Next(-120, 60));

            StatusTitulo status;
            if (vencimento < hoje)
            {
                status = rng.Next(100) < 70 ? StatusTitulo.Vencido : StatusTitulo.Pago;
            }
            else
            {
                status = StatusTitulo.Aberto;
            }

            lista.Add(new ContaPagar
            {
                Id = i,
                Numero = $"CP{i:D6}",
                Descricao = descricoes[rng.Next(descricoes.Length)],
                CentroCustoId = centro.Id,
                DataVencimento = vencimento,
                Valor = Math.Round((decimal)(rng.NextDouble() * 18000 + 300), 2),
                Status = status
            });
        }

        return lista;
    }

    private static List<OrdemProducao> GerarOrdensProducao(Random rng, DateTime hoje)
    {
        string[] produtos =
        {
            "Conjunto Mecânico A1", "Módulo Eletrônico B2", "Estrutura Metálica C3",
            "Painel de Controle D4", "Kit Hidráulico E5", "Componente Polimérico F6",
            "Sistema de Vedação G7", "Módulo de Energia H8"
        };

        string[] linhas = { "Linha 01", "Linha 02", "Linha 03", "Linha 04" };

        var lista = new List<OrdemProducao>();
        for (var i = 1; i <= 120; i++)
        {
            var inicio = hoje.AddDays(rng.Next(-90, 30));
            var previsao = inicio.AddDays(rng.Next(5, 45));

            StatusProducao status;
            if (previsao < hoje)
            {
                status = rng.Next(100) < 35 ? StatusProducao.Concluida : StatusProducao.Atrasada;
            }
            else if (inicio <= hoje)
            {
                status = StatusProducao.EmAndamento;
            }
            else
            {
                status = StatusProducao.Planejada;
            }

            lista.Add(new OrdemProducao
            {
                Id = i,
                Numero = $"OP{i:D5}",
                Produto = produtos[rng.Next(produtos.Length)],
                Linha = linhas[rng.Next(linhas.Length)],
                DataInicio = inicio,
                DataPrevisao = previsao,
                Quantidade = rng.Next(10, 2000),
                ValorPrevisto = Math.Round((decimal)(rng.NextDouble() * 180000 + 5000), 2),
                Status = status
            });
        }

        return lista;
    }
}