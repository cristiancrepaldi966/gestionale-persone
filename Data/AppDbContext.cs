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

    public DbSet<Autorizzazione> Autorizzazioni => Set<Autorizzazione>();

    // Progetti del modulo Stampa 3D (oggetto del catalogo e misure).
    public DbSet<Progetto3d> Progetti3d => Set<Progetto3d>();

    // Leghe del modulo Fantapolitica e le persone che ne fanno parte.
    public DbSet<Lega> Leghe => Set<Lega>();

    public DbSet<MembroLega> MembriLeghe => Set<MembroLega>();

    // Politici (gestiti dal Super admin) e squadre dei membri delle leghe.
    public DbSet<Politico> Politici => Set<Politico>();

    public DbSet<Squadra> Squadre => Set<Squadra>();

    public DbSet<PoliticoSquadra> PoliticiSquadre => Set<PoliticoSquadra>();

    public DbSet<ImpostazioniFantapolitica> ImpostazioniFantapolitica => Set<ImpostazioniFantapolitica>();

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

        modelBuilder.Entity<Autorizzazione>(a =>
        {
            a.HasKey(x => new { x.UtenteId, x.Modulo });
            a.Property(x => x.Modulo).HasMaxLength(30);
            a.Property(x => x.Livello).HasMaxLength(20).IsRequired();
            a.HasOne(x => x.Utente).WithMany(u => u.Autorizzazioni)
                .HasForeignKey(x => x.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Progetto3d>(p =>
        {
            p.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            p.Property(x => x.Oggetto).HasMaxLength(40).IsRequired();
            p.Property(x => x.Parametri).HasColumnType("jsonb").IsRequired();
            p.HasIndex(x => x.CreatoDaId);
            p.HasOne(x => x.CreatoDa).WithMany()
                .HasForeignKey(x => x.CreatoDaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Lega>(l =>
        {
            l.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            l.HasIndex(x => x.CreatoDaId);
            l.HasOne(x => x.CreatoDa).WithMany()
                .HasForeignKey(x => x.CreatoDaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MembroLega>(m =>
        {
            m.HasKey(x => new { x.LegaId, x.UtenteId });
            m.HasOne(x => x.Lega).WithMany(l => l.Membri)
                .HasForeignKey(x => x.LegaId)
                .OnDelete(DeleteBehavior.Cascade);
            m.HasOne(x => x.Utente).WithMany()
                .HasForeignKey(x => x.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Politico>(p =>
        {
            p.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            p.Property(x => x.Cognome).HasMaxLength(100).IsRequired();
            p.Property(x => x.Ruolo).HasMaxLength(30).IsRequired();
            p.Property(x => x.Partito).HasMaxLength(100);
            // Politici iniziali: amministrazione comunale di Adria (fonti pubbliche, 2023).
            p.HasData(
                new Politico { Id = 1, Nome = "Massimo", Cognome = "Barbujani", Ruolo = RuoliPolitico.Sindaco, Costo = 35, Partito = "Lista Bobo" },
                new Politico { Id = 2, Nome = "Federico", Cognome = "Simoni", Ruolo = RuoliPolitico.Vicesindaco, Costo = 25 },
                new Politico { Id = 3, Nome = "Giselda", Cognome = "Baratella", Ruolo = RuoliPolitico.Assessore, Costo = 20 },
                new Politico { Id = 4, Nome = "Giorgio", Cognome = "Crepaldi", Ruolo = RuoliPolitico.Assessore, Costo = 20 },
                new Politico { Id = 5, Nome = "Giorgio", Cognome = "D'Angelo", Ruolo = RuoliPolitico.Assessore, Costo = 20, Partito = "Lega" },
                new Politico { Id = 6, Nome = "Vittoria", Cognome = "Paccagnella", Ruolo = RuoliPolitico.Assessore, Costo = 20 },
                new Politico { Id = 7, Nome = "Antonella", Cognome = "Ravagnan", Ruolo = RuoliPolitico.Assessore, Costo = 20 },
                new Politico { Id = 8, Nome = "Omar", Cognome = "Barbierato", Ruolo = RuoliPolitico.Consigliere, Costo = 10, Partito = "Impegno per il bene comune" },
                new Politico { Id = 9, Nome = "Enrico", Cognome = "Bonato", Ruolo = RuoliPolitico.Consigliere, Costo = 10, Partito = "Impegno per il bene comune" },
                new Politico { Id = 10, Nome = "Sandra", Cognome = "Moda", Ruolo = RuoliPolitico.Consigliere, Costo = 10, Partito = "Impegno per il bene comune" },
                new Politico { Id = 11, Nome = "Giorgio", Cognome = "Zanellato", Ruolo = RuoliPolitico.Consigliere, Costo = 10, Partito = "PD" });
        });

        modelBuilder.Entity<ImpostazioniFantapolitica>(i =>
        {
            i.Property(x => x.Id).ValueGeneratedNever();
            i.HasData(new ImpostazioniFantapolitica { Id = Models.ImpostazioniFantapolitica.IdUnico, CreditiPerSquadra = 100 });
        });

        modelBuilder.Entity<Squadra>(s =>
        {
            s.Property(x => x.Nome).HasMaxLength(50).IsRequired();
            s.HasIndex(x => new { x.LegaId, x.UtenteId }).IsUnique();
            s.HasIndex(x => x.UtenteId);
            // La squadra appartiene a un membro della lega: se la persona esce dalla lega,
            // o la lega viene eliminata, la squadra se ne va con lei.
            s.HasOne<MembroLega>().WithOne()
                .HasForeignKey<Squadra>(x => new { x.LegaId, x.UtenteId })
                .OnDelete(DeleteBehavior.Cascade);
            s.HasOne(x => x.Utente).WithMany()
                .HasForeignKey(x => x.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PoliticoSquadra>(ps =>
        {
            ps.HasKey(x => new { x.SquadraId, x.PoliticoId });
            ps.HasOne(x => x.Squadra).WithMany(s => s.Politici)
                .HasForeignKey(x => x.SquadraId)
                .OnDelete(DeleteBehavior.Cascade);
            // Un politico eliminato esce dalle squadre che lo avevano scelto.
            ps.HasOne(x => x.Politico).WithMany()
                .HasForeignKey(x => x.PoliticoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Categoria>(c =>
        {
            c.Property(x => x.Nome).HasMaxLength(50).IsRequired();
            c.HasIndex(x => x.Nome).IsUnique();
            c.Property(x => x.Icona).HasMaxLength(40).IsRequired().HasDefaultValue(AspettoCategoria.IconaPredefinita);
            c.Property(x => x.Colore).HasMaxLength(7).IsRequired().HasDefaultValue(AspettoCategoria.ColorePredefinito);
            c.HasData(
                new Categoria { Id = 1, Nome = "Casa", Icona = "house-door", Colore = "#2a78d6" },
                new Categoria { Id = 2, Nome = "Spesa alimentare", Icona = "cart3", Colore = "#eb6834" },
                new Categoria { Id = 3, Nome = "Bollette", Icona = "lightning-charge", Colore = "#eda100" },
                new Categoria { Id = 4, Nome = "Trasporti", Icona = "car-front", Colore = "#1baf7a" },
                new Categoria { Id = 5, Nome = "Salute", Icona = "heart-pulse", Colore = "#e34948" },
                new Categoria { Id = 6, Nome = "Svago", Icona = "controller", Colore = "#e87ba4" },
                new Categoria { Id = 7, Nome = "Altro", Icona = "tag", Colore = "#6c757d" });
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
