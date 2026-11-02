using GestionalePersone.Data;
using GestionalePersone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Services;

public class ServizioUtenti
{
    public const int MaxTentativi = 5;
    public static readonly TimeSpan DurataBlocco = TimeSpan.FromMinutes(5);
    public const int LunghezzaMinimaPassword = 8;

    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Utente> _hasher;

    public ServizioUtenti(AppDbContext context, IPasswordHasher<Utente> hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public static string NormalizzaEmail(string email) => email.Trim().ToLowerInvariant();

    // Email valida: formato standard, senza spazi né caratteri come < > " ' ( ) ; ,
    private static readonly System.Text.RegularExpressions.Regex FormatoEmail = new(
        @"^[a-z0-9.!#$%&*+/=?^_`{|}~-]+@[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?)+$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public static bool EmailValida(string emailNormalizzata) =>
        emailNormalizzata.Length <= 256 && FormatoEmail.IsMatch(emailNormalizzata);

    public static string? ErrorePassword(string? password) =>
        string.IsNullOrEmpty(password) || password.Length < LunghezzaMinimaPassword
            ? $"La password deve avere almeno {LunghezzaMinimaPassword} caratteri."
            : null;

    public void ImpostaPassword(Utente utente, string password)
    {
        utente.PasswordHash = _hasher.HashPassword(utente, password);
        utente.VersioneSessione++;
    }

    public enum EsitoAccesso { Ok, CredenzialiErrate, Bloccato }

    public async Task<(EsitoAccesso Esito, Utente? Utente)> VerificaAccesso(string email, string password)
    {
        var normalizzata = NormalizzaEmail(email);
        var utente = await _context.Utenti.SingleOrDefaultAsync(u => u.Email == normalizzata);

        if (utente == null || !utente.Attivo)
        {
            // Calcola comunque un hash per non rivelare, dai tempi di risposta, se l'email esiste.
            _hasher.HashPassword(new Utente(), password);
            return (EsitoAccesso.CredenzialiErrate, null);
        }

        if (utente.BloccatoFinoA > DateTime.UtcNow)
            return (EsitoAccesso.Bloccato, null);

        var risultato = _hasher.VerifyHashedPassword(utente, utente.PasswordHash, password);

        if (risultato == PasswordVerificationResult.Failed)
        {
            utente.TentativiFalliti++;
            if (utente.TentativiFalliti >= MaxTentativi)
            {
                utente.BloccatoFinoA = DateTime.UtcNow.Add(DurataBlocco);
                utente.TentativiFalliti = 0;
            }
            await _context.SaveChangesAsync();
            return (EsitoAccesso.CredenzialiErrate, null);
        }

        if (risultato == PasswordVerificationResult.SuccessRehashNeeded)
            utente.PasswordHash = _hasher.HashPassword(utente, password);

        utente.TentativiFalliti = 0;
        utente.BloccatoFinoA = null;
        utente.UltimoAccesso = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (EsitoAccesso.Ok, utente);
    }

    public bool PasswordCorretta(Utente utente, string password) =>
        _hasher.VerifyHashedPassword(utente, utente.PasswordHash, password) != PasswordVerificationResult.Failed;

    // Reimpostazione d'emergenza della password (per chi non riesce più ad accedere):
    // con RESET_EMAIL e RESET_PASSWORD impostate, all'avvio quell'utente riceve la nuova
    // password e viene sbloccato e riattivato. Dopo l'accesso le variabili vanno tolte.
    public async Task ReimpostaPasswordDaVariabili(IConfiguration config, ILogger logger)
    {
        var email = config["RESET_EMAIL"];
        var password = config["RESET_PASSWORD"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return;

        var errore = ErrorePassword(password);
        if (errore != null)
        {
            logger.LogError("RESET_PASSWORD non valida: {Errore}", errore);
            return;
        }

        var normalizzata = NormalizzaEmail(email);
        var utente = await _context.Utenti.SingleOrDefaultAsync(u => u.Email == normalizzata);

        if (utente == null)
        {
            logger.LogError("RESET_EMAIL: nessun utente con questa email. Utenti presenti: {Numero}.",
                await _context.Utenti.CountAsync());
            return;
        }

        // Se la password è già quella (riavvii successivi) non la si cambia di nuovo,
        // così le sessioni aperte non vengono chiuse a ogni riavvio.
        if (!PasswordCorretta(utente, password))
        {
            ImpostaPassword(utente, password);
            logger.LogWarning("Password reimpostata per {Email} tramite RESET_PASSWORD. " +
                              "Togli RESET_EMAIL e RESET_PASSWORD dalle variabili d'ambiente.", utente.Email);
        }

        utente.Attivo = true;
        utente.TentativiFalliti = 0;
        utente.BloccatoFinoA = null;
        await _context.SaveChangesAsync();
    }

    // Crea il primo amministratore da ADMIN_EMAIL / ADMIN_PASSWORD se la tabella è vuota.
    public async Task CreaAdminIniziale(IConfiguration config, ILogger logger)
    {
        if (await _context.Utenti.AnyAsync())
            return;

        var email = config["ADMIN_EMAIL"];
        var password = config["ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            logger.LogWarning(
                "Nessun utente presente: imposta ADMIN_EMAIL e ADMIN_PASSWORD e riavvia " +
                "per creare il primo amministratore.");
            return;
        }

        var errore = ErrorePassword(password);
        if (errore != null)
        {
            logger.LogError("ADMIN_PASSWORD non valida: {Errore}", errore);
            return;
        }

        if (!EmailValida(NormalizzaEmail(email)))
        {
            logger.LogError("ADMIN_EMAIL non è un indirizzo email valido.");
            return;
        }

        var admin = new Utente { Email = NormalizzaEmail(email), IsAdmin = true };
        ImpostaPassword(admin, password);
        _context.Utenti.Add(admin);
        await _context.SaveChangesAsync();

        logger.LogInformation("Creato l'amministratore iniziale {Email}.", admin.Email);
    }
}
