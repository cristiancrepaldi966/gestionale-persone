using System.Security.Claims;
using GestionalePersone.Models;
using Microsoft.AspNetCore.Authorization;

namespace GestionalePersone.Services;

// Autorizzazioni per modulo: ogni livello è un "claim" nel cookie di accesso,
// nella forma "modulo:Livello" (es. "uscite:Utente", "admin:Amministratore").
public static class Accesso
{
    public const string TipoClaim = "modulo";

    public static string Valore(string modulo, string livello) => $"{modulo}:{livello}";

    public static IEnumerable<Claim> Claims(IEnumerable<Autorizzazione> autorizzazioni) =>
        autorizzazioni.Select(a => new Claim(TipoClaim, Valore(a.Modulo, a.Livello)));

    // Livello dell'utente collegato nel modulo, null se non ha accesso.
    public static string? Livello(this ClaimsPrincipal utente, string modulo) =>
        utente.FindAll(TipoClaim)
            .Select(c => c.Value.Split(':', 2))
            .Where(p => p.Length == 2 && p[0] == modulo)
            .Select(p => p[1])
            .FirstOrDefault();

    public static bool HaAccesso(this ClaimsPrincipal utente, string modulo) => utente.Livello(modulo) != null;

    public static bool E(this ClaimsPrincipal utente, string modulo, string livello) =>
        utente.Livello(modulo) == livello;

    // Dizionario modulo → livello (per le API).
    public static Dictionary<string, string> Mappa(IEnumerable<Autorizzazione> autorizzazioni) =>
        autorizzazioni.ToDictionary(a => a.Modulo, a => a.Livello);
}

// Regole di accesso usate dai controller con [Authorize(Policy = ...)].
public static class Politiche
{
    // Modulo Uscite (con la Dashboard): qualsiasi livello.
    public const string Uscite = "Uscite";

    // Modulo Admin: Amministratore.
    public const string Admin = "Admin";

    // Leggere le categorie: serve sia a Uscite (per scegliere la categoria) sia ad Admin.
    public const string LetturaCategorie = "LetturaCategorie";

    public static void Configura(AuthorizationOptions o)
    {
        o.AddPolicy(Uscite, p => p.RequireAssertion(c => c.User.HaAccesso(Moduli.Uscite)));
        o.AddPolicy(Admin, p => p.RequireAssertion(c => c.User.E(Moduli.Admin, Livelli.Amministratore)));
        o.AddPolicy(LetturaCategorie, p => p.RequireAssertion(c =>
            c.User.HaAccesso(Moduli.Uscite) || c.User.HaAccesso(Moduli.Admin)));
    }
}
