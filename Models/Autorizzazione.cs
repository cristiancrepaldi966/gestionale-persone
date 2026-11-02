namespace GestionalePersone.Models;

// Livello di accesso di una persona a un modulo (nessuna riga = nessun accesso).
public class Autorizzazione
{
    public int UtenteId { get; set; }

    public Utente Utente { get; set; } = null!;

    // Codice del modulo (vedi Moduli).
    public string Modulo { get; set; } = string.Empty;

    // Livello (vedi Livelli).
    public string Livello { get; set; } = string.Empty;
}
