namespace GestionalePersone.Models;

// Livelli di accesso a un modulo.
public static class Livelli
{
    public const string Utente = "Utente";
    public const string Amministratore = "Amministratore";
}

// Un modulo dell'app: un'area con le sue pagine e le sue API.
public record DefinizioneModulo(string Codice, string Nome, string Descrizione, string[] Livelli);

// Elenco dei moduli. Per aggiungerne uno: una riga qui, una policy in Program.cs
// (Politiche) e le sue pagine con avviaPagina({ modulo: "codice" }).
public static class Moduli
{
    public const string Uscite = "uscite";
    public const string Admin = "admin";

    public static readonly DefinizioneModulo[] Tutti =
    [
        new(Uscite, "Uscite", "Dashboard e registrazione delle uscite",
            [Livelli.Utente, Livelli.Amministratore]),
        new(Admin, "Admin", "Utenti, autorizzazioni e categorie",
            [Livelli.Amministratore]),
    ];

    public static DefinizioneModulo? Trova(string? codice) =>
        Tutti.FirstOrDefault(m => m.Codice == codice);

    public static bool LivelloValido(string modulo, string livello) =>
        Trova(modulo)?.Livelli.Contains(livello) == true;
}
