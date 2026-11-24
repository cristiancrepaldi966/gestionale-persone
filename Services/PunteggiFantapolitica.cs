using GestionalePersone.Data;
using GestionalePersone.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Services;

// Punti della Fantapolitica, calcolati dagli eventi.
// - In ogni evento un politico prende il suo voto (se non è "senza voto") più i bonus e
//   malus del listino che gli sono stati dati. I punti possono avere il mezzo punto.
// - Una squadra prende i punti dei politici che ha adesso, per gli eventi dal giorno
//   dell'acquisto compreso. Chi esce dalla squadra si porta via i suoi punti (lo storico
//   delle vendite resta nel database, ma non conta).
public class PunteggiFantapolitica
{
    // Per ogni politico, i punti presi in ogni evento: (data, punti).
    private readonly Dictionary<int, List<(DateOnly Data, decimal Punti)>> _eventi;

    private PunteggiFantapolitica(Dictionary<int, List<(DateOnly, decimal)>> eventi) => _eventi = eventi;

    public static async Task<PunteggiFantapolitica> Carica(AppDbContext context)
    {
        var voti = await context.VotiEventi.AsNoTracking()
            .Select(v => new { v.PoliticoId, v.EventoId, v.Evento.Data, Punti = v.Voto })
            .ToListAsync();
        var bonus = await context.BonusEventi.AsNoTracking()
            .Select(b => new { b.PoliticoId, b.EventoId, b.Evento.Data, Punti = (decimal)b.Punti })
            .ToListAsync();
        return new(voti.Concat(bonus)
            .GroupBy(r => (r.PoliticoId, r.EventoId, r.Data))
            .GroupBy(g => g.Key.PoliticoId)
            .ToDictionary(g => g.Key, g => g.Select(e => (e.Key.Data, e.Sum(r => r.Punti))).ToList()));
    }

    // Punti di un politico in un evento: voto (se c'è) più bonus e malus.
    public static decimal NellEvento(decimal? voto, IEnumerable<int> bonus) => (voto ?? 0) + bonus.Sum();

    // Tutti i punti presi dal politico.
    public decimal DelPolitico(int politicoId) =>
        _eventi.TryGetValue(politicoId, out var eventi) ? eventi.Sum(e => e.Punti) : 0;

    // Punti portati alla squadra da un politico che c'è adesso (0 se è stato venduto).
    public decimal DellAcquisto(PoliticoSquadra ps) =>
        ps.InSquadra && _eventi.TryGetValue(ps.PoliticoId, out var eventi)
            ? eventi.Where(e => e.Data >= ps.AcquistatoIl).Sum(e => e.Punti)
            : 0;

    // Punti della squadra: solo i politici che ha adesso.
    public decimal DellaSquadra(Squadra s) => s.Politici.Where(ps => ps.InSquadra).Sum(DellAcquisto);

    // Il giorno di oggi in Italia (acquisti, vendite e data proposta per gli eventi).
    public static DateOnly Oggi()
    {
        TimeZoneInfo fuso;
        try { fuso = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome"); }
        catch (TimeZoneNotFoundException) { fuso = TimeZoneInfo.Utc; }
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fuso));
    }
}
