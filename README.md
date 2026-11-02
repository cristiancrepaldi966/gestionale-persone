# Gestionale Uscite

Piccola applicazione ASP.NET Core 10 + PostgreSQL per registrare le uscite (spese)
divise per categoria, con interfaccia web in `wwwroot/`.

- **Uscite** (`index.html`): in alto a destra il pulsante con la lente apre i filtri
  (categoria, dal, al, sulla stessa riga) e il pulsante **+** inserisce un'uscita; sotto
  numero e totale delle uscite trovate e l'elenco con modifica ed eliminazione. Un pallino
  rosso sulla lente indica che ci sono filtri attivi.
- **Dashboard** (`dashboard.html`, prima voce del menu): stessi filtri delle uscite; i grafici
  sono in un **carosello** (swipe su telefono, frecce e pallini sopra, frecce della tastiera); grafico
  a barre delle spese per data (per giorno fino a 2 mesi, altrimenti per mese, con tabella dei
  dati) e grafico a torta per categoria (al massimo 6 fette, le minori in "Altre", con elenco
  di importi e percentuali; colore fisso per categoria). Grafici con **Chart.js** (in
  `wwwroot/lib/chartjs`). Dati da `GET /api/uscite/riepilogo`.
- **Menu**: mostra solo i moduli a cui la persona ha accesso: Dashboard e Uscite (modulo
  Uscite) e **Admin** con la tendina Utenti / Moduli (modulo Admin).
- **Icona e colore delle categorie**: ogni categoria ha un'icona (Bootstrap Icons, in
  `wwwroot/lib/bootstrap-icons`) e un colore scelti dall'amministratore tra quelli ammessi
  (`Models/AspettoCategoria.cs`). Nell'elenco delle uscite ogni riga ha la striscia del colore
  e l'icona della categoria; la dashboard usa gli stessi colori e icone.
- **Admin → Utenti** (solo anagrafica) e **Admin → Moduli → Uscite → Categorie**: stesso layout delle uscite (lente per cercare, + per inserire,
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

## Moduli e autorizzazioni

L'app è divisa in **moduli** (`Models/Moduli.cs`). Ogni persona ha, per ogni modulo, un
livello: *Nessuno*, *Utente* o *Amministratore* (tabella `Autorizzazioni`).

| Modulo | Contiene | Livelli |
|---|---|---|
| **Uscite** | Dashboard, elenco e inserimento delle uscite, categorie | Utente, Amministratore (per ora con gli stessi permessi) |
| **Admin** | Utenti, autorizzazioni ai moduli | Amministratore |

Tutto si gestisce dal modulo **Admin**:

- **Admin → Utenti**: l'anagrafica delle persone (nome, cognome, email, password, attivo).
  Un utente appena creato non ha accesso a nessun modulo.
- **Admin → Moduli**: l'elenco dei moduli. Scegliendo un modulo (es. **Uscite**) si apre la
  sua pagina:
  - scheda **Autorizzazioni**: gli utenti che accedono al modulo e il loro livello; con **+**
    si aggiunge un utente al modulo scegliendo il livello; da ogni riga si cambia il livello
    o si toglie l'accesso;
  - scheda **Categorie** (solo Uscite): le categorie del modulo.
- Valgono subito: alla richiesta successiva la persona ha (o perde) l'accesso.
- Il server controlla ogni API con una regola per modulo (`Services/Accesso.cs`,
  classe `Politiche`); le pagine mostrano solo i moduli consentiti e chi apre una pagina
  senza accesso viene portato alla prima pagina consentita (o a *Nessun accesso*).
- Le categorie si leggono da Uscite (per sceglierle) e si gestiscono da Admin.
- Nessuno può togliere a se stesso l'amministrazione del modulo Admin, e deve sempre restare
  almeno un amministratore attivo del modulo Admin.

**Aggiungere un modulo**: una riga in `Moduli.Tutti` (codice, nome, descrizione, livelli),
una regola in `Politiche`, `[Authorize(Policy = ...)]` sui suoi controller, la voce di menu
in `menuPagine` (`componenti.js`) e `avviaPagina({ modulo: "codice" })` nelle sue pagine.
Comparirà da solo in Admin → Moduli, pronto per assegnare le autorizzazioni.

## Accesso e utenti

Per usare l'app bisogna accedere con email e password. Gli utenti (nome, cognome, email e
autorizzazioni ai moduli) sono salvati nelle tabelle `Utenti` e `Autorizzazioni`; le password sono salvate solo come hash (PBKDF2 di ASP.NET Core Identity).

- **Primo amministratore**: all'avvio, se la tabella `Utenti` è vuota, viene creato con
  le variabili d'ambiente `ADMIN_EMAIL` e `ADMIN_PASSWORD` (almeno 8 caratteri), con il
  livello più alto in tutti i moduli.
  Dopo il primo avvio queste variabili non servono più e si possono togliere.
- **Nome e cognome**: obbligatori per i nuovi utenti; gli utenti creati prima di questa
  funzione compaiono come "Nome non inserito" finché non vengono completati con *Modifica*.
- **Gestione utenti** (Admin → Utenti): creare utenti, disattivarli, reimpostare la
  password, eliminarli. Le autorizzazioni si danno da Admin → Moduli.
- **Password dimenticata / account bloccato**: imposta `RESET_EMAIL` (l'email dell'utente)
  e `RESET_PASSWORD` (la nuova password, almeno 8 caratteri) e riavvia: all'avvio l'utente
  riceve la nuova password e viene sbloccato e riattivato. Dopo l'accesso **togli le due
  variabili**.
- **Uscite condivise**: chi ha accesso al modulo Uscite vede e gestisce tutte le uscite; per
  ognuna è indicato chi l'ha inserita.
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
| POST   | `/api/categorie`    | Crea categoria con nome, icona, colore (modulo Admin) |
| PUT/DELETE | `/api/categorie/{id}` | Rinomina / elimina categoria (modulo Admin) |
| POST   | `/api/auth/login`   | Accesso (email, password) |
| POST   | `/api/auth/logout`  | Uscita               |
| GET    | `/api/auth/me`      | Utente corrente      |
| POST   | `/api/auth/password`| Cambio della propria password |
| GET    | `/api/moduli`       | Moduli dell'app e loro livelli (con il numero di utenti per Admin) |
| GET    | `/api/moduli/{codice}/autorizzazioni` | Utenti con accesso al modulo e livello (modulo Admin) |
| PUT    | `/api/moduli/{codice}/autorizzazioni/{utenteId}` | Dà accesso / cambia livello, es. `{"livello":"Utente"}` (modulo Admin) |
| DELETE | `/api/moduli/{codice}/autorizzazioni/{utenteId}` | Toglie l'accesso al modulo (modulo Admin) |
| GET/POST | `/api/utenti`     | Elenco / crea utenti con autorizzazioni (modulo Admin) |
| PUT/DELETE | `/api/utenti/{id}` | Modifica / elimina utente (modulo Admin) |
| PUT    | `/api/utenti/{id}/autorizzazioni` | Solo le autorizzazioni, es. `{"uscite":"Utente","admin":null}` (modulo Admin) |

Un'uscita richiede data, importo maggiore di zero e categoria; le note (max 1000
caratteri) sono facoltative. Se i dati non sono validi l'API risponde
`400 Bad Request` con i dettagli dell'errore.
