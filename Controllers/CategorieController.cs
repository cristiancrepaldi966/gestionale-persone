using GestionalePersone.Data;
using GestionalePersone.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

[ApiController]
[Authorize]
[Route("api/categorie")]
public class CategorieController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategorieController(AppDbContext context)
    {
        _context = context;
    }

    public record CategoriaDto(int Id, string Nome, int NumeroUscite);

    [HttpGet]
    public async Task<IEnumerable<CategoriaDto>> GetCategorie() =>
        await _context.Categorie
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaDto(c.Id, c.Nome, _context.Uscite.Count(u => u.CategoriaId == c.Id)))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<CategoriaDto>> CreaCategoria(Categoria categoria)
    {
        var nome = categoria.Nome.Trim();

        if (await NomeEsistente(nome, null))
            return Conflict(new { messaggio = "Esiste già una categoria con questo nome." });

        var nuova = new Categoria { Nome = nome };
        _context.Categorie.Add(nuova);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategorie), null, new CategoriaDto(nuova.Id, nuova.Nome, 0));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> RinominaCategoria(int id, Categoria categoria)
    {
        var esistente = await _context.Categorie.FindAsync(id);
        if (esistente == null)
            return NotFound();

        var nome = categoria.Nome.Trim();

        if (await NomeEsistente(nome, id))
            return Conflict(new { messaggio = "Esiste già una categoria con questo nome." });

        esistente.Nome = nome;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
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
