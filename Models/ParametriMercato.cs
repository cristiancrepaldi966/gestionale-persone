using System.ComponentModel.DataAnnotations;

namespace GestionalePersone.Models;

// Ipotesi della simulazione di crescita di un modulo (modulo Analisi di mercato).
// Le percentuali sono numeri da 0 a 100; gli importi sono in euro.
// È anche il corpo delle richieste di simulazione: dal client arrivano solo questi campi.
public class ParametriSimulazione
{
    [Range(0, 1_000_000, ErrorMessage = "Gli utenti iniziali devono essere tra 0 e 1.000.000.")]
    public int UtentiIniziali { get; set; }

    [Range(0, 1000, ErrorMessage = "Il prezzo mensile deve essere tra 0 e 1.000 €.")]
    public decimal PrezzoMensile { get; set; }

    [Range(0, 100, ErrorMessage = "La quota di paganti deve essere tra 0 e 100%.")]
    public decimal QuotaPaganti { get; set; }

    // Ricavo da pubblicità o sponsor per ogni utente attivo, al mese.
    [Range(0, 10, ErrorMessage = "Il ricavo pubblicitario deve essere tra 0 e 10 € per utente.")]
    public decimal RicavoPubblicitario { get; set; }

    // Budget di marketing a pagamento, al mese.
    [Range(0, 1_000_000, ErrorMessage = "Il budget di marketing deve essere tra 0 e 1.000.000 € al mese.")]
    public decimal BudgetMarketing { get; set; }

    // Costo medio per portare un nuovo utente con il marketing a pagamento (CAC).
    [Range(0.1, 1000, ErrorMessage = "Il costo per utente deve essere tra 0,10 e 1.000 €.")]
    public decimal CostoPerUtente { get; set; } = 1;

    // Nuovi utenti al mese portati da quelli esistenti (passaparola, inviti), in % degli utenti.
    [Range(0, 50, ErrorMessage = "La crescita organica deve essere tra 0 e 50% al mese.")]
    public decimal CrescitaOrganica { get; set; }

    // Utenti che smettono di usare il modulo ogni mese, in %.
    [Range(0, 100, ErrorMessage = "L'abbandono deve essere tra 0 e 100% al mese.")]
    public decimal Abbandono { get; set; }

    [Range(0, 100, ErrorMessage = "Le ore settimanali devono essere tra 0 e 100.")]
    public decimal OreSettimanali { get; set; }

    // Valore di un'ora di lavoro (costo figurativo, non esce dalla cassa).
    [Range(0, 500, ErrorMessage = "Il costo orario deve essere tra 0 e 500 €.")]
    public decimal CostoOrario { get; set; }

    // Hosting, dominio, strumenti, store: costi che si pagano ogni mese.
    [Range(0, 1_000_000, ErrorMessage = "I costi fissi devono essere tra 0 e 1.000.000 € al mese.")]
    public decimal CostiFissi { get; set; }

    [Range(6, 60, ErrorMessage = "La durata deve essere tra 6 e 60 mesi.")]
    public int Mesi { get; set; } = 24;

    public ParametriSimulazione Copia() => new()
    {
        UtentiIniziali = UtentiIniziali,
        PrezzoMensile = PrezzoMensile,
        QuotaPaganti = QuotaPaganti,
        RicavoPubblicitario = RicavoPubblicitario,
        BudgetMarketing = BudgetMarketing,
        CostoPerUtente = CostoPerUtente,
        CrescitaOrganica = CrescitaOrganica,
        Abbandono = Abbandono,
        OreSettimanali = OreSettimanali,
        CostoOrario = CostoOrario,
        CostiFissi = CostiFissi,
        Mesi = Mesi,
    };

    public void CopiaIn(ParametriSimulazione destinazione)
    {
        destinazione.UtentiIniziali = UtentiIniziali;
        destinazione.PrezzoMensile = PrezzoMensile;
        destinazione.QuotaPaganti = QuotaPaganti;
        destinazione.RicavoPubblicitario = RicavoPubblicitario;
        destinazione.BudgetMarketing = BudgetMarketing;
        destinazione.CostoPerUtente = CostoPerUtente;
        destinazione.CrescitaOrganica = CrescitaOrganica;
        destinazione.Abbandono = Abbandono;
        destinazione.OreSettimanali = OreSettimanali;
        destinazione.CostoOrario = CostoOrario;
        destinazione.CostiFissi = CostiFissi;
        destinazione.Mesi = Mesi;
    }
}

// Ipotesi salvate dall'Amministratore di Analisi di mercato per un modulo
// (una riga per modulo). Se manca, valgono quelle predefinite della scheda.
public class ParametriMercato : ParametriSimulazione
{
    public string Modulo { get; set; } = string.Empty;

    public DateTime AggiornatoIl { get; set; }

    public int? AggiornatoDaId { get; set; }

    public Utente? AggiornatoDa { get; set; }
}
