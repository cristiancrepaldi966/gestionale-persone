using GestionalePersone.Data;
using GestionalePersone.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Services;

// Punti della Fantapolitica, calcolati dagli eventi.
// - Un politico prende i punti di ogni evento in cui è coinvolto.
// - Una squadra prende i punti dei politici che ha adesso, per gli eventi dal giorno
//   dell'acquisto compreso. Chi esce dalla squadra si porta via i suoi punti (lo storico
//   delle vendite resta nel database, ma non conta).
public class PunteggiFantapolitica
{
    // Eventi di ogni politico: (data, punti).
    private readonly Dictionary<int, List<(DateOnly Data, int Punti)>> _eventi;

    private PunteggiFantapolitica(Dictionary<int, List<(DateOnly, int)>> eventi) => _eventi = eventi;

    public static async Task<PunteggiFantapolitica> Carica(AppDbContext context)
    {
        var righe = await context.EventiPolitici.AsNoTracking()
            .Select(ep => new { ep.PoliticoId, ep.Evento.Data, ep.Evento.Punti })
            .ToListAsync();
        return new(righe.GroupBy(r => r.PoliticoId)
            .ToDictionary(g => g.Key, g => g.Select(r => (r.Data, r.Punti)).ToList()));
    }

    // Tutti i punti presi dal politico.
    public int DelPolitico(int politicoId) =>
        _eventi.TryGetValue(politicoId, out var eventi) ? eventi.Sum(e => e.Punti) : 0;

    // Punti portati alla squadra da un politico che c'è adesso (0 se è stato venduto).
    public int DellAcquisto(PoliticoSquadra ps) =>
        ps.InSquadra && _eventi.TryGetValue(ps.PoliticoId, out var eventi)
            ? eventi.Where(e => e.Data >= ps.AcquistatoIl).Sum(e => e.Punti)
            : 0;

    // Punti della squadra: solo i politici che ha adesso.
    public int DellaSquadra(Squadra s) => s.Politici.Where(ps => ps.InSquadra).Sum(DellAcquisto);

    // Il giorno di oggi in Italia (acquisti, vendite e data proposta per gli eventi).
    public static DateOnly Oggi()
    {
        TimeZoneInfo fuso;
        try { fuso = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome"); }
        catch (TimeZoneNotFoundException) { fuso = TimeZoneInfo.Utc; }
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fuso));
    }
}
