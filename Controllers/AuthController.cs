using System.Security.Claims;
using GestionalePersone.Data;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    public const string ClaimVersione = "versione_sessione";

    private readonly ServizioUtenti _utenti;
    private readonly AppDbContext _context;

    public AuthController(ServizioUtenti utenti, AppDbContext context)
    {
        _utenti = utenti;
        _context = context;
    }

    public record LoginRequest(string Email, string Password);

    public record CambioPasswordRequest(string PasswordAttuale, string NuovaPassword);

    // Moduli: codice del modulo → livello (solo i moduli a cui ha accesso).
    public record UtenteCorrente(int Id, string Nome, string Cognome, string Email, Dictionary<string, string> Moduli);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<UtenteCorrente>> Login(LoginRequest richiesta)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Email) || string.IsNullOrEmpty(richiesta.Password))
            return Unauthorized(new { messaggio = "Inserisci email e password." });

        var (esito, utente) = await _utenti.VerificaAccesso(richiesta.Email, richiesta.Password);

        if (esito == ServizioUtenti.EsitoAccesso.Bloccato)
            return Unauthorized(new { messaggio = "Troppi tentativi errati. Riprova tra qualche minuto." });

        if (utente == null)
            return Unauthorized(new { messaggio = "Email o password non corretti." });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, utente.Id.ToString()),
            new(ClaimTypes.Name, utente.Email),
            new(ClaimVersione, utente.VersioneSessione.ToString()),
        };
        var autorizzazioni = await _context.Autorizzazioni.AsNoTracking()
            .Where(a => a.UtenteId == utente.Id).ToListAsync();
        claims.AddRange(Accesso.Claims(autorizzazioni));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return new UtenteCorrente(utente.Id, utente.Nome, utente.Cognome, utente.Email, Accesso.Mappa(autorizzazioni));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UtenteCorrente>> Me()
    {
        // Letto dal database: nome e cognome possono essere cambiati dall'amministratore.
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var utente = await _context.Utenti.AsNoTracking().Include(u => u.Autorizzazioni)
            .SingleOrDefaultAsync(u => u.Id == id);
        if (utente == null)
            return Unauthorized();
        return new UtenteCorrente(utente.Id, utente.Nome, utente.Cognome, utente.Email, Accesso.Mappa(utente.Autorizzazioni));
    }

    [HttpPost("password")]
    [Authorize]
    public async Task<IActionResult> CambiaPassword(CambioPasswordRequest richiesta)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var utente = await _context.Utenti.FindAsync(id);

        if (utente == null)
            return Unauthorized();

        if (!_utenti.PasswordCorretta(utente, richiesta.PasswordAttuale ?? ""))
            return BadRequest(new { messaggio = "La password attuale non è corretta." });

        var errore = ServizioUtenti.ErrorePassword(richiesta.NuovaPassword);
        if (errore != null)
            return BadRequest(new { messaggio = errore });

        _utenti.ImpostaPassword(utente, richiesta.NuovaPassword);
        await _context.SaveChangesAsync();

        // La versione di sessione è cambiata: si richiede un nuovo accesso.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return NoContent();
    }
}
