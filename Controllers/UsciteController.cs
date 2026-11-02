using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GestionalePersone.Data;
using GestionalePersone.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

[ApiController]
[Authorize]
[Route("api/uscite")]
public class UsciteController : ControllerBase
{
    public const int MaxRisultati = 500;

    private readonly AppDbContext _context;

    public UsciteController(AppDbContext context)
    {
        _context = context;
    }

    public class UscitaRequest
    {
        [Required(ErrorMessage = "La data è obbligatoria.")]
        public DateOnly? Data { get; set; }

        [Required(ErrorMessage = "L'importo è obbligatorio.")]
        [Range(typeof(decimal), "0.01", "9999999999.99", ErrorMessage = "L'importo deve essere maggiore di zero.")]
        public decimal? Importo { get; set; }

        [Required(ErrorMessage = "La descrizione è obbligatoria.")]
        [StringLength(200, ErrorMessage = "La descrizione deve avere al massimo 200 caratteri.")]
        public string Descrizione { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoria è obbligatoria.")]
        public int? CategoriaId { get; set; }

        [StringLength(1000, ErrorMessage = "Le note devono avere al massimo 1000 caratteri.")]
        public string? Note { get; set; }
    }

    public record UscitaDto(
        int Id, DateOnly Data, decimal Importo, string Descrizione, string? Note,
        int CategoriaId, string Categoria, string? CreatoDa);

    public record RisultatoRicerca(IEnumerable<UscitaDto> Uscite, int Numero, decimal Totale, bool Troncato);

    // Ricerca: testo (descrizione e note), categoria, intervallo di date.
    [HttpGet]
    public async Task<RisultatoRicerca> Cerca(
        [FromQuery] string? q, [FromQuery] int? categoriaId,
        [FromQuery] DateOnly? dal, [FromQuery] DateOnly? al)
    {
        var query = _context.Uscite.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var testo = "%" + EscapeLike(q.Trim()) + "%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Descrizione, testo, "\\") ||
                (u.Note != null && EF.Functions.ILike(u.Note, testo, "\\")));
        }

        if (categoriaId.HasValue)
            query = query.Where(u => u.CategoriaId == categoriaId);

        if (dal.HasValue)
            query = query.Where(u => u.Data >= dal);

        if (al.HasValue)
            query = query.Where(u => u.Data <= al);

        var numero = await query.CountAsync();
        var totale = await query.SumAsync(u => (decimal?)u.Importo) ?? 0;

        var uscite = await query
            .OrderByDescending(u => u.Data).ThenByDescending(u => u.Id)
            .Take(MaxRisultati)
            .Select(u => new UscitaDto(
                u.Id, u.Data, u.Importo, u.Descrizione, u.Note,
                u.CategoriaId, u.Categoria.Nome,
                u.CreatoDa != null ? u.CreatoDa.Email : null))
            .ToListAsync();

        return new RisultatoRicerca(uscite, numero, totale, numero > MaxRisultati);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UscitaDto>> GetUscita(int id)
    {
        var dto = await _context.Uscite.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UscitaDto(
                u.Id, u.Data, u.Importo, u.Descrizione, u.Note,
                u.CategoriaId, u.Categoria.Nome,
                u.CreatoDa != null ? u.CreatoDa.Email : null))
            .SingleOrDefaultAsync();

        return dto == null ? NotFound() : dto;
    }

    [HttpPost]
    public async Task<ActionResult<UscitaDto>> CreaUscita(UscitaRequest richiesta)
    {
        if (!await _context.Categorie.AnyAsync(c => c.Id == richiesta.CategoriaId))
            return BadRequest(new { messaggio = "La categoria scelta non esiste." });

        var uscita = new Uscita
        {
            CreatoDaId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        };
        Applica(uscita, richiesta);

        _context.Uscite.Add(uscita);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUscita), new { id = uscita.Id }, (await GetUscita(uscita.Id)).Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ModificaUscita(int id, UscitaRequest richiesta)
    {
        var uscita = await _context.Uscite.FindAsync(id);
        if (uscita == null)
            return NotFound();

        if (!await _context.Categorie.AnyAsync(c => c.Id == richiesta.CategoriaId))
            return BadRequest(new { messaggio = "La categoria scelta non esiste." });

        Applica(uscita, richiesta);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminaUscita(int id)
    {
        var uscita = await _context.Uscite.FindAsync(id);
        if (uscita == null)
            return NotFound();

        _context.Uscite.Remove(uscita);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static void Applica(Uscita uscita, UscitaRequest r)
    {
        uscita.Data = r.Data!.Value;
        uscita.Importo = Math.Round(r.Importo!.Value, 2);
        uscita.Descrizione = r.Descrizione.Trim();
        uscita.Note = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim();
        uscita.CategoriaId = r.CategoriaId!.Value;
    }

    private static string EscapeLike(string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
