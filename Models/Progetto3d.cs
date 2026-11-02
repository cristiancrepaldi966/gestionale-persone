namespace GestionalePersone.Models;

// Una configurazione salvata nel modulo Stampa 3D: oggetto del catalogo e sue misure.
public class Progetto3d
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    // Codice dell'oggetto del catalogo (Services/CatalogoStampa3d.cs), es. "targhetta".
    public string Oggetto { get; set; } = string.Empty;

    // Misure in JSON, già controllate: { "testo": "CASA", "larghezza": 100, ... }
    public string Parametri { get; set; } = "{}";

    // Chi ha creato il progetto (null se l'utente è stato eliminato).
    public int? CreatoDaId { get; set; }

    public Utente? CreatoDa { get; set; }

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;

    public DateTime AggiornatoIl { get; set; } = DateTime.UtcNow;
}
