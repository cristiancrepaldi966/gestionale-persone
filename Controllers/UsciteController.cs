using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

[ApiController]
[Authorize(Policy = Politiche.Finanze)]
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

        [Required(ErrorMessage = "La categoria è obbligatoria.")]
        public int? CategoriaId { get; set; }

        [StringLength(1000, ErrorMessage = "Le note devono avere al massimo 1000 caratteri.")]
        public string? Note { get; set; }
    }

    public record UscitaDto(
        int Id, DateOnly Data, decimal Importo, string? Note,
        int CategoriaId, string Categoria, string CategoriaIcona, string CategoriaColore, string? CreatoDa);

    public record RisultatoRicerca(IEnumerable<UscitaDto> Uscite, int Numero, decimal Totale, bool Troncato);

    // Filtri comuni a ricerca e riepilogo: testo (note e nome della categoria),
    // categoria, intervallo di date.
    private IQueryable<Uscita> Filtra(string? q, int? categoriaId, DateOnly? dal, DateOnly? al)
    {
        var query = _context.Uscite.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var testo = "%" + EscapeLike(q.Trim()) + "%";
            query = query.Where(u =>
                (u.Note != null && EF.Functions.ILike(u.Note, testo, "\\")) ||
                EF.Functions.ILike(u.Categoria.Nome, testo, "\\"));
        }

        if (categoriaId.HasValue)
            query = query.Where(u => u.CategoriaId == categoriaId);

        if (dal.HasValue)
            query = query.Where(u => u.Data >= dal);

        if (al.HasValue)
            query = query.Where(u => u.Data <= al);

        return query;
    }

    [HttpGet]
    public async Task<RisultatoRicerca> Cerca(
        [FromQuery] string? q, [FromQuery] int? categoriaId,
        [FromQuery] DateOnly? dal, [FromQuery] DateOnly? al)
    {
        var query = Filtra(q, categoriaId, dal, al);

        var numero = await query.CountAsync();
        var totale = await query.SumAsync(u => (decimal?)u.Importo) ?? 0;

        var uscite = await query
            .OrderByDescending(u => u.Data).ThenByDescending(u => u.Id)
            .Take(MaxRisultati)
            .Select(u => new UscitaDto(
                u.Id, u.Data, u.Importo, u.Note,
                u.CategoriaId, u.Categoria.Nome, u.Categoria.Icona, u.Categoria.Colore,
                u.CreatoDa != null ? u.CreatoDa.Email : null))
            .ToListAsync();

        return new RisultatoRicerca(uscite, numero, totale, numero > MaxRisultati);
    }

    public record TotaleGiorno(DateOnly Data, decimal Totale);

    public record TotaleCategoria(int CategoriaId, string Categoria, string Icona, string Colore, decimal Totale, int Numero);

    public record Riepilogo(int Numero, decimal Totale,
        IEnumerable<TotaleGiorno> PerGiorno, IEnumerable<TotaleCategoria> PerCategoria);

    // Dati della dashboard: totali per giorno e per categoria, con gli stessi filtri.
    [HttpGet("riepilogo")]
    public async Task<Riepilogo> GetRiepilogo(
        [FromQuery] int? categoriaId, [FromQuery] DateOnly? dal, [FromQuery] DateOnly? al)
    {
        var query = Filtra(null, categoriaId, dal, al);

        var giorni = await query
            .GroupBy(u => u.Data)
            .Select(g => new { Data = g.Key, Totale = g.Sum(u => u.Importo) })
            .OrderBy(t => t.Data)
            .ToListAsync();

        var categorie = await query
            .GroupBy(u => new { u.CategoriaId, u.Categoria.Nome, u.Categoria.Icona, u.Categoria.Colore })
            .Select(g => new { g.Key.CategoriaId, g.Key.Nome, g.Key.Icona, g.Key.Colore, Totale = g.Sum(u => u.Importo), Numero = g.Count() })
            .OrderByDescending(t => t.Totale)
            .ToListAsync();

        var perGiorno = giorni.Select(g => new TotaleGiorno(g.Data, g.Totale)).ToList();
        var perCategoria = categorie
            .Select(c => new TotaleCategoria(c.CategoriaId, c.Nome, c.Icona, c.Colore, c.Totale, c.Numero))
            .ToList();

        return new Riepilogo(
            perCategoria.Sum(c => c.Numero),
            perCategoria.Sum(c => c.Totale),
            perGiorno, perCategoria);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UscitaDto>> GetUscita(int id)
    {
        var dto = await _context.Uscite.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UscitaDto(
                u.Id, u.Data, u.Importo, u.Note,
                u.CategoriaId, u.Categoria.Nome, u.Categoria.Icona, u.Categoria.Colore,
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
        uscita.Note = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim();
        uscita.CategoriaId = r.CategoriaId!.Value;
    }

    private static string EscapeLike(string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
