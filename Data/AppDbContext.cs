using GestionalePersone.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Data;

public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Uscita> Uscite => Set<Uscita>();

    public DbSet<Categoria> Categorie => Set<Categoria>();

    public DbSet<Utente> Utenti => Set<Utente>();

    // Chiavi che firmano i cookie di accesso: salvate nel database così
    // gli utenti restano collegati anche dopo un riavvio o un deploy.
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Utente>(u =>
        {
            u.Property(x => x.Email).HasMaxLength(256).IsRequired();
            u.Property(x => x.Nome).HasMaxLength(100).IsRequired().HasDefaultValue("");
            u.Property(x => x.Cognome).HasMaxLength(100).IsRequired().HasDefaultValue("");
            u.HasIndex(x => x.Email).IsUnique();
            u.Property(x => x.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Categoria>(c =>
        {
            c.Property(x => x.Nome).HasMaxLength(50).IsRequired();
            c.HasIndex(x => x.Nome).IsUnique();
            c.HasData(
                new Categoria { Id = 1, Nome = "Casa" },
                new Categoria { Id = 2, Nome = "Spesa alimentare" },
                new Categoria { Id = 3, Nome = "Bollette" },
                new Categoria { Id = 4, Nome = "Trasporti" },
                new Categoria { Id = 5, Nome = "Salute" },
                new Categoria { Id = 6, Nome = "Svago" },
                new Categoria { Id = 7, Nome = "Altro" });
        });

        modelBuilder.Entity<Uscita>(u =>
        {
            u.Property(x => x.Importo).HasPrecision(12, 2);
            u.Property(x => x.Note).HasMaxLength(1000);
            u.HasIndex(x => x.Data);

            // Una categoria usata da qualche uscita non si può eliminare.
            u.HasOne(x => x.Categoria).WithMany()
                .HasForeignKey(x => x.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            u.HasOne(x => x.CreatoDa).WithMany()
                .HasForeignKey(x => x.CreatoDaId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
