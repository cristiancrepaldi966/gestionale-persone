using GestionalePersone.Models;

namespace GestionalePersone.Services;

// Simulazione mese per mese della crescita di un modulo: utenti, ricavi, soldi spesi e
// lavoro. Modello volutamente semplice, per confrontare ipotesi diverse:
//
//   nuovi da marketing = budget / costo per utente
//   nuovi organici     = utenti × crescita organica
//   (entrambi rallentano avvicinandosi al mercato raggiungibile)
//   persi              = utenti × abbandono
//   ricavi             = paganti × prezzo + utenti × ricavo pubblicitario
//   uscite di cassa    = budget di marketing + costi fissi
//   lavoro             = ore settimanali × 52/12 (valore figurativo: ore × costo orario)
public static class Proiezione
{
    public const double SettimaneAlMese = 52.0 / 12.0;

    public record Mese(int Numero, int Utenti, int Nuovi, int Persi, int Paganti,
        decimal Ricavi, decimal UsciteCassa, decimal CostoLavoro, decimal Saldo, decimal SaldoCumulato);

    public record Riepilogo(
        int UtentiFinali,
        int PagantiFinali,
        decimal RicaviMensiliFinali,
        decimal TotaleRicavi,
        decimal TotaleUsciteCassa,       // soldi da investire nel periodo
        decimal FabbisognoMassimo,       // la perdita cumulata più alta: i soldi da tenere pronti
        int OreTotali,
        decimal ValoreLavoro,
        int? MesePareggio,               // primo mese in cui i ricavi coprono le uscite di cassa
        int? MeseRientro,                // primo mese in cui il saldo cumulato torna positivo
        decimal CostoPerUtenteFinale,    // uscite di cassa / utenti finali
        decimal QuotaMercato);           // utenti finali / mercato raggiungibile, in %

    public record Risultato(ParametriSimulazione Parametri, long MercatoRaggiungibile, Riepilogo Riepilogo, IReadOnlyList<Mese> Mesi);

    public static Risultato Calcola(ParametriSimulazione p, long mercatoRaggiungibile)
    {
        var tetto = Math.Max(1.0, mercatoRaggiungibile);
        double utenti = Math.Min(p.UtentiIniziali, tetto);

        var prezzo = (double)p.PrezzoMensile;
        var quotaPaganti = (double)p.QuotaPaganti / 100;
        var pubblicita = (double)p.RicavoPubblicitario;
        var budget = (double)p.BudgetMarketing;
        var crescita = (double)p.CrescitaOrganica / 100;
        var abbandono = (double)p.Abbandono / 100;
        var uscite = budget + (double)p.CostiFissi;
        var oreMese = (double)p.OreSettimanali * SettimaneAlMese;
        var costoLavoro = oreMese * (double)p.CostoOrario;

        var mesi = new List<Mese>(p.Mesi);
        double cumulato = 0, fabbisogno = 0, totaleRicavi = 0;
        int? pareggio = null, rientro = null;

        for (var m = 1; m <= p.Mesi; m++)
        {
            var spazio = Math.Max(0, 1 - utenti / tetto);            // quanto mercato resta
            var nuovi = (budget / (double)p.CostoPerUtente + utenti * crescita) * spazio;
            var persi = utenti * abbandono;
            utenti = Math.Clamp(utenti + nuovi - persi, 0, tetto);

            var paganti = utenti * quotaPaganti;
            var ricavi = paganti * prezzo + utenti * pubblicita;
            var saldo = ricavi - uscite;
            cumulato += saldo;
            totaleRicavi += ricavi;
            fabbisogno = Math.Min(fabbisogno, cumulato);

            if (pareggio == null && uscite > 0 && saldo >= 0 && utenti >= 1) pareggio = m;
            if (rientro == null && cumulato >= 0 && m > 1 && fabbisogno < 0) rientro = m;

            mesi.Add(new Mese(m, (int)Math.Round(utenti), (int)Math.Round(nuovi), (int)Math.Round(persi),
                (int)Math.Round(paganti), Euro(ricavi), Euro(uscite), Euro(costoLavoro), Euro(saldo), Euro(cumulato)));
        }

        var totaleUscite = uscite * p.Mesi;
        var finale = mesi[^1];
        var riepilogo = new Riepilogo(
            finale.Utenti,
            finale.Paganti,
            finale.Ricavi,
            Euro(totaleRicavi),
            Euro(totaleUscite),
            Euro(-fabbisogno),
            (int)Math.Round(oreMese * p.Mesi),
            Euro(costoLavoro * p.Mesi),
            pareggio,
            rientro,
            finale.Utenti > 0 ? Euro(totaleUscite / finale.Utenti) : 0,
            Math.Round((decimal)(finale.Utenti / tetto * 100), 2));

        return new Risultato(p, mercatoRaggiungibile, riepilogo, mesi);
    }

    private static decimal Euro(double v) => Math.Round((decimal)v, 2);
}
