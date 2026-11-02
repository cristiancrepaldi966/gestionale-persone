using System.Security.Claims;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

// Moduli dell'app e, dal modulo Admin (Super admin), le autorizzazioni di ciascun modulo:
// si sceglie il modulo (es. Gestione finanze) e si decide chi vi accede e con che livello.
[ApiController]
[Authorize]
[Route("api/moduli")]
public class ModuliController : ControllerBase
{
    private readonly AppDbContext _context;

    public ModuliController(AppDbContext context)
    {
        _context = context;
    }

    public record ModuloDto(string Codice, string Nome, string Descrizione, string[] Livelli, int? NumeroUtenti);

    public record AccessoDto(int UtenteId, string Nome, string Cognome, string Email, bool Attivo, string Livello);

    public record LivelloRequest(string? Livello);

    private int IdCorrente => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Elenco dei moduli (per tutti); il numero di utenti solo per il Super admin.
    [HttpGet]
    public async Task<IEnumerable<ModuloDto>> GetModuli()
    {
        var admin = User.SuperAdmin();
        var conteggi = admin
            ? await _context.Autorizzazioni.GroupBy(a => a.Modulo)
                .Select(g => new { g.Key, Numero = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Numero)
            : [];
        return Moduli.Tutti.Select(m => new ModuloDto(m.Codice, m.Nome, m.Descrizione, m.Livelli,
            admin ? conteggi.GetValueOrDefault(m.Codice) : null));
    }

    // Utenti con accesso al modulo e loro livello.
    [HttpGet("{codice}/autorizzazioni")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<ActionResult<IEnumerable<AccessoDto>>> GetAutorizzazioni(string codice)
    {
        if (Moduli.Trova(codice) is not { Livelli.Length: > 0 })
            return NotFound(new { messaggio = "Modulo sconosciuto o non assegnabile." });

        return await _context.Autorizzazioni
            .Where(a => a.Modulo == codice)
            .OrderBy(a => a.Utente.Cognome == "").ThenBy(a => a.Utente.Cognome).ThenBy(a => a.Utente.Nome)
            .ThenBy(a => a.Utente.Email)
            .Select(a => new AccessoDto(a.UtenteId, a.Utente.Nome, a.Utente.Cognome, a.Utente.Email,
                a.Utente.Attivo, a.Livello))
            .ToListAsync();
    }

    // Dà accesso al modulo o cambia il livello.
    [HttpPut("{codice}/autorizzazioni/{utenteId}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> ImpostaLivello(string codice, int utenteId, LivelloRequest richiesta)
    {
        var modulo = Moduli.Trova(codice);
        if (modulo is not { Livelli.Length: > 0 })
            return NotFound(new { messaggio = "Modulo sconosciuto o non assegnabile." });

        if (string.IsNullOrWhiteSpace(richiesta.Livello) || !Moduli.LivelloValido(codice, richiesta.Livello))
            return BadRequest(new { messaggio = $"Livello non valido per il modulo {modulo.Nome}." });

        var utente = await _context.Utenti.Include(u => u.Autorizzazioni).SingleOrDefaultAsync(u => u.Id == utenteId);
        if (utente == null)
            return NotFound(new { messaggio = "Utente non trovato." });

        var esistente = utente.Autorizzazioni.FirstOrDefault(a => a.Modulo == codice);

        if (esistente == null)
            _context.Autorizzazioni.Add(new Autorizzazione { UtenteId = utenteId, Modulo = codice, Livello = richiesta.Livello });
        else
            esistente.Livello = richiesta.Livello;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Toglie l'accesso al modulo.
    [HttpDelete("{codice}/autorizzazioni/{utenteId}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> TogliAccesso(string codice, int utenteId)
    {
        if (Moduli.Trova(codice) is not { Livelli.Length: > 0 })
            return NotFound(new { messaggio = "Modulo sconosciuto o non assegnabile." });

        var esistente = await _context.Autorizzazioni.SingleOrDefaultAsync(a => a.UtenteId == utenteId && a.Modulo == codice);
        if (esistente == null)
            return NotFound(new { messaggio = "L'utente non ha accesso a questo modulo." });

        _context.Autorizzazioni.Remove(esistente);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
