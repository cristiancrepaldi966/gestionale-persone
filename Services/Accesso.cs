using System.Security.Claims;
using GestionalePersone.Models;
using Microsoft.AspNetCore.Authorization;

namespace GestionalePersone.Services;

// Autorizzazioni: ogni livello è un "claim" nel cookie di accesso, nella forma
// "modulo:Livello" (es. "finanze:Utente"); il Super admin ha in più il claim "superadmin".
public static class Accesso
{
    public const string TipoClaim = "modulo";
    public const string TipoSuperAdmin = "superadmin";

    public static string Valore(string modulo, string livello) => $"{modulo}:{livello}";

    // Claim di un utente (con le autorizzazioni caricate).
    public static IEnumerable<Claim> Claims(Utente utente)
    {
        foreach (var a in utente.Autorizzazioni)
            yield return new Claim(TipoClaim, Valore(a.Modulo, a.Livello));
        if (utente.SuperAdmin)
            yield return new Claim(TipoSuperAdmin, "true");
    }

    // Tipi di claim che descrivono le autorizzazioni (per accorgersi se sono cambiate).
    public static bool EAutorizzazione(Claim c) => c.Type == TipoClaim || c.Type == TipoSuperAdmin;

    public static bool SuperAdmin(this ClaimsPrincipal utente) => utente.HasClaim(TipoSuperAdmin, "true");

    // Livello dell'utente collegato nel modulo, null se non ha accesso.
    // Il Super admin è Amministratore di tutti i moduli.
    public static string? Livello(this ClaimsPrincipal utente, string modulo)
    {
        if (utente.SuperAdmin()) return Livelli.Amministratore;
        return utente.FindAll(TipoClaim)
            .Select(c => c.Value.Split(':', 2))
            .Where(p => p.Length == 2 && p[0] == modulo)
            .Select(p => p[1])
            .FirstOrDefault();
    }

    public static bool HaAccesso(this ClaimsPrincipal utente, string modulo) => utente.Livello(modulo) != null;

    public static bool E(this ClaimsPrincipal utente, string modulo, string livello) =>
        utente.Livello(modulo) == livello;

    // Dizionario modulo → livello delle autorizzazioni registrate.
    public static Dictionary<string, string> Mappa(IEnumerable<Autorizzazione> autorizzazioni) =>
        autorizzazioni.ToDictionary(a => a.Modulo, a => a.Livello);

    // Moduli effettivamente disponibili: il Super admin li ha tutti come Amministratore.
    public static Dictionary<string, string> ModuliEffettivi(Utente utente) =>
        utente.SuperAdmin
            ? Moduli.Tutti.ToDictionary(m => m.Codice, _ => Livelli.Amministratore)
            : Mappa(utente.Autorizzazioni);
}

// Regole di accesso usate dai controller con [Authorize(Policy = ...)].
public static class Politiche
{
    // Modulo Gestione finanze (Dashboard, uscite): qualsiasi livello.
    public const string Finanze = "Finanze";

    // Gestione finanze, livello Amministratore: gestisce le categorie.
    public const string FinanzeAmministratore = "FinanzeAmministratore";

    // Modulo Fantapolitica: qualsiasi livello.
    public const string Fantapolitica = "Fantapolitica";

    // Fantapolitica, livello Amministratore: gestisce politici ed eventi.
    public const string FantapoliticaAmministratore = "FantapoliticaAmministratore";

    // Modulo Stampa 3D: qualsiasi livello (catalogo, configuratore, i propri progetti).
    public const string Stampa3d = "Stampa3d";

    // Modulo IA: qualsiasi livello (IA 3D).
    public const string Ia = "Ia";

    // Modulo Admin: solo Super admin.
    public const string Admin = "Admin";

    // Leggere le categorie (per scegliere la categoria delle uscite).
    public const string LetturaCategorie = "LetturaCategorie";

    public static void Configura(AuthorizationOptions o)
    {
        o.AddPolicy(Finanze, p => p.RequireAssertion(c => c.User.HaAccesso(Moduli.Finanze)));
        o.AddPolicy(FinanzeAmministratore, p => p.RequireAssertion(c => c.User.E(Moduli.Finanze, Livelli.Amministratore)));
        o.AddPolicy(Fantapolitica, p => p.RequireAssertion(c => c.User.HaAccesso(Moduli.Fantapolitica)));
        o.AddPolicy(FantapoliticaAmministratore, p => p.RequireAssertion(c => c.User.E(Moduli.Fantapolitica, Livelli.Amministratore)));
        o.AddPolicy(Stampa3d, p => p.RequireAssertion(c => c.User.HaAccesso(Moduli.Stampa3d)));
        o.AddPolicy(Ia, p => p.RequireAssertion(c => c.User.HaAccesso(Moduli.Ia)));
        o.AddPolicy(Admin, p => p.RequireAssertion(c => c.User.SuperAdmin()));
        o.AddPolicy(LetturaCategorie, p => p.RequireAssertion(c => c.User.HaAccesso(Moduli.Finanze)));
    }
}
