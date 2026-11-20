using System.ComponentModel.DataAnnotations;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

// Modulo Fantapolitica: listino dei bonus e malus ed eventi che danno i punti ai politici.
// Tutti gli utenti del modulo li vedono; solo il Super admin li gestisce.
// Ogni evento usa una voce del listino e dà i suoi punti a ogni politico coinvolto.
[ApiController]
[Authorize(Policy = Politiche.Fantapolitica)]
[Route("api/fantapolitica")]
public class FantapoliticaEventiController : ControllerBase
{
    private readonly AppDbContext _context;

    public FantapoliticaEventiController(AppDbContext context)
    {
        _context = context;
    }

    public class VoceRequest
    {
        [Required(ErrorMessage = "Il nome della voce è obbligatorio.")]
        [StringLength(100, ErrorMessage = "Il nome deve avere al massimo 100 caratteri.")]
        public string Nome { get; set; } = string.Empty;

        [Range(-1000, 1000, ErrorMessage = "I punti devono essere tra -1000 e 1000.")]
        public int Punti { get; set; }
    }

    public class EventoRequest
    {
        [Required(ErrorMessage = "La data è obbligatoria.")]
        public DateOnly? Data { get; set; }

        [Required(ErrorMessage = "Scegli una voce del listino.")]
        public int? VocePunteggioId { get; set; }

        [StringLength(300, ErrorMessage = "La descrizione deve avere al massimo 300 caratteri.")]
        public string? Descrizione { get; set; }

        public List<int>? Politici { get; set; }
    }

    public record VoceDto(int Id, string Nome, int Punti, int Eventi);

    public record PoliticoEventoDto(int Id, string Nome, string Cognome, string Ruolo);

    public record EventoDto(int Id, DateOnly Data, int VocePunteggioId, string Voce, int Punti, string? Descrizione,
        List<PoliticoEventoDto> Politici);

    // ---------------------------------------------------------------------------
    // Listino dei bonus e malus
    // ---------------------------------------------------------------------------

    [HttpGet("voci")]
    public async Task<IEnumerable<VoceDto>> GetVoci()
    {
        var voci = await _context.VociPunteggio.AsNoTracking()
            .Select(v => new VoceDto(v.Id, v.Nome, v.Punti, _context.Eventi.Count(e => e.VocePunteggioId == v.Id)))
            .ToListAsync();
        // Prima i bonus (dal più alto), poi i malus.
        return voci.OrderByDescending(v => v.Punti).ThenBy(v => v.Nome);
    }

