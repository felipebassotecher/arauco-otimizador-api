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
    public DbSet<Entities.Cenario.CenarioParametro> CenarioParametro { get; set; }

    // Parametro
    public DbSet<Entities.Parametro.Parametro> Parametro { get; set; }
    public DbSet<Entities.Parametro.ParametroValor> ParametroValor { get; set; }

    // Demanda
    public DbSet<Entities.Demanda.Demanda> Demanda { get; set; }

    // Pedido
    public DbSet<Entities.Pedido.Pedido> Pedido { get; set; }

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

        modelBuilder.Entity<Entities.Cenario.CenarioParametro>(c =>
        {
            c.HasKey(x => new { x.CenarioId, x.ParametroId });
        });

        // Parametro
        modelBuilder.Entity<Entities.Parametro.Parametro>(p =>
        {
            p.HasKey(x => x.ParametroId);
        });

        modelBuilder.Entity<Entities.Parametro.ParametroValor>(v =>
        {
            v.HasKey(x => x.Id);
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
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySqlWithSecrets();
    }

    public static DbContext Create()
    {
        var builder = new DbContextOptionsBuilder<DbContext>();

        return new DbContext(builder.Options);
    }
}