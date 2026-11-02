namespace GestionalePersone.Models;

// Livelli di accesso a un modulo.
public static class Livelli
{
    public const string Utente = "Utente";
    public const string Amministratore = "Amministratore";
}

// Un modulo dell'app: un'area con le sue pagine e le sue API.
public record DefinizioneModulo(string Codice, string Nome, string Descrizione, string[] Livelli);

// Elenco dei moduli. Per aggiungerne uno: una riga qui, una regola in Services/Accesso.cs
// (Politiche) e le sue pagine con avviaPagina({ modulo: "codice" }). Chi vi accede e con
// che livello si decide in Admin → Moduli.
public static class Moduli
{
    public const string Uscite = "uscite";
    public const string Admin = "admin";

    public static readonly DefinizioneModulo[] Tutti =
    [
        new(Uscite, "Uscite", "Dashboard, uscite e categorie",
            [Livelli.Utente, Livelli.Amministratore]),
        new(Admin, "Admin", "Utenti e autorizzazioni ai moduli",
            [Livelli.Amministratore]),
    ];

    public static DefinizioneModulo? Trova(string? codice) =>
        Tutti.FirstOrDefault(m => m.Codice == codice);

    public static bool LivelloValido(string modulo, string livello) =>
        Trova(modulo)?.Livelli.Contains(livello) == true;
}
