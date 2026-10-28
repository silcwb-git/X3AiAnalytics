using Microsoft.EntityFrameworkCore;
using X3AiAnalytics.Application.Ai;
using X3AiAnalytics.Application.Ai.Providers;
using X3AiAnalytics.Infrastructure.Ai;
using X3AiAnalytics.Infrastructure.Data;
using X3AiAnalytics.Infrastructure.Data.Seed;
using X3AiAnalytics.Infrastructure.Queries;
using X3AiAnalytics.Infrastructure.Queries.Definitions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
                      ?? "Data Source=x3ai.db"));

// Catálogo de consultas
builder.Services.AddScoped<IQueryDefinition, FaturamentoPorClientePeriodoQuery>();
builder.Services.AddScoped<IQueryDefinition, TitulosVencidosPorClienteQuery>();
builder.Services.AddScoped<IQueryDefinition, EvolucaoReceitaMensalQuery>();
builder.Services.AddScoped<IQueryDefinition, ContasPagarVencidasPorCentroCustoQuery>();
builder.Services.AddScoped<IQueryDefinition, OrdensProducaoAtrasadasQuery>();
builder.Services.AddScoped<IQueryDefinition, TicketMedioPorRegiaoQuery>();
builder.Services.AddScoped<IQueryDefinition, ConcentracaoReceitaPorClienteQuery>();
builder.Services.AddScoped<IQueryDefinition, EvolucaoContasPagarQuery>();
builder.Services.AddScoped<QueryCatalog>();

// IA
builder.Services.AddSingleton<IAiProvider>(_ => AiProviderFactory.Create());
builder.Services.AddScoped<AiOrchestrator>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData.Populate(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/v1/health", () => Results.Ok(new
{
    status = "ok",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/api/v1/queries", (QueryCatalog catalog) =>
    Results.Ok(catalog.All.Select(d => new
    {
        name = d.Name,
        domain = d.Domain,
        description = d.Description,
        parameters = d.Parameters.Select(p => new { p.Name, p.Type, p.Description })
    })));

app.MapControllers();

app.Run();
public partial class Program { }