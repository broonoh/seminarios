using Microsoft.EntityFrameworkCore;
using SeminarioMobile.Models;

namespace SeminarioMobile.Data;

public class AppDbContext : DbContext
{
    public DbSet<Local> Locais => Set<Local>();
    public DbSet<Classe> Classes => Set<Classe>();
    public DbSet<Igreja> Igrejas => Set<Igreja>();
    public DbSet<Observacao> Observacoes => Set<Observacao>();
    public DbSet<Seminario> Seminarios => Set<Seminario>();

    public static string CaminhoBanco =>
        Path.Combine(FileSystem.AppDataDirectory, "seminario.db3");

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Filename={CaminhoBanco}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Local>().HasIndex(l => l.Nome).IsUnique();
        modelBuilder.Entity<Classe>().HasIndex(c => c.Nome).IsUnique();
        modelBuilder.Entity<Igreja>().HasIndex(i => i.Nome).IsUnique();

        modelBuilder.Entity<Seminario>()
            .HasOne(s => s.Local)
            .WithMany()
            .HasForeignKey(s => s.LocalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Seminario>()
            .HasOne(s => s.Classe)
            .WithMany()
            .HasForeignKey(s => s.ClasseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Seminario>()
            .Property(s => s.ValorAdulto)
            .HasColumnType("decimal(10,2)");

        modelBuilder.Entity<Seminario>()
            .Property(s => s.ValorCrianca)
            .HasColumnType("decimal(10,2)");
    }
}
