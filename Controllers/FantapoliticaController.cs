using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

// Modulo Fantapolitica: leghe. Tutti gli utenti del modulo vedono tutte le leghe e ne
// possono creare; chi crea una lega ne fa sempre parte. I membri si scelgono tra gli
// utenti attivi abilitati al modulo (Super admin compresi). Modifica ed eliminazione:
// solo chi ha creato la lega e il Super admin (non l'Amministratore del modulo).
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

    public record PersonaDto(int Id, string Nome);

    public record LegaDto(int Id, string Nome, PersonaDto? CreatoDa, bool Gestibile, DateTime CreatoIl,
        List<PersonaDto> Membri);

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

    // ---------------------------------------------------------------------------
    // Utenti da scegliere come membri
    // ---------------------------------------------------------------------------

    [HttpGet("utenti")]
    public async Task<IEnumerable<PersonaDto>> GetUtenti()
    {
        var utenti = await UtentiAbilitati.ToListAsync();
        return utenti.Select(u => new PersonaDto(u.Id, NomeVisibile(u)))
            .OrderBy(p => p.Nome, StringComparer.Create(new System.Globalization.CultureInfo("it-IT"), true));
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
        return leghe.Select(Dto);
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
        return Dto(lega);
    }

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

    private LegaDto Dto(Lega l) => new(
        l.Id,
        l.Nome,
        l.CreatoDa == null ? null : new PersonaDto(l.CreatoDa.Id, NomeVisibile(l.CreatoDa)),
        PuoGestire(l),
        l.CreatoIl,
        l.Membri
            .Select(m => new PersonaDto(m.UtenteId, NomeVisibile(m.Utente)))
            .OrderBy(p => p.Id != l.CreatoDaId)
            .ThenBy(p => p.Nome, StringComparer.Create(new System.Globalization.CultureInfo("it-IT"), true))
            .ToList());
}
