# Gestionale Uscite

Piccola applicazione ASP.NET Core 10 + PostgreSQL per registrare le uscite (spese)
divise per categoria, con interfaccia web in `wwwroot/`.

- **Uscite** (`index.html`): in alto a destra il pulsante con la lente apre i filtri
  (categoria, dal, al, sulla stessa riga) e il pulsante **+** inserisce un'uscita; sotto
  numero e totale delle uscite trovate e l'elenco con modifica ed eliminazione. Un pallino
  rosso sulla lente indica che ci sono filtri attivi.
- **Dashboard** (`dashboard.html`, prima voce del menu): stessi filtri delle uscite; grafico
  a barre delle spese per data (per giorno fino a 2 mesi, altrimenti per mese, con tabella dei
  dati) e grafico a torta per categoria (al massimo 6 fette, le minori in "Altre", con elenco
  di importi e percentuali; colore fisso per categoria). Grafici con **Chart.js** (in
  `wwwroot/lib/chartjs`). Dati da `GET /api/uscite/riepilogo`.
- **Menu**: Dashboard, Uscite e, solo per gli amministratori, **Admin** con la tendina
  Utenti / Categorie.
- **Icona e colore delle categorie**: ogni categoria ha un'icona (Bootstrap Icons, in
  `wwwroot/lib/bootstrap-icons`) e un colore scelti dall'amministratore tra quelli ammessi
  (`Models/AspettoCategoria.cs`). Nell'elenco delle uscite ogni riga ha la striscia del colore
  e l'icona della categoria; la dashboard usa gli stessi colori e icone.
- **Categorie** e **Utenti** (solo amministratori): stesso layout delle uscite (lente per cercare, + per inserire,
  modifica nella stessa finestra). Categorie: aggiungi, rinomina, elimina (solo se non usate).
  Al primo avvio vengono create: Casa, Spesa alimentare, Bollette, Trasporti, Salute,
  Svago, Altro.

## Interfaccia (wwwroot)

- **Bootstrap 5** (solo CSS, salvato in `wwwroot/lib/bootstrap`, nessun CDN esterno) per
  pulsanti, campi, card, elenchi, etichette, griglia e cerchietti di caricamento.
  Il JavaScript di Bootstrap non è usato: finestra e tendina sono gestite da `componenti.js`
  (le versioni di Bootstrap su iPhone spostano i tocchi quando si apre la tastiera).
- **`componenti.css`**: solo le aggiunte a Bootstrap (pulsanti rotondi, cerchio utente,
  segnaposto delle date, finestra di inserimento su telefono).
- **`componenti.js`**: funzioni comuni (pulsanti con cerchietto di attesa, invio dei moduli,
  pannello di ricerca, modulo di inserimento, messaggi, etichette, formati, chiamate al
  server, barra in alto e menu utente).
- **`js/`**: lo script di ogni pagina, in un file separato (nessuno script dentro l'HTML).
- **`componenti.html`**: pagina di riferimento con un esempio di ogni componente e le classi
  da usare. Le pagine nuove vanno costruite con questi componenti.

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

Per usare l'app bisogna accedere con email e password. Gli utenti (ruolo, nome, cognome,
email) sono salvati nella tabella `Utenti`; le password sono salvate solo come hash (PBKDF2 di ASP.NET Core Identity).

- **Primo amministratore**: all'avvio, se la tabella `Utenti` è vuota, viene creato con
  le variabili d'ambiente `ADMIN_EMAIL` e `ADMIN_PASSWORD` (almeno 8 caratteri).
  Dopo il primo avvio queste variabili non servono più e si possono togliere.
- **Nome e cognome**: obbligatori per i nuovi utenti; gli utenti creati prima di questa
  funzione compaiono come "Nome non inserito" finché non vengono completati con *Modifica*.
- **Gestione utenti** (pagina *Utenti*, solo amministratori): creare utenti, renderli
  amministratori, disattivarli, reimpostare la password, eliminarli. Deve sempre restare
  almeno un amministratore attivo.
