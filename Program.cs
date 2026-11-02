using System.Security.Claims;
using System.Threading.RateLimiting;
using GestionalePersone.Controllers;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(GetConnectionString(builder.Configuration)));

builder.Services.AddScoped<IPasswordHasher<Utente>, PasswordHasher<Utente>>();
builder.Services.AddScoped<ServizioUtenti>();

// Chiavi dei cookie salvate nel database: l'accesso resta valido dopo riavvii e deploy.
builder.Services.AddDataProtection()
    .SetApplicationName("GestionalePersone")
    .PersistKeysToDbContext<AppDbContext>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "gestionale_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        // È un'API: rispondi 401/403 invece di reindirizzare a una pagina.
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };

        // A ogni richiesta controlla che l'utente esista ancora, sia attivo e non abbia
        // cambiato password; aggiorna il ruolo se l'amministratore lo ha modificato.
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var id = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var versione = ctx.Principal?.FindFirstValue(AuthController.ClaimVersione);
            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

            var utente = int.TryParse(id, out var idUtente)
                ? await db.Utenti.AsNoTracking().SingleOrDefaultAsync(u => u.Id == idUtente)
                : null;

            if (utente == null || !utente.Attivo || versione != utente.VersioneSessione.ToString())
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            if (ctx.Principal!.IsInRole("Admin") != utente.IsAdmin)
            {
                var identity = (ClaimsIdentity)ctx.Principal.Identity!;
                foreach (var ruolo in identity.FindAll(ClaimTypes.Role).ToList())
                    identity.RemoveClaim(ruolo);
                if (utente.IsAdmin)
                    identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                ctx.ShouldRenew = true;
            }
        };
    });

builder.Services.AddAuthorization();

// Al massimo 10 tentativi di login al minuto per indirizzo IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "sconosciuto",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// Render (e altri hosting) mettono l'app dietro un proxy che gestisce HTTPS.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Crea o aggiorna lo schema del database e il primo amministratore all'avvio.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    await scope.ServiceProvider.GetRequiredService<ServizioUtenti>()
        .CreaAdminIniziale(app.Configuration, app.Logger);
}

app.UseForwardedHeaders();

// Intestazioni di sicurezza: il browser esegue solo script e stili caricati
// da file di questo sito (niente script iniettati), la pagina non può essere
// inserita in siti altrui e i tipi di file non vengono "indovinati".
app.Use(async (context, next) =>
{
    var h = context.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "same-origin";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    // Swagger (solo in sviluppo) usa script interni: lì niente CSP.
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        h["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self'; " +
            "img-src 'self' data:; font-src 'self'; connect-src 'self'; " +
            "object-src 'none'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'";
    }

    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// La connessione si legge, in ordine, da:
//  1. DATABASE_URL
//  2. ConnectionStrings__DefaultConnection / appsettings.json
// Entrambe accettano sia il formato URL (postgres://utente:password@host:porta/db,
// quello fornito da Render) sia il formato Npgsql (Host=...;Database=...;...).
static string GetConnectionString(IConfiguration config)
{
    var fonti = new[]
    {
        ("DATABASE_URL", config["DATABASE_URL"]),
        ("ConnectionStrings__DefaultConnection", config.GetConnectionString("DefaultConnection")),
    };

    foreach (var (nome, valore) in fonti)
    {
        if (!string.IsNullOrWhiteSpace(valore))
        {
            return NormalizzaConnessione(nome, valore);
        }
    }

    throw new InvalidOperationException(
        "Connessione al database non configurata: imposta DATABASE_URL " +
        "oppure ConnectionStrings__DefaultConnection.");
}

static string NormalizzaConnessione(string nome, string valore)
{
    // Spazi, a capo o virgolette copiati per errore insieme al valore.
    valore = valore.Trim().Trim('"', '\'').Trim();

    try
    {
        if (valore.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            valore.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return DaUrl(valore);
        }

        // Verifica subito che il formato sia valido.
        return new NpgsqlConnectionStringBuilder(valore).ConnectionString;
    }
    catch (Exception ex) when (ex is ArgumentException or UriFormatException or FormatException)
    {
        // Il valore non viene stampato per non mostrare la password nei log.
        throw new InvalidOperationException(
            $"La variabile {nome} non contiene una connessione PostgreSQL valida " +
            $"(lunghezza {valore.Length} caratteri). Usa il formato " +
            "postgresql://utente:password@host:porta/database (es. l'Internal Database URL " +
            "di Render) oppure Host=...;Port=5432;Database=...;Username=...;Password=... " +
            $"Dettaglio: {ex.Message}");
    }
}

static string DaUrl(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':', 2);

    var csb = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        // Usa SSL se il server lo supporta (Render esterno), altrimenti in chiaro.
        SslMode = SslMode.Prefer,
    };

    // Rispetta un eventuale ?sslmode=require nell'URL.
    foreach (var parte in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var kv = parte.Split('=', 2);
        if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<SslMode>(kv[1].Replace("-", ""), ignoreCase: true, out var modo))
        {
            csb.SslMode = modo;
        }
    }

    return csb.ConnectionString;
}
