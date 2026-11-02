using System.ComponentModel.DataAnnotations;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

[ApiController]
[Authorize(Policy = Politiche.LetturaCategorie)]
[Route("api/categorie")]
public class CategorieController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategorieController(AppDbContext context)
    {
        _context = context;
    }

    public record CategoriaDto(int Id, string Nome, string Icona, string Colore, int NumeroUscite);

    // Dal client si accetta solo il nome (nessun altro campo può essere impostato).
    public class CategoriaRequest
    {
        [Required(ErrorMessage = "Il nome della categoria è obbligatorio.")]
        [StringLength(50, ErrorMessage = "Il nome della categoria deve avere al massimo 50 caratteri.")]
        public string Nome { get; set; } = string.Empty;

        // Facoltativi: se mancano si usano l'icona e il colore predefiniti.
        public string? Icona { get; set; }

        public string? Colore { get; set; }
    }

    // Icone e colori che si possono scegliere.
    public record Aspetto(IEnumerable<string> Icone, IEnumerable<string> Colori);

    [HttpGet("aspetto")]
    public Aspetto GetAspetto() => new(AspettoCategoria.Icone, AspettoCategoria.Colori);

    // Controlla icona e colore (solo valori dell'elenco ammesso).
    private static string? ErroreAspetto(CategoriaRequest r, out string icona, out string colore)
    {
        icona = string.IsNullOrWhiteSpace(r.Icona) ? AspettoCategoria.IconaPredefinita : r.Icona.Trim();
        colore = string.IsNullOrWhiteSpace(r.Colore) ? AspettoCategoria.ColorePredefinito : r.Colore.Trim().ToLowerInvariant();

        if (!AspettoCategoria.Icone.Contains(icona)) return "Icona non valida.";
        if (!AspettoCategoria.Colori.Contains(colore)) return "Colore non valido.";
        return null;
    }

    [HttpGet]
    public async Task<IEnumerable<CategoriaDto>> GetCategorie() =>
        await _context.Categorie
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaDto(c.Id, c.Nome, c.Icona, c.Colore, _context.Uscite.Count(u => u.CategoriaId == c.Id)))
            .ToListAsync();

    // Chi accede a Gestione finanze legge le categorie (servono per inserire le uscite);
    // crearle, modificarle ed eliminarle è dell'Amministratore del modulo (o del Super admin).
    [HttpPost]
    [Authorize(Policy = Politiche.FinanzeAmministratore)]
    public async Task<ActionResult<CategoriaDto>> CreaCategoria(CategoriaRequest categoria)
    {
        var nome = categoria.Nome.Trim();

        var errore = ErroreAspetto(categoria, out var icona, out var colore);
        if (errore != null)
            return BadRequest(new { messaggio = errore });

        if (await NomeEsistente(nome, null))
            return Conflict(new { messaggio = "Esiste già una categoria con questo nome." });

        var nuova = new Categoria { Nome = nome, Icona = icona, Colore = colore };
        _context.Categorie.Add(nuova);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategorie), null,
            new CategoriaDto(nuova.Id, nuova.Nome, nuova.Icona, nuova.Colore, 0));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Politiche.FinanzeAmministratore)]
    public async Task<IActionResult> RinominaCategoria(int id, CategoriaRequest categoria)
    {
        var esistente = await _context.Categorie.FindAsync(id);
        if (esistente == null)
            return NotFound();

        var nome = categoria.Nome.Trim();

        var errore = ErroreAspetto(categoria, out var icona, out var colore);
        if (errore != null)
            return BadRequest(new { messaggio = errore });

        if (await NomeEsistente(nome, id))
            return Conflict(new { messaggio = "Esiste già una categoria con questo nome." });

        esistente.Nome = nome;
        esistente.Icona = icona;
        esistente.Colore = colore;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Politiche.FinanzeAmministratore)]
    public async Task<IActionResult> EliminaCategoria(int id)
    {
        var categoria = await _context.Categorie.FindAsync(id);
        if (categoria == null)
            return NotFound();

        var usate = await _context.Uscite.CountAsync(u => u.CategoriaId == id);
        if (usate > 0)
            return Conflict(new
            {
                messaggio = usate == 1
                    ? "La categoria è usata da 1 uscita: spostala in un'altra categoria prima di eliminarla."
                    : $"La categoria è usata da {usate} uscite: spostale in un'altra categoria prima di eliminarla."
            });

        _context.Categorie.Remove(categoria);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Confronto senza distinguere maiuscole/minuscole.
    private Task<bool> NomeEsistente(string nome, int? escludiId)
    {
        var minuscolo = nome.ToLower();
        return _context.Categorie.AnyAsync(c => c.Nome.ToLower() == minuscolo && c.Id != escludiId);
    }
}