- **Password dimenticata / account bloccato**: imposta `RESET_EMAIL` (l'email dell'utente)
  e `RESET_PASSWORD` (la nuova password, almeno 8 caratteri) e riavvia: all'avvio l'utente
  riceve la nuova password e viene sbloccato e riattivato. Dopo l'accesso **togli le due
  variabili**.
- **Ruoli**: gli amministratori gestiscono uscite, categorie e utenti; gli utenti normali
  gestiscono le uscite e possono solo scegliere le categorie esistenti. Tutti gli utenti vedono
  tutte le uscite; per ognuna è indicato chi l'ha inserita.
- **Cambio password**: ognuno può cambiare la propria dalla pagina *Password*; le altre
  sessioni aperte con la vecchia password vengono chiuse.
- **Protezioni**: dopo 5 password errate l'account si blocca per 5 minuti; massimo 10
  tentativi di accesso al minuto per indirizzo IP. La sessione dura 7 giorni e resta valida
  anche dopo riavvii e deploy (le chiavi dei cookie sono salvate nel database).

## Sicurezza contro le injection

- **SQL**: tutte le query passano da Entity Framework (parametri, mai testo concatenato);
  nella ricerca i caratteri speciali `%` e `_` vengono neutralizzati.
- **HTML/JavaScript (XSS)**: i dati vengono sempre inseriti nella pagina come testo
  (`textContent`), mai come HTML. In più la **Content-Security-Policy** permette solo script e
  stili caricati da file del sito: un eventuale script iniettato non viene eseguito. Per questo
  nessuna pagina contiene script o stili scritti dentro l'HTML.
- **Campi non previsti**: le API ricevono oggetti dedicati (solo i campi ammessi), quindi non
  si possono impostare id, autore o altri campi interni.
- **Redirect dopo l'accesso**: il parametro `ritorno` è accettato solo se porta a una pagina
  di questo sito.
- **Altre intestazioni**: `X-Frame-Options: DENY` (la pagina non può essere inserita in siti
  altrui), `X-Content-Type-Options: nosniff`, `Referrer-Policy: same-origin`.

## API

Tutte le API richiedono l'accesso (altrimenti rispondono `401`).

| Metodo | URL                 | Descrizione          |
|--------|---------------------|----------------------|
| GET    | `/api/uscite?q=&categoriaId=&dal=&al=` | Ricerca uscite (con numero e totale) |
| GET    | `/api/uscite/riepilogo?categoriaId=&dal=&al=` | Totali per giorno e per categoria (dashboard) |
| GET    | `/api/uscite/{id}`  | Dettaglio uscita     |
| POST   | `/api/uscite`       | Crea uscita          |
| PUT    | `/api/uscite/{id}`  | Modifica uscita      |
| DELETE | `/api/uscite/{id}`  | Elimina uscita       |
| GET    | `/api/categorie`    | Elenco categorie     |
| GET    | `/api/categorie/aspetto` | Icone e colori ammessi |
| POST   | `/api/categorie`    | Crea categoria con nome, icona, colore (solo admin) |
| PUT/DELETE | `/api/categorie/{id}` | Rinomina / elimina categoria (solo admin) |
| POST   | `/api/auth/login`   | Accesso (email, password) |
| POST   | `/api/auth/logout`  | Uscita               |
| GET    | `/api/auth/me`      | Utente corrente      |
| POST   | `/api/auth/password`| Cambio della propria password |
| GET/POST | `/api/utenti`     | Elenco / crea utenti (solo admin) |
| PUT/DELETE | `/api/utenti/{id}` | Modifica / elimina utente (solo admin) |

Un'uscita richiede data, importo maggiore di zero e categoria; le note (max 1000
caratteri) sono facoltative. Se i dati non sono validi l'API risponde
`400 Bad Request` con i dettagli dell'errore.
