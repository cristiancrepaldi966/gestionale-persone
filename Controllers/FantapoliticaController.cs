using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

// Modulo Fantapolitica: leghe, politici e squadre.
// Leghe: tutti gli utenti del modulo vedono tutte le leghe e ne
// possono creare; chi crea una lega ne fa sempre parte. I membri si scelgono tra gli
// utenti attivi abilitati al modulo (Super admin compresi). Modifica ed eliminazione:
// solo chi ha creato la lega e il Super admin (non l'Amministratore del modulo).
// Politici: tutti li vedono, solo il Super admin li gestisce.
// Squadre: ogni membro ha una squadra per lega con Squadra.PoliticiPerSquadra politici
// (lo stesso politico può stare in più squadre); tutti i membri vedono le squadre della lega.
[ApiController]
[Authorize(Policy = Politiche.Fantapolitica)]
[Route("api/fantapolitica")]
public class FantapoliticaController : ControllerBase
{
    private readonly AppDbContext _context;

    public FantapoliticaController(AppDbContext context)
    {
        _context = context;
    }

    public class LegaRequest
    {
        [Required(ErrorMessage = "Il nome della lega è obbligatorio.")]
        [StringLength(100, ErrorMessage = "Il nome deve avere al massimo 100 caratteri.")]
        public string Nome { get; set; } = string.Empty;

        // Utenti scelti come membri (chi ha creato la lega viene aggiunto comunque).
        public List<int>? Membri { get; set; }
    }

    public class PoliticoRequest
    {
        [Required(ErrorMessage = "Il nome è obbligatorio.")]
        [StringLength(100, ErrorMessage = "Il nome deve avere al massimo 100 caratteri.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il cognome è obbligatorio.")]
        [StringLength(100, ErrorMessage = "Il cognome deve avere al massimo 100 caratteri.")]
        public string Cognome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il ruolo è obbligatorio.")]
        public string Ruolo { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Il partito deve avere al massimo 100 caratteri.")]
        public string? Partito { get; set; }
    }

    public class SquadraRequest
    {
        [Required(ErrorMessage = "Il nome della squadra è obbligatorio.")]
        [StringLength(50, ErrorMessage = "Il nome della squadra deve avere al massimo 50 caratteri.")]
        public string Nome { get; set; } = string.Empty;

        public List<int>? Politici { get; set; }
    }

    public record PersonaDto(int Id, string Nome);

    public record PoliticoDto(int Id, string Nome, string Cognome, string Ruolo, string? Partito, int Squadre);

    public record SquadraDto(int Id, string Nome, List<PoliticoDto> Politici, DateTime AggiornatoIl);

    public record MembroDto(int Id, string Nome, SquadraDto? Squadra);

    public record LegaDto(int Id, string Nome, PersonaDto? CreatoDa, bool Gestibile, DateTime CreatoIl,
        List<MembroDto> Membri);

    // Le mie leghe con la mia squadra (pagina "La mia squadra").
    public record MiaLegaDto(int Id, string Nome, SquadraDto? Squadra);

    public record MieSquadreDto(int PoliticiPerSquadra, List<MiaLegaDto> Leghe);

    private int IdCorrente => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Utenti che possono far parte di una lega: attivi e abilitati al modulo.
    private IQueryable<Utente> UtentiAbilitati => _context.Utenti.AsNoTracking()
        .Where(u => u.Attivo && (u.SuperAdmin || u.Autorizzazioni.Any(a => a.Modulo == Moduli.Fantapolitica)));

    // Nome da mostrare: "Nome Cognome", oppure l'email se mancano.
    private static string NomeVisibile(Utente u)
    {
        var nome = $"{u.Nome} {u.Cognome}".Trim();
        return string.IsNullOrEmpty(nome) ? u.Email : nome;
    }

    private bool PuoGestire(Lega l) => l.CreatoDaId == IdCorrente || User.SuperAdmin();

    private static readonly StringComparer Alfabetico =
        StringComparer.Create(new System.Globalization.CultureInfo("it-IT"), true);

