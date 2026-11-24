using System.ComponentModel.DataAnnotations;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

// Modulo Fantapolitica: listino dei bonus e malus ed eventi. Tutti gli utenti del modulo li
// vedono; solo il Super admin li gestisce.
// In ogni evento ogni politico prende un voto da 1 a 10 (anche mezzi voti) o resta senza
// voto, più eventuali bonus e malus del listino: i suoi punti sono la somma.
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

    // Voto (null = senza voto) e bonus/malus (id delle voci del listino) di un politico.
    public class PagellaRequest
    {
        public int PoliticoId { get; set; }

        public decimal? Voto { get; set; }

        public List<int>? Bonus { get; set; }
    }

    public class EventoRequest
    {
        [Required(ErrorMessage = "La data è obbligatoria.")]
        public DateOnly? Data { get; set; }

        [Required(ErrorMessage = "Il titolo è obbligatorio.")]
        [StringLength(150, ErrorMessage = "Il titolo deve avere al massimo 150 caratteri.")]
        public string Titolo { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "La descrizione deve avere al massimo 300 caratteri.")]
        public string? Descrizione { get; set; }

        // I politici non presenti sono senza voto e senza bonus.
        public List<PagellaRequest>? Politici { get; set; }
    }

    public record VoceDto(int Id, string Nome, int Punti, int Eventi);

    public record BonusDto(int VocePunteggioId, string Voce, int Punti);

    // Pagella di un politico nell'evento.
    public record PagellaDto(int PoliticoId, string Nome, string Cognome, string Ruolo, decimal? Voto,
        List<BonusDto> Bonus, decimal Punti);

    public record EventoDto(int Id, DateOnly Data, string Titolo, string? Descrizione, List<PagellaDto> Politici);

    public record RegoleVotoDto(decimal VotoMinimo, decimal VotoMassimo, decimal VotoProposto);

    // ---------------------------------------------------------------------------
    // Listino dei bonus e malus
    // ---------------------------------------------------------------------------

    [HttpGet("voci")]
    public async Task<IEnumerable<VoceDto>> GetVoci()
    {
        var voci = await _context.VociPunteggio.AsNoTracking()
            .Select(v => new VoceDto(v.Id, v.Nome, v.Punti,
                _context.BonusEventi.Where(b => b.VocePunteggioId == v.Id).Select(b => b.EventoId).Distinct().Count()))
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

        var eventi = await _context.BonusEventi.Where(b => b.VocePunteggioId == id).Select(b => b.EventoId).Distinct().CountAsync();
        if (eventi > 0)
            return Conflict(new { messaggio = $"La voce è usata in {(eventi == 1 ? "1 evento" : $"{eventi} eventi")}: non si può eliminare." });

        _context.VociPunteggio.Remove(voce);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ---------------------------------------------------------------------------
    // Eventi
    // ---------------------------------------------------------------------------

    [HttpGet("eventi/regole")]
    public RegoleVotoDto GetRegole() => new(Evento.VotoMinimo, Evento.VotoMassimo, Evento.VotoProposto);

    // Dal più recente; in ogni evento i politici dal punteggio più alto.
    [HttpGet("eventi")]
    public async Task<IEnumerable<EventoDto>> GetEventi()
    {
        var eventi = await _context.Eventi.AsNoTracking()
            .Include(e => e.Voti).ThenInclude(v => v.Politico)
            .Include(e => e.Bonus).ThenInclude(b => b.Politico)
            .Include(e => e.Bonus).ThenInclude(b => b.Voce)
            .OrderByDescending(e => e.Data).ThenByDescending(e => e.Id)
            .AsSplitQuery()
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

    // I punti dei bonus e malus vengono ripresi dal listino, con il valore di oggi.
    [HttpPut("eventi/{id}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> ModificaEvento(int id, EventoRequest richiesta)
    {
        var evento = await _context.Eventi.Include(e => e.Voti).Include(e => e.Bonus).SingleOrDefaultAsync(e => e.Id == id);
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
        if (string.IsNullOrWhiteSpace(richiesta.Titolo))
            return BadRequest(new { messaggio = "Il titolo è obbligatorio." });

        var pagelle = richiesta.Politici ?? [];
        if (pagelle.Select(p => p.PoliticoId).Distinct().Count() != pagelle.Count)
            return BadRequest(new { messaggio = "Un politico compare due volte." });
        if (pagelle.Any(p => p.Voto != null && !Evento.VotoValido(p.Voto.Value)))
            return BadRequest(new { messaggio = $"I voti vanno da {Evento.VotoMinimo:0} a {Evento.VotoMassimo:0}, anche con il mezzo voto." });

        var idPolitici = pagelle.Select(p => p.PoliticoId).ToList();
        if (await _context.Politici.CountAsync(p => idPolitici.Contains(p.Id)) != idPolitici.Count)
            return BadRequest(new { messaggio = "Uno dei politici non esiste più. Ricarica la pagina." });

        var idVoci = pagelle.SelectMany(p => p.Bonus ?? []).Distinct().ToList();
        var voci = await _context.VociPunteggio.Where(v => idVoci.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.Punti);
        if (voci.Count != idVoci.Count)
            return BadRequest(new { messaggio = "Una delle voci del listino non esiste più. Ricarica la pagina." });

        if (pagelle.All(p => p.Voto == null && (p.Bonus == null || p.Bonus.Count == 0)))
            return BadRequest(new { messaggio = "Dai almeno un voto o un bonus/malus." });

        evento.Data = richiesta.Data.Value;
        evento.Titolo = richiesta.Titolo.Trim();
        evento.Descrizione = string.IsNullOrWhiteSpace(richiesta.Descrizione) ? null : richiesta.Descrizione.Trim();

        // Voti e bonus si riscrivono da capo.
        evento.Voti.Clear();
        evento.Bonus.Clear();
        foreach (var p in pagelle)
        {
            if (p.Voto != null)
                evento.Voti.Add(new VotoEvento { PoliticoId = p.PoliticoId, Voto = p.Voto.Value });
            foreach (var voce in p.Bonus ?? [])
                evento.Bonus.Add(new BonusEvento { PoliticoId = p.PoliticoId, VocePunteggioId = voce, Punti = voci[voce] });
        }
        return null;
    }

    private static EventoDto Dto(Evento e)
    {
        var politici = e.Voti.Select(v => v.Politico).Concat(e.Bonus.Select(b => b.Politico))
            .DistinctBy(p => p.Id);
        var pagelle = politici.Select(p =>
            {
                var voto = e.Voti.FirstOrDefault(v => v.PoliticoId == p.Id)?.Voto;
                var bonus = e.Bonus.Where(b => b.PoliticoId == p.Id)
                    .Select(b => new BonusDto(b.VocePunteggioId, b.Voce.Nome, b.Punti)).ToList();
                return new PagellaDto(p.Id, p.Nome, p.Cognome, p.Ruolo, voto, bonus,
                    PunteggiFantapolitica.NellEvento(voto, bonus.Select(b => b.Punti)));
            })
            .OrderByDescending(x => x.Punti).ThenBy(x => x.Cognome).ThenBy(x => x.Nome)
            .ToList();
        return new(e.Id, e.Data, e.Titolo, e.Descrizione, pagelle);
    }
}