    [HttpPost("voci")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<ActionResult<VoceDto>> CreaVoce(VoceRequest richiesta)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Nome))
            return BadRequest(new { messaggio = "Il nome della voce è obbligatorio." });

        var voce = new VocePunteggio { Nome = richiesta.Nome.Trim(), Punti = richiesta.Punti };
        _context.VociPunteggio.Add(voce);
        await _context.SaveChangesAsync();
        return Created($"/api/fantapolitica/voci/{voce.Id}", new VoceDto(voce.Id, voce.Nome, voce.Punti, 0));
    }

    // Cambiare i punti di una voce non cambia gli eventi già registrati.
    [HttpPut("voci/{id}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> ModificaVoce(int id, VoceRequest richiesta)
    {
        var voce = await _context.VociPunteggio.FindAsync(id);
        if (voce == null)
            return NotFound(new { messaggio = "Voce non trovata." });
        if (string.IsNullOrWhiteSpace(richiesta.Nome))
            return BadRequest(new { messaggio = "Il nome della voce è obbligatorio." });

        voce.Nome = richiesta.Nome.Trim();
        voce.Punti = richiesta.Punti;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("voci/{id}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> EliminaVoce(int id)
    {
        var voce = await _context.VociPunteggio.FindAsync(id);
        if (voce == null)
            return NotFound(new { messaggio = "Voce non trovata." });

        var eventi = await _context.Eventi.CountAsync(e => e.VocePunteggioId == id);
        if (eventi > 0)
            return Conflict(new { messaggio = $"La voce è usata da {(eventi == 1 ? "1 evento" : $"{eventi} eventi")}: non si può eliminare." });

        _context.VociPunteggio.Remove(voce);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ---------------------------------------------------------------------------
    // Eventi
    // ---------------------------------------------------------------------------

    // Dal più recente.
    [HttpGet("eventi")]
    public async Task<IEnumerable<EventoDto>> GetEventi()
    {
        var eventi = await _context.Eventi.AsNoTracking()
            .Include(e => e.Voce)
            .Include(e => e.Politici).ThenInclude(ep => ep.Politico)
            .OrderByDescending(e => e.Data).ThenByDescending(e => e.Id)
            .ToListAsync();
        return eventi.Select(Dto);
    }

    [HttpPost("eventi")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> CreaEvento(EventoRequest richiesta)
    {
        var evento = new Evento();
        var errore = await Applica(evento, richiesta);
        if (errore != null) return errore;

        _context.Eventi.Add(evento);
        await _context.SaveChangesAsync();
        return Created($"/api/fantapolitica/eventi/{evento.Id}", new { evento.Id });
    }

    // I punti vengono ripresi dalla voce scelta, con il valore di oggi.
    [HttpPut("eventi/{id}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> ModificaEvento(int id, EventoRequest richiesta)
    {
        var evento = await _context.Eventi.Include(e => e.Politici).SingleOrDefaultAsync(e => e.Id == id);
        if (evento == null)
            return NotFound(new { messaggio = "Evento non trovato." });

        var errore = await Applica(evento, richiesta);
        if (errore != null) return errore;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("eventi/{id}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> EliminaEvento(int id)
    {
        var evento = await _context.Eventi.FindAsync(id);
        if (evento == null)
            return NotFound(new { messaggio = "Evento non trovato." });

        _context.Eventi.Remove(evento);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ActionResult?> Applica(Evento evento, EventoRequest richiesta)
    {
        if (richiesta.Data == null)
            return BadRequest(new { messaggio = "La data è obbligatoria." });
        if (richiesta.Data > PunteggiFantapolitica.Oggi())
            return BadRequest(new { messaggio = "La data non può essere nel futuro." });

        var voce = richiesta.VocePunteggioId == null ? null : await _context.VociPunteggio.FindAsync(richiesta.VocePunteggioId);
        if (voce == null)
            return BadRequest(new { messaggio = "Scegli una voce del listino." });

        var politici = (richiesta.Politici ?? []).ToHashSet();
        if (politici.Count == 0)
            return BadRequest(new { messaggio = "Scegli almeno un politico." });
        if (await _context.Politici.CountAsync(p => politici.Contains(p.Id)) != politici.Count)
            return BadRequest(new { messaggio = "Uno dei politici scelti non esiste più. Ricarica la pagina." });

        evento.Data = richiesta.Data.Value;
        evento.VocePunteggioId = voce.Id;
        evento.Punti = voce.Punti;
        evento.Descrizione = string.IsNullOrWhiteSpace(richiesta.Descrizione) ? null : richiesta.Descrizione.Trim();
        evento.Politici.RemoveAll(ep => !politici.Contains(ep.PoliticoId));
        foreach (var politicoId in politici.Where(p => evento.Politici.All(ep => ep.PoliticoId != p)))
            evento.Politici.Add(new EventoPolitico { PoliticoId = politicoId });
        return null;
    }

    private static EventoDto Dto(Evento e) => new(
        e.Id, e.Data, e.VocePunteggioId, e.Voce.Nome, e.Punti, e.Descrizione,
        e.Politici.Select(ep => ep.Politico)
            .OrderBy(p => p.Cognome).ThenBy(p => p.Nome)
            .Select(p => new PoliticoEventoDto(p.Id, p.Nome, p.Cognome, p.Ruolo))
            .ToList());
}