    // Ordine dei politici: per ruolo (Sindaco, Vicesindaco, ...), poi cognome e nome.
    private static IEnumerable<Politico> Ordina(IEnumerable<Politico> politici) => politici
        .OrderBy(p => Array.IndexOf(RuoliPolitico.Tutti, p.Ruolo))
        .ThenBy(p => p.Cognome, Alfabetico)
        .ThenBy(p => p.Nome, Alfabetico);

    private static PoliticoDto Dto(Politico p, int squadre = 0) =>
        new(p.Id, p.Nome, p.Cognome, p.Ruolo, p.Partito, squadre);

    private static SquadraDto Dto(Squadra s) =>
        new(s.Id, s.Nome, Ordina(s.Politici.Select(ps => ps.Politico)).Select(p => Dto(p)).ToList(), s.AggiornatoIl);

    // ---------------------------------------------------------------------------
    // Utenti da scegliere come membri
    // ---------------------------------------------------------------------------

    [HttpGet("utenti")]
    public async Task<IEnumerable<PersonaDto>> GetUtenti()
    {
        var utenti = await UtentiAbilitati.ToListAsync();
        return utenti.Select(u => new PersonaDto(u.Id, NomeVisibile(u)))
            .OrderBy(p => p.Nome, Alfabetico);
    }

    // ---------------------------------------------------------------------------
    // Leghe
    // ---------------------------------------------------------------------------

    [HttpGet("leghe")]
    public async Task<IEnumerable<LegaDto>> GetLeghe()
    {
        var leghe = await _context.Leghe.AsNoTracking()
            .Include(l => l.CreatoDa)
            .Include(l => l.Membri).ThenInclude(m => m.Utente)
            .OrderBy(l => l.Nome)
            .ToListAsync();
        var squadre = await SquadreDi(leghe.Select(l => l.Id).ToList());
        return leghe.Select(l => Dto(l, squadre));
    }

    [HttpGet("leghe/{id}")]
    public async Task<ActionResult<LegaDto>> GetLega(int id)
    {
        var lega = await _context.Leghe.AsNoTracking()
            .Include(l => l.CreatoDa)
            .Include(l => l.Membri).ThenInclude(m => m.Utente)
            .SingleOrDefaultAsync(l => l.Id == id);
        if (lega == null)
            return NotFound(new { messaggio = "Lega non trovata." });
        return Dto(lega, await SquadreDi([lega.Id]));
    }

    // Squadre delle leghe indicate, per (lega, utente).
    private async Task<Dictionary<(int, int), Squadra>> SquadreDi(List<int> leghe) =>
        (await _context.Squadre.AsNoTracking()
            .Include(s => s.Politici).ThenInclude(ps => ps.Politico)
            .Where(s => leghe.Contains(s.LegaId))
            .ToListAsync())
        .ToDictionary(s => (s.LegaId, s.UtenteId));

