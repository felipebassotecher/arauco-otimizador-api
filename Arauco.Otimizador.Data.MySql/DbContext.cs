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

    // Otimizador
    public DbSet<Entities.Otimizador.CenarioOtimizacaoResultado> CenarioOtimizacaoResultado { get; set; }
    public DbSet<Entities.Otimizador.OtimizacaoAlocacao> OtimizacaoAlocacao { get; set; }
    public DbSet<Entities.Otimizador.OtimizacaoNaoAlocado> OtimizacaoNaoAlocado { get; set; }
    public DbSet<Entities.Otimizador.OtimizacaoEmbarque> OtimizacaoEmbarque { get; set; }
    public DbSet<Entities.Otimizador.OtimizacaoOcupacao> OtimizacaoOcupacao { get; set; }
    public DbSet<Entities.Otimizador.OtimizacaoCriterio> OtimizacaoCriterio { get; set; }

    // OtimizadorV2
    public DbSet<Entities.OtimizadorV2.CenarioOtimizacaoV2Resultado> CenarioOtimizacaoV2Resultado { get; set; }
    public DbSet<Entities.OtimizadorV2.PedidoV2> PedidoV2 { get; set; }
    public DbSet<Entities.OtimizadorV2.PedidoV2NaoAlocado> PedidoV2NaoAlocado { get; set; }

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
            d.Property(p => p.SegmentoEnum).HasColumnName("SegmentoId");
        });

        // Pedido
        modelBuilder.Entity<Entities.Pedido.Pedido>(p =>
        {
            p.HasKey(x => x.PedidoId);

            p.Property(x => x.TipoFreteEnum).HasColumnName("TipoFreteId");
        });

        // Otimizador
        modelBuilder.Entity<Entities.Otimizador.CenarioOtimizacaoResultado>(r =>
        {
            r.HasKey(x => x.ResultadoId);
        });

        modelBuilder.Entity<Entities.Otimizador.OtimizacaoAlocacao>(a =>
        {
            a.HasKey(x => x.AlocacaoId);
        });

        modelBuilder.Entity<Entities.Otimizador.OtimizacaoNaoAlocado>(n =>
        {
            n.HasKey(x => x.NaoAlocadoId);
        });

        modelBuilder.Entity<Entities.Otimizador.OtimizacaoEmbarque>(e =>
        {
            e.HasKey(x => x.EmbarqueId);
        });

        modelBuilder.Entity<Entities.Otimizador.OtimizacaoOcupacao>(o =>
        {
            o.HasKey(x => x.OcupacaoId);
        });

        modelBuilder.Entity<Entities.Otimizador.OtimizacaoCriterio>(c =>
        {
            c.HasKey(x => x.CriterioId);
        });

        // OtimizadorV2
        modelBuilder.Entity<Entities.OtimizadorV2.CenarioOtimizacaoV2Resultado>(r =>
        {
            r.HasKey(x => x.ResultadoId);
        });

        modelBuilder.Entity<Entities.OtimizadorV2.PedidoV2>(p =>
        {
            p.HasKey(x => x.PedidoId);

            p.Property(x => x.TipoFreteEnum).HasColumnName("TipoFreteId");
        });

        modelBuilder.Entity<Entities.OtimizadorV2.PedidoV2NaoAlocado>(n =>
        {
            n.HasKey(x => x.NaoAlocadoId);
        });
    }
}