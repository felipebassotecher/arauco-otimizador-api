using Microsoft.EntityFrameworkCore;

namespace Arauco.Otimizador.Data.MySql;

public class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbContext(DbContextOptions<DbContext> options) : base(options)
    {
    }

    // Cartao
    public DbSet<Entities.Cartao.Cartao> Cartao { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cartao
        modelBuilder.Entity<Entities.Cartao.Cartao>(u =>
        {
            u.HasKey(c => c.CartaoId);

            u.Property(p => p.TipoEnum).HasColumnName("TipoCartaoId");
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