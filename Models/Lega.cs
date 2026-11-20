namespace GestionalePersone.Models;

// Una lega del modulo Fantapolitica: un gruppo di persone scelte tra gli utenti abilitati
// al modulo. La crea qualsiasi utente del modulo, che ne fa sempre parte; la modificano
// ed eliminano solo chi l'ha creata e il Super admin.
public class Lega
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    // Chi ha creato la lega (null se l'utente è stato eliminato).
    public int? CreatoDaId { get; set; }

    public Utente? CreatoDa { get; set; }

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;

    public List<MembroLega> Membri { get; set; } = [];
}

// Una persona che fa parte di una lega (una persona può essere in più leghe).
public class MembroLega
{
    public int LegaId { get; set; }

    public Lega Lega { get; set; } = null!;

    public int UtenteId { get; set; }

    public Utente Utente { get; set; } = null!;
}
