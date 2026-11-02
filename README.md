# Gestionale Uscite

Piccola applicazione ASP.NET Core 10 + PostgreSQL per registrare le uscite (spese)
divise per categoria, con interfaccia web in `wwwroot/`.

- **Dashboard** (`index.html`): ricerca per testo (descrizione e note), categoria e
  intervallo di date, con numero e totale delle uscite trovate; pulsante **+** in alto a
  destra per inserire un'uscita; elenco con modifica ed eliminazione.
- **Categorie** (`categorie.html`): aggiungi, rinomina, elimina (solo se non usate).
  Al primo avvio vengono create: Casa, Spesa alimentare, Bollette, Trasporti, Salute,
  Svago, Altro.

## Avvio rapido con Docker (consigliato)

Serve solo [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
docker compose up --build
```

Poi apri <http://localhost:8080> e accedi con `admin@example.com` / `cambiami123`
(cambia la password dal link **Password** in alto). Il database viene creato automaticamente.
Per fermare: `Ctrl+C`, per cancellare anche i dati: `docker compose down -v`.

## Avvio senza Docker

Servono [.NET 10 SDK](https://dotnet.microsoft.com/download) e un PostgreSQL in esecuzione.

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=gestionale;Username=postgres;Password=postgres"
export ADMIN_EMAIL="admin@example.com" ADMIN_PASSWORD="cambiami123"
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
quindi le tabelle vengono create o aggiornate da sole.

### Modificare il modello dati

Dopo aver cambiato i modelli in `Models/`:

```bash
dotnet tool install --global dotnet-ef   # una volta sola
dotnet ef migrations add NomeModifica
```

## Accesso e utenti

Per usare l'app bisogna accedere con email e password. Gli utenti sono salvati nella
tabella `Utenti`; le password sono salvate solo come hash (PBKDF2 di ASP.NET Core Identity).

- **Primo amministratore**: all'avvio, se la tabella `Utenti` è vuota, viene creato con
  le variabili d'ambiente `ADMIN_EMAIL` e `ADMIN_PASSWORD` (almeno 8 caratteri).
  Dopo il primo avvio queste variabili non servono più e si possono togliere.
- **Gestione utenti** (pagina *Utenti*, solo amministratori): creare utenti, renderli
  amministratori, disattivarli, reimpostare la password, eliminarli. Deve sempre restare
  almeno un amministratore attivo.
- **Ruoli**: gli amministratori gestiscono uscite, categorie e utenti; gli utenti normali
  uscite e categorie. Tutti gli utenti vedono tutte le uscite; per ognuna è indicato chi l'ha inserita.
- **Cambio password**: ognuno può cambiare la propria dalla pagina *Password*; le altre
  sessioni aperte con la vecchia password vengono chiuse.
- **Protezioni**: dopo 5 password errate l'account si blocca per 5 minuti; massimo 10
  tentativi di accesso al minuto per indirizzo IP. La sessione dura 7 giorni e resta valida
  anche dopo riavvii e deploy (le chiavi dei cookie sono salvate nel database).

## API

Tutte le API richiedono l'accesso (altrimenti rispondono `401`).

| Metodo | URL                 | Descrizione          |
|--------|---------------------|----------------------|
| GET    | `/api/uscite?q=&categoriaId=&dal=&al=` | Ricerca uscite (con numero e totale) |
| GET    | `/api/uscite/{id}`  | Dettaglio uscita     |
| POST   | `/api/uscite`       | Crea uscita          |
| PUT    | `/api/uscite/{id}`  | Modifica uscita      |
| DELETE | `/api/uscite/{id}`  | Elimina uscita       |
| GET/POST | `/api/categorie`  | Elenco / crea categorie |
| PUT/DELETE | `/api/categorie/{id}` | Rinomina / elimina categoria |
| POST   | `/api/auth/login`   | Accesso (email, password) |
| POST   | `/api/auth/logout`  | Uscita               |
| GET    | `/api/auth/me`      | Utente corrente      |
| POST   | `/api/auth/password`| Cambio della propria password |
| GET/POST | `/api/utenti`     | Elenco / crea utenti (solo admin) |
| PUT/DELETE | `/api/utenti/{id}` | Modifica / elimina utente (solo admin) |

Un'uscita richiede data, importo maggiore di zero, descrizione (max 200 caratteri) e
categoria; le note sono facoltative. Se i dati non sono validi l'API risponde
`400 Bad Request` con i dettagli dell'errore.
