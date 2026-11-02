using System.Security.Claims;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

// Modulo Analisi di mercato: per ogni modulo dell'app la scheda di mercato
// (Services/SchedeMercato.cs) e la simulazione di crescita (Services/Proiezione.cs).
// Tutti gli utenti del modulo leggono e provano simulazioni; l'Amministratore salva le ipotesi.
[ApiController]
[Authorize(Policy = Politiche.Mercato)]
[Route("api/mercato")]
public class MercatoController : ControllerBase
{
    private readonly AppDbContext _context;

    public MercatoController(AppDbContext context)
    {
        _context = context;
    }

    public record ConfrontoDto(
        string Modulo, string Nome, string Idea, string Settore,
        long MercatoRaggiungibile, long Obiettivo,
        int OreFasi, decimal CostoFasi, int UtentiObiettivoFasi,
        bool IpotesiPersonalizzate, Proiezione.Riepilogo Riepilogo);

    public record PanoramicaDto(IEnumerable<ConfrontoDto> Moduli, IEnumerable<string> SenzaAnalisi);

    public record IpotesiSalvateDto(DateTime AggiornatoIl, string? AggiornatoDa);

    public record SchedaDto(string Nome, SchedeMercato.Scheda Scheda, ParametriSimulazione Parametri,
        IpotesiSalvateDto? Salvate, Proiezione.Risultato Proiezione);

    // Confronto tra i moduli analizzati, con le ipotesi in vigore per ciascuno.
    [HttpGet]
    public async Task<PanoramicaDto> GetPanoramica()
    {
        var salvati = await _context.ParametriMercato.AsNoTracking().ToDictionaryAsync(p => p.Modulo);

        var moduli = SchedeMercato.Tutte.Select(s =>
        {
            var parametri = salvati.TryGetValue(s.Modulo, out var p) ? p : s.Predefiniti;
            var proiezione = Proiezione.Calcola(parametri, s.Mercato.Raggiungibile.Persone);
            return new ConfrontoDto(s.Modulo, NomeModulo(s.Modulo), s.Idea, s.Settore,
                s.Mercato.Raggiungibile.Persone, s.Mercato.Obiettivo.Persone,
                s.Fasi.Sum(f => f.Ore), s.Fasi.Sum(f => f.Costo), s.Fasi.Max(f => f.UtentiObiettivo),
                p != null, proiezione.Riepilogo);
        }).ToList();

        // Moduli dell'app ancora senza scheda (Admin e questo modulo esclusi).
        var senza = Moduli.Assegnabili
            .Where(m => m.Codice != Moduli.Mercato && SchedeMercato.Trova(m.Codice) == null)
            .Select(m => m.Nome);

        return new PanoramicaDto(moduli, senza);
    }

    // Scheda completa di un modulo con le ipotesi in vigore e la loro proiezione.
    [HttpGet("{modulo}")]
    public async Task<ActionResult<SchedaDto>> GetScheda(string modulo)
    {
        var scheda = SchedeMercato.Trova(modulo);
        if (scheda == null)
            return NotFound(new { messaggio = "Nessuna analisi di mercato per questo modulo." });

        var salvati = await _context.ParametriMercato.AsNoTracking()
            .Where(p => p.Modulo == modulo)
            .Select(p => new { Parametri = p, Autore = p.AggiornatoDa == null ? null : (p.AggiornatoDa.Nome + " " + p.AggiornatoDa.Cognome).Trim() })
            .SingleOrDefaultAsync();

        ParametriSimulazione parametri = salvati?.Parametri.Copia() ?? scheda.Predefiniti;
        var info = salvati == null ? null
            : new IpotesiSalvateDto(salvati.Parametri.AggiornatoIl, string.IsNullOrEmpty(salvati.Autore) ? null : salvati.Autore);

        return new SchedaDto(NomeModulo(modulo), scheda, parametri, info,
            Proiezione.Calcola(parametri, scheda.Mercato.Raggiungibile.Persone));
    }

    // Prova delle ipotesi diverse senza salvarle.
    [HttpPost("{modulo}/simulazione")]
    public ActionResult<Proiezione.Risultato> Simula(string modulo, ParametriSimulazione parametri)
    {
        var scheda = SchedeMercato.Trova(modulo);
        if (scheda == null)
            return NotFound(new { messaggio = "Nessuna analisi di mercato per questo modulo." });

        return Proiezione.Calcola(parametri, scheda.Mercato.Raggiungibile.Persone);
    }

    // Salva le ipotesi del modulo: le vedranno tutti, anche nel confronto.
    [HttpPut("{modulo}/parametri")]
    [Authorize(Policy = Politiche.MercatoAmministratore)]
    public async Task<IActionResult> SalvaParametri(string modulo, ParametriSimulazione parametri)
    {
        if (SchedeMercato.Trova(modulo) == null)
            return NotFound(new { messaggio = "Nessuna analisi di mercato per questo modulo." });

        var riga = await _context.ParametriMercato.FindAsync(modulo);
        if (riga == null)
        {
            riga = new ParametriMercato { Modulo = modulo };
            _context.ParametriMercato.Add(riga);
        }

        parametri.CopiaIn(riga);
        riga.AggiornatoIl = DateTime.UtcNow;
        riga.AggiornatoDaId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Torna alle ipotesi predefinite della scheda.
    [HttpDelete("{modulo}/parametri")]
    [Authorize(Policy = Politiche.MercatoAmministratore)]
    public async Task<IActionResult> RipristinaParametri(string modulo)
    {
        if (SchedeMercato.Trova(modulo) == null)
            return NotFound(new { messaggio = "Nessuna analisi di mercato per questo modulo." });

        await _context.ParametriMercato.Where(p => p.Modulo == modulo).ExecuteDeleteAsync();
        return NoContent();
    }

    private static string NomeModulo(string codice) => Moduli.Trova(codice)?.Nome ?? codice;
}
