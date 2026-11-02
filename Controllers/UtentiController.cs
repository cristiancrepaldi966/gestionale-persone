using System.Security.Claims;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

// Modulo Admin: persone e loro autorizzazioni ai moduli.
[ApiController]
[Route("api/utenti")]
[Authorize(Policy = Politiche.Admin)]
public class UtentiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ServizioUtenti _utenti;

    public UtentiController(AppDbContext context, ServizioUtenti utenti)
    {
        _context = context;
        _utenti = utenti;
    }

    // Autorizzazioni: codice del modulo → livello (solo i moduli a cui ha accesso).
    public record UtenteDto(
        int Id, string Nome, string Cognome, string Email, Dictionary<string, string> Autorizzazioni,
        bool Attivo, DateTime CreatoIl, DateTime? UltimoAccesso, bool Bloccato);

    // Campi "string?": se mancano risponde il nostro controllo (in italiano), non quello automatico.
    // Autorizzazioni: modulo → livello; livello vuoto o null = nessun accesso.
    public record NuovoUtenteRequest(string? Nome, string? Cognome, string? Email, string? Password,
        Dictionary<string, string?>? Autorizzazioni);

    public record ModificaUtenteRequest(string? Nome, string? Cognome, bool Attivo, string? NuovaPassword,
        Dictionary<string, string?>? Autorizzazioni);

    private static UtenteDto ToDto(Utente u) => new(
        u.Id, u.Nome, u.Cognome, u.Email, Accesso.Mappa(u.Autorizzazioni), u.Attivo, u.CreatoIl, u.UltimoAccesso,
        u.BloccatoFinoA > DateTime.UtcNow);

    // Nome e cognome: obbligatori, massimo 100 caratteri ciascuno.
    private static string? ErroreNome(string? nome, string? cognome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "Inserisci il nome.";
        if (string.IsNullOrWhiteSpace(cognome)) return "Inserisci il cognome.";
        if (nome.Trim().Length > 100 || cognome.Trim().Length > 100)
            return "Nome e cognome devono avere al massimo 100 caratteri.";
        return null;
    }

    // Controlla le autorizzazioni richieste: moduli esistenti e livelli ammessi.
    private static string? ErroreAutorizzazioni(Dictionary<string, string?>? richieste,
        out List<Autorizzazione> valide)
    {
        valide = [];
        foreach (var (modulo, livello) in richieste ?? [])
        {
            if (Moduli.Trova(modulo) == null)
                return $"Modulo sconosciuto: {modulo}.";
            if (string.IsNullOrWhiteSpace(livello))
                continue;   // nessun accesso
            if (!Moduli.LivelloValido(modulo, livello))
                return $"Livello non valido per il modulo {Moduli.Trova(modulo)!.Nome}.";
            valide.Add(new Autorizzazione { Modulo = modulo, Livello = livello });
        }
        return null;
    }

    private static bool EAmministratore(IEnumerable<Autorizzazione> autorizzazioni) =>
        autorizzazioni.Any(a => a.Modulo == Moduli.Admin && a.Livello == Livelli.Amministratore);

    private int IdCorrente => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IEnumerable<UtenteDto>> GetUtenti()
    {
        var utenti = await _context.Utenti
            .Include(u => u.Autorizzazioni)
            .OrderBy(u => u.Cognome == "")      // chi non ha ancora nome e cognome in fondo
            .ThenBy(u => u.Cognome).ThenBy(u => u.Nome).ThenBy(u => u.Email)
            .ToListAsync();
        return utenti.Select(ToDto);
    }

    [HttpPost]
    public async Task<ActionResult<UtenteDto>> CreaUtente(NuovoUtenteRequest richiesta)
    {
        var erroreNome = ErroreNome(richiesta.Nome, richiesta.Cognome);
        if (erroreNome != null)
            return BadRequest(new { messaggio = erroreNome });

        var email = ServizioUtenti.NormalizzaEmail(richiesta.Email ?? "");

        if (!ServizioUtenti.EmailValida(email))
            return BadRequest(new { messaggio = "Inserisci un'email valida." });

        var errore = ServizioUtenti.ErrorePassword(richiesta.Password);
        if (errore != null)
            return BadRequest(new { messaggio = errore });

        var erroreAut = ErroreAutorizzazioni(richiesta.Autorizzazioni, out var autorizzazioni);
        if (erroreAut != null)
            return BadRequest(new { messaggio = erroreAut });

        if (await _context.Utenti.AnyAsync(u => u.Email == email))
            return Conflict(new { messaggio = "Esiste già un utente con questa email." });

        var utente = new Utente
        {
            Nome = richiesta.Nome!.Trim(),
            Cognome = richiesta.Cognome!.Trim(),
            Email = email,
            Autorizzazioni = autorizzazioni
        };
        _utenti.ImpostaPassword(utente, richiesta.Password!);

        _context.Utenti.Add(utente);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUtenti), null, ToDto(utente));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ModificaUtente(int id, ModificaUtenteRequest richiesta)
    {
        var utente = await _context.Utenti.Include(u => u.Autorizzazioni).SingleOrDefaultAsync(u => u.Id == id);
        if (utente == null)
            return NotFound();

        var erroreNome = ErroreNome(richiesta.Nome, richiesta.Cognome);
        if (erroreNome != null)
            return BadRequest(new { messaggio = erroreNome });

        // Senza "autorizzazioni" nella richiesta, restano quelle attuali.
        var nuove = utente.Autorizzazioni.Select(a => new Autorizzazione { Modulo = a.Modulo, Livello = a.Livello }).ToList();
        if (richiesta.Autorizzazioni != null)
        {
            var erroreAut = ErroreAutorizzazioni(richiesta.Autorizzazioni, out nuove);
            if (erroreAut != null)
                return BadRequest(new { messaggio = erroreAut });
        }

        var erroreAdmin = await ControllaPerditaAmministratore(utente, nuove, richiesta.Attivo);
        if (erroreAdmin != null)
            return BadRequest(new { messaggio = erroreAdmin });

        if (!string.IsNullOrEmpty(richiesta.NuovaPassword))
        {
            var errore = ServizioUtenti.ErrorePassword(richiesta.NuovaPassword);
            if (errore != null)
                return BadRequest(new { messaggio = errore });

            _utenti.ImpostaPassword(utente, richiesta.NuovaPassword);
            utente.TentativiFalliti = 0;
            utente.BloccatoFinoA = null;
        }

        utente.Nome = richiesta.Nome!.Trim();
        utente.Cognome = richiesta.Cognome!.Trim();
        utente.Attivo = richiesta.Attivo;
        SostituisciAutorizzazioni(utente, nuove);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Solo le autorizzazioni (tabella Persone × Moduli della pagina Autorizzazioni).
    [HttpPut("{id}/autorizzazioni")]
    public async Task<IActionResult> ModificaAutorizzazioni(int id, Dictionary<string, string?> richiesta)
    {
        var utente = await _context.Utenti.Include(u => u.Autorizzazioni).SingleOrDefaultAsync(u => u.Id == id);
        if (utente == null)
            return NotFound();

        var erroreAut = ErroreAutorizzazioni(richiesta, out var nuove);
        if (erroreAut != null)
            return BadRequest(new { messaggio = erroreAut });

        var erroreAdmin = await ControllaPerditaAmministratore(utente, nuove, utente.Attivo);
        if (erroreAdmin != null)
            return BadRequest(new { messaggio = erroreAdmin });

        SostituisciAutorizzazioni(utente, nuove);
        await _context.SaveChangesAsync();
        return Ok(ToDto(utente));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminaUtente(int id)
    {
        if (id == IdCorrente)
            return BadRequest(new { messaggio = "Non puoi eliminare il tuo stesso utente." });

        var utente = await _context.Utenti.Include(u => u.Autorizzazioni).SingleOrDefaultAsync(u => u.Id == id);
        if (utente == null)
            return NotFound();

        if (EAmministratore(utente.Autorizzazioni) && utente.Attivo && !await AltriAmministratoriAttivi(id))
            return BadRequest(new { messaggio = "Deve restare almeno un amministratore attivo del modulo Admin." });

        _context.Utenti.Remove(utente);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Nessuno può togliere a se stesso l'amministrazione o disattivarsi,
    // e deve restare almeno un amministratore attivo del modulo Admin.
    private async Task<string?> ControllaPerditaAmministratore(Utente utente, List<Autorizzazione> nuove, bool attivo)
    {
        var perde = EAmministratore(utente.Autorizzazioni) && utente.Attivo &&
                    (!EAmministratore(nuove) || !attivo);
        if (!perde)
            return null;
        if (utente.Id == IdCorrente)
            return "Non puoi togliere a te stesso l'amministrazione del modulo Admin o disattivarti.";
        if (!await AltriAmministratoriAttivi(utente.Id))
            return "Deve restare almeno un amministratore attivo del modulo Admin.";
        return null;
    }

    private void SostituisciAutorizzazioni(Utente utente, List<Autorizzazione> nuove)
    {
        _context.Autorizzazioni.RemoveRange(utente.Autorizzazioni);
        utente.Autorizzazioni = nuove;
        foreach (var a in nuove)
            a.UtenteId = utente.Id;
        _context.Autorizzazioni.AddRange(nuove);
    }

    private Task<bool> AltriAmministratoriAttivi(int escludiId) =>
        _context.Utenti.AnyAsync(u => u.Id != escludiId && u.Attivo &&
            u.Autorizzazioni.Any(a => a.Modulo == Moduli.Admin && a.Livello == Livelli.Amministratore));
}
