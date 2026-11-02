namespace GestionalePersone.Models;

public class Utente
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Cognome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Hash PBKDF2 generato da PasswordHasher di ASP.NET Core: la password non è mai salvata in chiaro.
    public string PasswordHash { get; set; } = string.Empty;

    // Livello di accesso a ciascun modulo (vedi Moduli).
    public List<Autorizzazione> Autorizzazioni { get; set; } = [];

    public bool Attivo { get; set; } = true;

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;

    public DateTime? UltimoAccesso { get; set; }

    // Cambia quando la password viene cambiata: le sessioni aperte con la vecchia password scadono.
    public int VersioneSessione { get; set; }

    // Protezione da tentativi ripetuti.
    public int TentativiFalliti { get; set; }

    public DateTime? BloccatoFinoA { get; set; }
}
