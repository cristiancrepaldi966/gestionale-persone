namespace GestionalePersone.Models;

// Livelli di accesso a un modulo. Sopra a tutti c'è il Super admin (Utente.SuperAdmin),
// che vede tutti i moduli come Amministratore e l'unico ad accedere al modulo Admin.
public static class Livelli
{
    public const string Utente = "Utente";
    public const string Amministratore = "Amministratore";
}

// Un modulo dell'app: un'area con le sue pagine e le sue API.
// Livelli vuoti = modulo non assegnabile (solo Super admin).
public record DefinizioneModulo(string Codice, string Nome, string Descrizione, string[] Livelli);

// Elenco dei moduli. Per aggiungerne uno: una riga qui, una regola in Services/Accesso.cs
// (Politiche) e le sue pagine con avviaPagina({ modulo: "codice" }). Chi vi accede e con
// che livello si decide in Admin → Moduli.
public static class Moduli
{
    public const string Finanze = "finanze";
    public const string Fantapolitica = "fantapolitica";
    public const string Mercato = "mercato";
    public const string Admin = "admin";

    public static readonly DefinizioneModulo[] Tutti =
    [
        new(Admin, "Admin", "Utenti e autorizzazioni ai moduli (solo Super admin)", []),
        new(Finanze, "Gestione finanze", "Dashboard, uscite e categorie",
            [Livelli.Utente, Livelli.Amministratore]),
        new(Fantapolitica, "Fantapolitica", "Squadre di politici, eventi e classifica",
            [Livelli.Utente, Livelli.Amministratore]),
        new(Mercato, "Analisi di mercato", "Posizionamento, piano di crescita e investimento dei moduli",
            [Livelli.Utente, Livelli.Amministratore]),
    ];

    // Moduli a cui si possono autorizzare le persone.
    public static IEnumerable<DefinizioneModulo> Assegnabili => Tutti.Where(m => m.Livelli.Length > 0);

    public static DefinizioneModulo? Trova(string? codice) =>
        Tutti.FirstOrDefault(m => m.Codice == codice);

    public static bool LivelloValido(string modulo, string livello) =>
        Trova(modulo)?.Livelli.Contains(livello) == true;
}
