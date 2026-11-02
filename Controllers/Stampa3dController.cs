using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

// Modulo Stampa 3D: catalogo degli oggetti, generazione del file STL dalle misure
// e progetti salvati. Ognuno gestisce i propri progetti; l'Amministratore li vede tutti.
[ApiController]
[Authorize(Policy = Politiche.Stampa3d)]
[Route("api/stampa3d")]
public class Stampa3dController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly GeneratoreStl _generatore;

    public Stampa3dController(AppDbContext context, GeneratoreStl generatore)
    {
        _context = context;
        _generatore = generatore;
    }

    public class StlRequest
    {
        [Required(ErrorMessage = "Scegli un oggetto del catalogo.")]
        public string Oggetto { get; set; } = string.Empty;

        public Dictionary<string, JsonElement>? Parametri { get; set; }
    }

    public class ProgettoRequest : StlRequest
    {
        [Required(ErrorMessage = "Il nome del progetto è obbligatorio.")]
        [StringLength(100, ErrorMessage = "Il nome deve avere al massimo 100 caratteri.")]
        public string Nome { get; set; } = string.Empty;
    }

    public record ProgettoDto(int Id, string Nome, string Oggetto, string NomeOggetto,
        JsonElement Parametri, string? CreatoDa, bool Mio, DateTime CreatoIl, DateTime AggiornatoIl);

    private int IdCorrente => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool Amministratore => User.E(Moduli.Stampa3d, Livelli.Amministratore);

    // ---------------------------------------------------------------------------
    // Catalogo e generazione
    // ---------------------------------------------------------------------------

    [HttpGet("catalogo")]
    public IEnumerable<CatalogoStampa3d.Oggetto> GetCatalogo() => CatalogoStampa3d.Tutti;

    // Restituisce il file STL dell'oggetto con le misure richieste.
    [HttpPost("stl")]
    public async Task<IActionResult> GeneraStl(StlRequest richiesta, CancellationToken annulla)
    {
        var oggetto = CatalogoStampa3d.Trova(richiesta.Oggetto);
        if (oggetto == null)
            return NotFound(new { messaggio = "Oggetto non presente nel catalogo." });

        var (valori, _, errori) = CatalogoStampa3d.Valida(oggetto, richiesta.Parametri);
        if (errori.Count > 0)
            return BadRequest(new { messaggio = string.Join(" ", errori) });

        try
        {
            var stl = await _generatore.Genera(oggetto, valori, annulla);
            return File(stl, "model/stl", oggetto.Codice + ".stl");
        }
        catch (GeneratoreStl.OpenScadNonDisponibile e)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { messaggio = e.Message });
        }
        catch (GeneratoreStl.GenerazioneFallita e)
        {
            return UnprocessableEntity(new { messaggio = e.Message });
        }
    }

    // ---------------------------------------------------------------------------
    // Progetti
    // ---------------------------------------------------------------------------

    // I miei progetti; l'Amministratore con ?tutti=true vede anche quelli degli altri.
    [HttpGet("progetti")]
    public async Task<IEnumerable<ProgettoDto>> GetProgetti(bool tutti = false)
    {
        var id = IdCorrente;
        var query = _context.Progetti3d.AsNoTracking();
        if (!(tutti && Amministratore))
            query = query.Where(p => p.CreatoDaId == id);

        var righe = await query
            .OrderByDescending(p => p.AggiornatoIl)
            .Select(p => new
            {
                p.Id, p.Nome, p.Oggetto, p.Parametri, p.CreatoDaId, p.CreatoIl, p.AggiornatoIl,
                Autore = p.CreatoDa == null ? null : (p.CreatoDa.Nome + " " + p.CreatoDa.Cognome).Trim(),
                Email = p.CreatoDa == null ? null : p.CreatoDa.Email,
            })
            .ToListAsync();

        return righe.Select(r => Dto(r.Id, r.Nome, r.Oggetto, r.Parametri, r.CreatoDaId,
            string.IsNullOrEmpty(r.Autore) ? r.Email : r.Autore, r.CreatoIl, r.AggiornatoIl));
    }

    [HttpGet("progetti/{id}")]
    public async Task<ActionResult<ProgettoDto>> GetProgetto(int id)
    {
        var p = await _context.Progetti3d.AsNoTracking().Include(x => x.CreatoDa).SingleOrDefaultAsync(x => x.Id == id);
        if (p == null || !PuoGestire(p))
            return NotFound(new { messaggio = "Progetto non trovato." });

        var autore = p.CreatoDa == null ? null : $"{p.CreatoDa.Nome} {p.CreatoDa.Cognome}".Trim();
        return Dto(p.Id, p.Nome, p.Oggetto, p.Parametri, p.CreatoDaId,
            string.IsNullOrEmpty(autore) ? p.CreatoDa?.Email : autore, p.CreatoIl, p.AggiornatoIl);
    }

    [HttpPost("progetti")]
    public async Task<ActionResult<ProgettoDto>> CreaProgetto(ProgettoRequest richiesta)
    {
        var (oggetto, parametri, errore) = Controlla(richiesta);
        if (errore != null) return errore;

        var progetto = new Progetto3d
        {
            Nome = richiesta.Nome.Trim(),
            Oggetto = oggetto!.Codice,
            Parametri = parametri!,
            CreatoDaId = IdCorrente,
        };
        _context.Progetti3d.Add(progetto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProgetto), new { id = progetto.Id },
            Dto(progetto.Id, progetto.Nome, progetto.Oggetto, progetto.Parametri, progetto.CreatoDaId, null,
                progetto.CreatoIl, progetto.AggiornatoIl));
    }

    [HttpPut("progetti/{id}")]
    public async Task<IActionResult> ModificaProgetto(int id, ProgettoRequest richiesta)
    {
        var progetto = await _context.Progetti3d.FindAsync(id);
        if (progetto == null || !PuoGestire(progetto))
            return NotFound(new { messaggio = "Progetto non trovato." });

        var (oggetto, parametri, errore) = Controlla(richiesta);
        if (errore != null) return errore;

        progetto.Nome = richiesta.Nome.Trim();
        progetto.Oggetto = oggetto!.Codice;
        progetto.Parametri = parametri!;
        progetto.AggiornatoIl = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("progetti/{id}")]
    public async Task<IActionResult> EliminaProgetto(int id)
    {
        var progetto = await _context.Progetti3d.FindAsync(id);
        if (progetto == null || !PuoGestire(progetto))
            return NotFound(new { messaggio = "Progetto non trovato." });

        _context.Progetti3d.Remove(progetto);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Il proprietario gestisce i suoi progetti; l'Amministratore tutti.
    private bool PuoGestire(Progetto3d p) => p.CreatoDaId == IdCorrente || Amministratore;

    private (CatalogoStampa3d.Oggetto?, string?, ActionResult?) Controlla(ProgettoRequest richiesta)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Nome))
            return (null, null, BadRequest(new { messaggio = "Il nome del progetto è obbligatorio." }));

        var oggetto = CatalogoStampa3d.Trova(richiesta.Oggetto);
        if (oggetto == null)
            return (null, null, BadRequest(new { messaggio = "Oggetto non presente nel catalogo." }));

        var (_, puliti, errori) = CatalogoStampa3d.Valida(oggetto, richiesta.Parametri);
        if (errori.Count > 0)
            return (null, null, BadRequest(new { messaggio = string.Join(" ", errori) }));

        return (oggetto, JsonSerializer.Serialize(puliti), null);
    }

    private ProgettoDto Dto(int id, string nome, string oggetto, string parametri, int? creatoDaId, string? autore,
        DateTime creatoIl, DateTime aggiornatoIl) =>
        new(id, nome, oggetto, CatalogoStampa3d.Trova(oggetto)?.Nome ?? oggetto,
            JsonDocument.Parse(parametri).RootElement.Clone(), autore, creatoDaId == IdCorrente, creatoIl, aggiornatoIl);
}
