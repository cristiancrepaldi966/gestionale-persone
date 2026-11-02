# Gestionale Persone

Piccola applicazione ASP.NET Core 10 + PostgreSQL per gestire un elenco di persone
(inserimento, modifica, eliminazione), con interfaccia web in `wwwroot/index.html`.

## Avvio rapido con Docker (consigliato)

Serve solo [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
docker compose up --build
```

Poi apri <http://localhost:8080>. Il database viene creato automaticamente.
Per fermare: `Ctrl+C`, per cancellare anche i dati: `docker compose down -v`.

## Avvio senza Docker

Servono [.NET 10 SDK](https://dotnet.microsoft.com/download) e un PostgreSQL in esecuzione.

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=gestionale;Username=postgres;Password=postgres"
dotnet run --environment Development
```

In ambiente `Development` è disponibile anche Swagger su `/swagger` per provare l'API.

## Configurazione del database

La connessione viene letta, in ordine, da:

1. `DATABASE_URL` nel formato `postgres://utente:password@host:porta/database`
   (è il formato fornito da Render e da molti altri hosting);
2. `ConnectionStrings__DefaultConnection` (variabile d'ambiente) oppure
   `ConnectionStrings:DefaultConnection` in `appsettings.json`.

All'avvio l'applicazione applica automaticamente le migrazioni (`Migrations/`),
quindi la tabella `Persone` viene creata o aggiornata da sola.

### Modificare il modello dati

Dopo aver cambiato `Models/Persona.cs`:

```bash
dotnet tool install --global dotnet-ef   # una volta sola
dotnet ef migrations add NomeModifica
```

## API

| Metodo | URL                 | Descrizione          |
|--------|---------------------|----------------------|
| GET    | `/api/persone`      | Elenco persone       |
| GET    | `/api/persone/{id}` | Dettaglio persona    |
| POST   | `/api/persone`      | Crea persona         |
| PUT    | `/api/persone/{id}` | Modifica persona     |
| DELETE | `/api/persone/{id}` | Elimina persona      |

Nome e cognome sono obbligatori (massimo 100 caratteri): in caso contrario l'API
risponde `400 Bad Request` con i dettagli dell'errore.
