using Microsoft.EntityFrameworkCore;

namespace Arauco.Otimizador.Data.MySql;

public class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbContext(DbContextOptions<DbContext> options) : base(options)
    {
    }

    // Cartao
    public DbSet<Entities.Cartao.Cartao> Cartao { get; set; }

    // Cenario
    public DbSet<Entities.Cenario.Cenario> Cenario { get; set; }
    public DbSet<Entities.Cenario.CenarioCriterio> CenarioCriterio { get; set; }
    public DbSet<Entities.Cenario.CenarioArquivo> CenarioArquivo { get; set; }

    // Demanda
    public DbSet<Entities.Demanda.Demanda> Demanda { get; set; }

    // Pedido
    public DbSet<Entities.Pedido.Pedido> Pedido { get; set; }

    // Setup
    public DbSet<Entities.Setup.Setup> Setup { get; set; }
    public DbSet<Entities.Setup.SetupOrdemImportancia> SetupOrdemImportancia { get; set; }

    // Otimizador
    public DbSet<Entities.Otimizador.CenarioOtimizacaoResultado> CenarioOtimizacaoResultado { get; set; }
    public DbSet<Entities.Otimizador.PedidoOtimizado> PedidoOtimizado { get; set; }
    public DbSet<Entities.Otimizador.PedidoOtimizadoNaoAlocado> PedidoOtimizadoNaoAlocado { get; set; }
    public DbSet<Entities.Otimizador.PedidoOtimizadoMotivo> PedidoOtimizadoMotivo { get; set; }

    // Dataset (master data consumida pelo motor de otimização)
    public DbSet<Entities.Dataset.Centro> Centro { get; set; }
    public DbSet<Entities.Dataset.Produto> Produto { get; set; }
    public DbSet<Entities.Dataset.Elegibilidade> Elegibilidade { get; set; }
    public DbSet<Entities.Dataset.Capacidade> Capacidade { get; set; }
    public DbSet<Entities.Dataset.Carteira> Carteira { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cartao
        modelBuilder.Entity<Entities.Cartao.Cartao>(u =>
        {
            u.HasKey(c => c.CartaoId);

            u.Property(p => p.TipoEnum).HasColumnName("TipoCartaoId");
        });

        // Cenario
        modelBuilder.Entity<Entities.Cenario.Cenario>(c =>
        {
            c.HasKey(x => x.CenarioId);

            c.Property(p => p.StatusEnum).HasColumnName("StatusCenarioId");
        });

        modelBuilder.Entity<Entities.Cenario.CenarioCriterio>(c =>
        {
            c.HasKey(x => x.Id);

            c.Property(p => p.Operador).HasColumnName("OperadorId");
        });

        modelBuilder.Entity<Entities.Cenario.CenarioArquivo>(a =>
        {
            a.HasKey(x => x.CenarioId);
        });

        // Demanda
        modelBuilder.Entity<Entities.Demanda.Demanda>(d =>
        {
            d.HasKey(x => x.DemandaId);

            d.Property(p => p.TipoFreteEnum).HasColumnName("TipoFreteId");
        });

        // Pedido
        modelBuilder.Entity<Entities.Pedido.Pedido>(p =>
        {
            p.HasKey(x => x.PedidoId);

            p.Property(x => x.TipoFreteEnum).HasColumnName("TipoFreteId");
        });

        // Setup
        modelBuilder.Entity<Entities.Setup.Setup>(s =>
        {
            s.HasKey(x => x.SetupId);

            s.Property(x => x.ModoCapacidade).HasColumnName("ModoCapacidadeId");
        });

        modelBuilder.Entity<Entities.Setup.SetupOrdemImportancia>(o =>
        {
            o.HasKey(x => x.Id);

            o.Property(x => x.CriterioEnum).HasColumnName("CriterioOrdemId");
            o.Property(x => x.Descricao).HasColumnType("VARCHAR(500)");
        });

        // Otimizador
        modelBuilder.Entity<Entities.Otimizador.CenarioOtimizacaoResultado>(r =>
        {
            r.HasKey(x => x.ResultadoId);
        });

        modelBuilder.Entity<Entities.Otimizador.PedidoOtimizado>(p =>
        {
            p.HasKey(x => x.PedidoId);

            p.Property(x => x.TipoFreteEnum).HasColumnName("TipoFreteId");
        });

        modelBuilder.Entity<Entities.Otimizador.PedidoOtimizadoNaoAlocado>(n =>
        {
            n.HasKey(x => x.NaoAlocadoId);

            n.Property(x => x.CategoriaEnum).HasColumnName("CategoriaId");
            n.Property(x => x.MotivoEnum).HasColumnName("MotivoId");
        });

        modelBuilder.Entity<Entities.Otimizador.PedidoOtimizadoMotivo>(m =>
        {
            m.HasKey(x => x.Id);

            m.Property(x => x.CategoriaEnum).HasColumnName("CategoriaId");
            m.Property(x => x.MotivoEnum).HasColumnName("MotivoId");
        });

        // Dataset
        modelBuilder.Entity<Entities.Dataset.Centro>(c =>
        {
            c.HasKey(x => x.CentroId);
        });

        modelBuilder.Entity<Entities.Dataset.Produto>(p =>
        {
            p.HasKey(x => x.ProdutoId);
        });

        modelBuilder.Entity<Entities.Dataset.Elegibilidade>(e =>
        {
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<Entities.Dataset.Capacidade>(c =>
        {
            c.HasKey(x => x.Id);
        });

        modelBuilder.Entity<Entities.Dataset.Carteira>(c =>
        {
            c.HasKey(x => x.CarteiraId);
        });
    }
}