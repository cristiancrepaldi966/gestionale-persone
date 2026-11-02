using System.Security.Claims;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

[ApiController]
[Route("api/utenti")]
[Authorize(Roles = "Admin")]
public class UtentiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ServizioUtenti _utenti;

    public UtentiController(AppDbContext context, ServizioUtenti utenti)
    {
        _context = context;
        _utenti = utenti;
    }

    public record UtenteDto(
        int Id, string Email, bool IsAdmin, bool Attivo,
        DateTime CreatoIl, DateTime? UltimoAccesso, bool Bloccato);

    public record NuovoUtenteRequest(string Email, string Password, bool IsAdmin);

    public record ModificaUtenteRequest(bool IsAdmin, bool Attivo, string? NuovaPassword);

    private static UtenteDto ToDto(Utente u) => new(
        u.Id, u.Email, u.IsAdmin, u.Attivo, u.CreatoIl, u.UltimoAccesso,
        u.BloccatoFinoA > DateTime.UtcNow);

    private int IdCorrente => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IEnumerable<UtenteDto>> GetUtenti()
    {
        var utenti = await _context.Utenti.OrderBy(u => u.Email).ToListAsync();
        return utenti.Select(ToDto);
    }

    [HttpPost]
    public async Task<ActionResult<UtenteDto>> CreaUtente(NuovoUtenteRequest richiesta)
    {
        var email = ServizioUtenti.NormalizzaEmail(richiesta.Email ?? "");

        if (email.Length == 0 || email.Length > 256 || !email.Contains('@'))
            return BadRequest(new { messaggio = "Inserisci un'email valida." });

        var errore = ServizioUtenti.ErrorePassword(richiesta.Password);
        if (errore != null)
            return BadRequest(new { messaggio = errore });

        if (await _context.Utenti.AnyAsync(u => u.Email == email))
            return Conflict(new { messaggio = "Esiste già un utente con questa email." });

        var utente = new Utente { Email = email, IsAdmin = richiesta.IsAdmin };
        _utenti.ImpostaPassword(utente, richiesta.Password);

        _context.Utenti.Add(utente);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUtenti), null, ToDto(utente));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ModificaUtente(int id, ModificaUtenteRequest richiesta)
    {
        var utente = await _context.Utenti.FindAsync(id);
        if (utente == null)
            return NotFound();

        var perdeAdmin = utente.IsAdmin && (!richiesta.IsAdmin || !richiesta.Attivo);

        if (id == IdCorrente && perdeAdmin)
            return BadRequest(new { messaggio = "Non puoi togliere a te stesso i permessi di amministratore o disattivarti." });

        if (perdeAdmin && !await AltriAdminAttivi(id))
            return BadRequest(new { messaggio = "Deve restare almeno un amministratore attivo." });

        if (!string.IsNullOrEmpty(richiesta.NuovaPassword))
        {
            var errore = ServizioUtenti.ErrorePassword(richiesta.NuovaPassword);
            if (errore != null)
                return BadRequest(new { messaggio = errore });

            _utenti.ImpostaPassword(utente, richiesta.NuovaPassword);
            utente.TentativiFalliti = 0;
            utente.BloccatoFinoA = null;
        }

        utente.IsAdmin = richiesta.IsAdmin;
        utente.Attivo = richiesta.Attivo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminaUtente(int id)
    {
        if (id == IdCorrente)
            return BadRequest(new { messaggio = "Non puoi eliminare il tuo stesso utente." });

        var utente = await _context.Utenti.FindAsync(id);
        if (utente == null)
            return NotFound();

        if (utente.IsAdmin && utente.Attivo && !await AltriAdminAttivi(id))
            return BadRequest(new { messaggio = "Deve restare almeno un amministratore attivo." });

        _context.Utenti.Remove(utente);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private Task<bool> AltriAdminAttivi(int escludiId) =>
        _context.Utenti.AnyAsync(u => u.Id != escludiId && u.IsAdmin && u.Attivo);
}
