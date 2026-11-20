namespace GestionalePersone.Models;

// Una voce del listino dei bonus e malus (es. "Intervento in consiglio", +2).
// Il listino lo gestisce il Super admin.
public class VocePunteggio
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    // Punti dati a ogni politico coinvolto (negativi per i malus).
    public int Punti { get; set; }
}

// Un fatto che dà punti ai politici coinvolti, secondo una voce del listino.
// Lo registra il Super admin.
public class Evento
{
    public int Id { get; set; }

    public DateOnly Data { get; set; }

    public int VocePunteggioId { get; set; }

    public VocePunteggio Voce { get; set; } = null!;

    // Punti della voce al momento del salvataggio: se poi il listino cambia,
    // gli eventi già registrati restano come sono.
    public int Punti { get; set; }

    // Dettaglio facoltativo (es. "Consiglio del 12/11, intervento sul bilancio").
    public string? Descrizione { get; set; }

    public List<EventoPolitico> Politici { get; set; } = [];

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;
}

// Un politico coinvolto in un evento.
public class EventoPolitico
{
    public int EventoId { get; set; }

    public Evento Evento { get; set; } = null!;

    public int PoliticoId { get; set; }

    public Politico Politico { get; set; } = null!;
}
