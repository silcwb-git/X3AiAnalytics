using Microsoft.EntityFrameworkCore;
using X3AiAnalytics.Domain.Entities;

namespace X3AiAnalytics.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Fatura> Faturas => Set<Fatura>();
    public DbSet<TituloReceber> TitulosReceber => Set<TituloReceber>();
    public DbSet<CentroCusto> CentrosCusto => Set<CentroCusto>();
    public DbSet<ContaPagar> ContasPagar => Set<ContaPagar>();
    public DbSet<OrdemProducao> OrdensProducao => Set<OrdemProducao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>(e =>
        {
            e.HasIndex(c => c.Codigo).IsUnique();
            e.Property(c => c.Nome).HasMaxLength(200).IsRequired();
            e.Property(c => c.Regiao).HasMaxLength(60);
            e.Property(c => c.Segmento).HasMaxLength(60);
        });

        modelBuilder.Entity<Fatura>(e =>
        {
            e.Property(f => f.Valor).HasPrecision(18, 2);
            e.HasOne(f => f.Cliente)
                .WithMany()
                .HasForeignKey(f => f.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TituloReceber>(e =>
        {
            e.Property(t => t.Valor).HasPrecision(18, 2);
            e.HasOne(t => t.Cliente)
                .WithMany()
                .HasForeignKey(t => t.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContaPagar>(e =>
        {
            e.Property(c => c.Valor).HasPrecision(18, 2);
            e.HasOne(c => c.CentroCusto)
                .WithMany()
                .HasForeignKey(c => c.CentroCustoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrdemProducao>(e =>
        {
            e.Property(o => o.Quantidade).HasPrecision(18, 2);
            e.Property(o => o.ValorPrevisto).HasPrecision(18, 2);
        });
    }
}