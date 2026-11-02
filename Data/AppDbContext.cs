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

    public DbSet<Persona> Persone => Set<Persona>();

    public DbSet<Utente> Utenti => Set<Utente>();

    // Chiavi che firmano i cookie di accesso: salvate nel database così
    // gli utenti restano collegati anche dopo un riavvio o un deploy.
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Utente>(u =>
        {
            u.Property(x => x.Email).HasMaxLength(256).IsRequired();
            u.HasIndex(x => x.Email).IsUnique();
            u.Property(x => x.PasswordHash).IsRequired();
        });
    }
}
