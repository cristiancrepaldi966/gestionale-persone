using GestionalePersone.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(GetConnectionString(builder.Configuration)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Crea o aggiorna lo schema del database all'avvio.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
