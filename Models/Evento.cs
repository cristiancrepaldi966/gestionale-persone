namespace GestionalePersone.Models;

// Una voce del listino dei bonus e malus (es. "Intervento in consiglio", +2).
// Il listino lo gestisce il Super admin.
public class VocePunteggio
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    // Punti dati al politico (negativi per i malus).
    public int Punti { get; set; }
}

// Un evento (una "giornata", es. "Consiglio comunale del 12/11"): ogni politico prende un
// voto da 1 a 10 (anche mezzi voti) oppure resta senza voto, più eventuali bonus e malus
// del listino. Punti del politico = voto + bonus e malus. Lo registra il Super admin.
public class Evento
{
    public const decimal VotoMinimo = 1m;
    public const decimal VotoMassimo = 10m;

    // Voto proposto per tutti quando si crea un evento.
    public const decimal VotoProposto = 6m;

    public int Id { get; set; }

    public DateOnly Data { get; set; }

    public string Titolo { get; set; } = string.Empty;

    // Dettaglio facoltativo.
    public string? Descrizione { get; set; }

    public List<VotoEvento> Voti { get; set; } = [];

    public List<BonusEvento> Bonus { get; set; } = [];

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;

    // Voto valido: da 1 a 10, a passi di mezzo punto.
    public static bool VotoValido(decimal voto) =>
        voto >= VotoMinimo && voto <= VotoMassimo && voto * 2 == decimal.Truncate(voto * 2);
}

// Il voto di un politico in un evento (chi non ha la riga è "senza voto").
public class VotoEvento
{
    public int EventoId { get; set; }

    public Evento Evento { get; set; } = null!;

    public int PoliticoId { get; set; }

    public Politico Politico { get; set; } = null!;

    public decimal Voto { get; set; }
}

// Un bonus o malus del listino dato a un politico in un evento (anche se è senza voto,
// es. "Assenza in consiglio").
public class BonusEvento
{
    public int Id { get; set; }

    public int EventoId { get; set; }

    public Evento Evento { get; set; } = null!;

    public int PoliticoId { get; set; }

    public Politico Politico { get; set; } = null!;

    public int VocePunteggioId { get; set; }

    public VocePunteggio Voce { get; set; } = null!;

    // Punti della voce al momento del salvataggio: se poi il listino cambia, gli eventi
    // già registrati restano come sono.
    public int Punti { get; set; }
}
