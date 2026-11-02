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
//  1. DATABASE_URL (formato postgres://utente:password@host:porta/db, es. Render)
//  2. ConnectionStrings__DefaultConnection / appsettings.json
static string GetConnectionString(IConfiguration config)
{
    var databaseUrl = config["DATABASE_URL"];

    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            // Usa SSL se il server lo supporta (Render esterno), altrimenti in chiaro.
            SslMode = SslMode.Prefer,
        };

        return csb.ConnectionString;
    }

    var connectionString = config.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connessione al database non configurata: imposta DATABASE_URL " +
            "oppure ConnectionStrings__DefaultConnection.");
    }

    return connectionString;
}