    [HttpPost("leghe")]
    public async Task<ActionResult<LegaDto>> CreaLega(LegaRequest richiesta)
    {
        var (membri, errore) = await Controlla(richiesta, IdCorrente);
        if (errore != null) return errore;

        var lega = new Lega
        {
            Nome = richiesta.Nome.Trim(),
            CreatoDaId = IdCorrente,
            Membri = membri!.Select(id => new MembroLega { UtenteId = id }).ToList(),
        };
        _context.Leghe.Add(lega);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLega), new { id = lega.Id }, (await GetLega(lega.Id)).Value);
    }

    [HttpPut("leghe/{id}")]
    public async Task<IActionResult> ModificaLega(int id, LegaRequest richiesta)
    {
        var lega = await _context.Leghe.Include(l => l.Membri).SingleOrDefaultAsync(l => l.Id == id);
        if (lega == null)
            return NotFound(new { messaggio = "Lega non trovata." });
        if (!PuoGestire(lega))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { messaggio = "Solo chi ha creato la lega può modificarla." });

        // Chi l'ha creata resta sempre nella lega (anche quando la modifica il Super admin).
        // Chi è già nella lega ci resta anche se nel frattempo non è più abilitato al modulo.
        var (membri, errore) = await Controlla(richiesta, lega.CreatoDaId,
            lega.Membri.Select(m => m.UtenteId).ToHashSet());
        if (errore != null) return errore;

        lega.Nome = richiesta.Nome.Trim();
        lega.Membri.RemoveAll(m => !membri!.Contains(m.UtenteId));
        foreach (var utenteId in membri!.Where(u => lega.Membri.All(m => m.UtenteId != u)))
            lega.Membri.Add(new MembroLega { UtenteId = utenteId });

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("leghe/{id}")]
    public async Task<IActionResult> EliminaLega(int id)
    {
        var lega = await _context.Leghe.FindAsync(id);
        if (lega == null)
            return NotFound(new { messaggio = "Lega non trovata." });
        if (!PuoGestire(lega))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { messaggio = "Solo chi ha creato la lega può eliminarla." });

        _context.Leghe.Remove(lega);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Controlla il nome e i membri scelti; restituisce gli id dei membri con il creatore incluso.
    // giaMembri: chi è già nella lega può restarci anche se non è più tra gli abilitati.
    private async Task<(HashSet<int>?, ActionResult?)> Controlla(LegaRequest richiesta, int? creatoreId,
        HashSet<int>? giaMembri = null)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Nome))
            return (null, BadRequest(new { messaggio = "Il nome della lega è obbligatorio." }));

        var scelti = (richiesta.Membri ?? []).ToHashSet();
        var nuovi = scelti.Where(id => giaMembri == null || !giaMembri.Contains(id)).ToList();
        if (nuovi.Count > 0)
        {
            var abilitati = await UtentiAbilitati.Where(u => nuovi.Contains(u.Id)).CountAsync();
            if (abilitati != nuovi.Count)
                return (null, BadRequest(new { messaggio = "Si possono scegliere solo utenti abilitati alla Fantapolitica." }));
        }

        if (creatoreId != null) scelti.Add(creatoreId.Value);
        return (scelti, null);
    }

    private LegaDto Dto(Lega l, Dictionary<(int, int), Squadra> squadre) => new(
        l.Id,
        l.Nome,
        l.CreatoDa == null ? null : new PersonaDto(l.CreatoDa.Id, NomeVisibile(l.CreatoDa)),
        PuoGestire(l),
        l.CreatoIl,
        l.Membri
            .Select(m => new MembroDto(m.UtenteId, NomeVisibile(m.Utente),
                squadre.TryGetValue((l.Id, m.UtenteId), out var s) ? Dto(s) : null))
            .OrderBy(p => p.Id != l.CreatoDaId)
            .ThenBy(p => p.Nome, Alfabetico)
            .ToList());

    // ---------------------------------------------------------------------------
    // Politici: tutti li vedono, solo il Super admin li gestisce
    // ---------------------------------------------------------------------------

    [HttpGet("politici")]
    public async Task<IEnumerable<PoliticoDto>> GetPolitici()
    {
        var politici = await _context.Politici.AsNoTracking().ToListAsync();
        var squadre = await _context.PoliticiSquadre.AsNoTracking()
            .GroupBy(ps => ps.PoliticoId)
            .Select(g => new { g.Key, Numero = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Numero);
        return Ordina(politici).Select(p => Dto(p, squadre.GetValueOrDefault(p.Id)));
    }

    [HttpPost("politici")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<ActionResult<PoliticoDto>> CreaPolitico(PoliticoRequest richiesta)
    {
        var politico = new Politico();
        var errore = Applica(politico, richiesta);
        if (errore != null) return errore;

        _context.Politici.Add(politico);
        await _context.SaveChangesAsync();
        return Created($"/api/fantapolitica/politici/{politico.Id}", Dto(politico));
    }

    [HttpPut("politici/{id}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> ModificaPolitico(int id, PoliticoRequest richiesta)
    {
        var politico = await _context.Politici.FindAsync(id);
        if (politico == null)
            return NotFound(new { messaggio = "Politico non trovato." });

        var errore = Applica(politico, richiesta);
        if (errore != null) return errore;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Il politico esce anche dalle squadre che lo avevano scelto.
    [HttpDelete("politici/{id}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> EliminaPolitico(int id)
    {
        var politico = await _context.Politici.FindAsync(id);
        if (politico == null)
            return NotFound(new { messaggio = "Politico non trovato." });

        _context.Politici.Remove(politico);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private ActionResult? Applica(Politico politico, PoliticoRequest richiesta)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Nome) || string.IsNullOrWhiteSpace(richiesta.Cognome))
            return BadRequest(new { messaggio = "Nome e cognome sono obbligatori." });
        if (!RuoliPolitico.Tutti.Contains(richiesta.Ruolo))
            return BadRequest(new { messaggio = "Ruolo non valido." });

        politico.Nome = richiesta.Nome.Trim();
        politico.Cognome = richiesta.Cognome.Trim();
        politico.Ruolo = richiesta.Ruolo;
        politico.Partito = string.IsNullOrWhiteSpace(richiesta.Partito) ? null : richiesta.Partito.Trim();
        return null;
    }

    // ---------------------------------------------------------------------------
    // Squadre: ognuno gestisce la propria, una per ogni lega di cui fa parte
    // ---------------------------------------------------------------------------

    // Le leghe di cui faccio parte, ciascuna con la mia squadra (se l'ho già creata).
    [HttpGet("squadre/mie")]
    public async Task<MieSquadreDto> GetMieSquadre()
    {
        var id = IdCorrente;
        var leghe = await _context.MembriLeghe.AsNoTracking()
            .Where(m => m.UtenteId == id)
            .Select(m => new { m.Lega.Id, m.Lega.Nome })
            .ToListAsync();
        var squadre = await _context.Squadre.AsNoTracking()
            .Include(s => s.Politici).ThenInclude(ps => ps.Politico)
            .Where(s => s.UtenteId == id)
            .ToDictionaryAsync(s => s.LegaId);

        return new MieSquadreDto(Squadra.PoliticiPerSquadra, leghe
            .OrderBy(l => l.Nome, Alfabetico)
            .Select(l => new MiaLegaDto(l.Id, l.Nome, squadre.TryGetValue(l.Id, out var s) ? Dto(s) : null))
            .ToList());
    }

    // Crea o aggiorna la mia squadra nella lega.
    [HttpPut("leghe/{legaId}/squadra")]
    public async Task<IActionResult> SalvaSquadra(int legaId, SquadraRequest richiesta)
    {
        var id = IdCorrente;
        if (!await _context.MembriLeghe.AnyAsync(m => m.LegaId == legaId && m.UtenteId == id))
            return NotFound(new { messaggio = "Non fai parte di questa lega." });

        if (string.IsNullOrWhiteSpace(richiesta.Nome))
            return BadRequest(new { messaggio = "Il nome della squadra è obbligatorio." });

        var scelti = (richiesta.Politici ?? []).ToHashSet();
        if (scelti.Count != Squadra.PoliticiPerSquadra)
            return BadRequest(new { messaggio = $"La squadra deve avere {Squadra.PoliticiPerSquadra} politici." });
        if (await _context.Politici.CountAsync(p => scelti.Contains(p.Id)) != scelti.Count)
            return BadRequest(new { messaggio = "Uno dei politici scelti non esiste più. Ricarica la pagina." });

        var squadra = await _context.Squadre.Include(s => s.Politici)
            .SingleOrDefaultAsync(s => s.LegaId == legaId && s.UtenteId == id);
        if (squadra == null)
        {
            squadra = new Squadra { LegaId = legaId, UtenteId = id };
            _context.Squadre.Add(squadra);
        }

        squadra.Nome = richiesta.Nome.Trim();
        squadra.AggiornatoIl = DateTime.UtcNow;
        squadra.Politici.RemoveAll(ps => !scelti.Contains(ps.PoliticoId));
        foreach (var politicoId in scelti.Where(p => squadra.Politici.All(ps => ps.PoliticoId != p)))
            squadra.Politici.Add(new PoliticoSquadra { PoliticoId = politicoId });

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("leghe/{legaId}/squadra")]
    public async Task<IActionResult> EliminaSquadra(int legaId)
    {
        var id = IdCorrente;
        var squadra = await _context.Squadre.SingleOrDefaultAsync(s => s.LegaId == legaId && s.UtenteId == id);
        if (squadra == null)
            return NotFound(new { messaggio = "Squadra non trovata." });

        _context.Squadre.Remove(squadra);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
