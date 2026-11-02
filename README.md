# Gestionale

Applicazione ASP.NET Core 10 + PostgreSQL divisa in **moduli**, con interfaccia web in
`wwwroot/`. Oggi i moduli sono **Admin**, **Gestione finanze** (uscite per categoria),
**Fantapolitica** (in costruzione) e **Stampa 3D** (oggetti personalizzabili da stampare).

- **Pagina di ingresso** (`index.html`, per tutti): dopo l'accesso, un riquadro per ogni
  modulo abilitato con le sue pagine. In alto, la barra mostra un menu per ogni modulo
  abilitato (es. *Gestione finanze ▾*, *Admin ▾*).
- **Gestione finanze**:
  - **Dashboard** (`dashboard.html`): filtri come le uscite; grafici in un **carosello**
    (swipe su telefono, frecce e pallini sopra): barre delle spese per data (per giorno fino
    a 2 mesi, altrimenti per mese, con tabella dei dati) e torta per categoria (al massimo 6
    fette, le minori in "Altre", con importi e percentuali). Grafici con **Chart.js**
    (`wwwroot/lib/chartjs`). Dati da `GET /api/uscite/riepilogo`.
  - **Uscite** (`uscite.html`): lente per i filtri (categoria, dal, al), **+** per
    inserire, elenco con modifica ed eliminazione; ogni riga ha la striscia del colore e
    l'icona della categoria.
  - **Categorie** (`categorie.html`, solo Amministratore del modulo): icona (Bootstrap Icons,
    `wwwroot/lib/bootstrap-icons`) e colore tra quelli ammessi (`Models/AspettoCategoria.cs`);
    aggiungi, modifica, elimina (solo se non usate). Al primo avvio vengono create: Casa,
    Spesa alimentare, Bollette, Trasporti, Salute, Svago, Altro.
- **Stampa 3D**:
  - **Catalogo** (`stampa3d.html`): 8 oggetti personalizzabili (targhetta, portachiavi, scatola
    con coperchio, portapenne, supporto per telefono, gancio, sottobicchiere, distanziale).
  - **Configuratore** (`stampa3d-configuratore.html?oggetto=codice` o `?progetto=id`): le misure
    dell'oggetto, l'**anteprima 3D** girevole (three.js, `wwwroot/lib/three`) e **Scarica STL**;
    si salva come progetto.
  - **Progetti** (`stampa3d-progetti.html`): i propri progetti; l'Amministratore del modulo
    può mostrare quelli di tutti.
  - Ogni oggetto è un file **OpenSCAD** in `Modelli3d/`; il catalogo (misure, minimi, massimi,
    valori predefiniti) è in `Services/CatalogoStampa3d.cs`. Il server controlla le misure e
    genera l'STL con OpenSCAD (`Services/GeneratoreStl.cs`: massimo 2 generazioni insieme,
    60 secondi, risultati recenti in memoria). Nel Docker OpenSCAD e i font DejaVu sono già
    installati; senza Docker serve OpenSCAD nel PATH (oppure `OPENSCAD_PATH`).
- **Admin** (solo Super admin): **Utenti** (anagrafica e Super admin) e **Moduli** (chi
  accede a ciascun modulo e con che livello).

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
livello: *Nessuno*, *Utente* o *Amministratore* (tabella `Autorizzazioni`). Sopra a tutti
c'è il **Super admin** (`Utenti.SuperAdmin`).

| Livello | Cosa vede e fa |
|---|---|
| **Super admin** | Tutti i moduli, sempre, come Amministratore; è l'unico ad accedere ad **Admin** |
| **Amministratore** di un modulo | Il modulo e la sua gestione (in Gestione finanze: anche le Categorie) |
| **Utente** di un modulo | Il modulo (in Gestione finanze: Dashboard e Uscite) |
| Nessun modulo | Solo la pagina di ingresso, con l'invito a chiedere l'accesso |

| Modulo | Contiene | Livelli assegnabili |
|---|---|---|
| **Gestione finanze** (`finanze`) | Dashboard, Uscite, Categorie | Utente, Amministratore |
| **Fantapolitica** (`fantapolitica`) | Panoramica; Politici ed eventi (solo Amministratore). Pagine in costruzione | Utente, Amministratore |
| **Stampa 3D** (`stampa3d`) | Catalogo, configuratore, progetti propri; l'Amministratore vede i progetti di tutti | Utente, Amministratore |
| **Admin** (`admin`) | Utenti, Moduli | nessuno: solo Super admin |

Dal modulo **Admin** (Super admin):

- **Admin → Utenti**: l'anagrafica (nome, cognome, email, password, attivo) e la casella
  **Super admin**. Un utente appena creato non ha moduli.
- **Admin → Moduli**: l'elenco dei moduli; scegliendo **Gestione finanze** si vedono gli
  utenti autorizzati con il loro livello; con **+** si aggiunge un utente scegliendo il livello
  (anche Amministratore del modulo), da ogni riga si cambia il livello o si toglie l'accesso.
- Le modifiche valgono subito: alla richiesta successiva la persona ha (o perde) l'accesso.
- Il server controlla ogni API con una regola (`Services/Accesso.cs`, classe `Politiche`);
  chi apre una pagina senza accesso torna alla pagina di ingresso.
- Nessuno può togliere a se stesso il Super admin o disattivarsi, e deve sempre restare
  almeno un Super admin attivo.

**Aggiungere un modulo**: una riga in `Moduli.Tutti` (codice, nome, descrizione, livelli),
una regola in `Politiche` e `[Authorize(Policy = ...)]` sui suoi controller; in
`componenti.js` una voce in `MODULI`, `DEFINIZIONE_MODULI` (nome, icona, pagine) e
`ORDINE_MODULI`; nelle sue pagine `avviaPagina({ modulo: "codice" })`. Comparirà da solo nel
menu, nella pagina di ingresso e in Admin → Moduli.

## Accesso e utenti

Per usare l'app bisogna accedere con email e password. Gli utenti (nome, cognome, email e
autorizzazioni ai moduli) sono salvati nelle tabelle `Utenti` e `Autorizzazioni`; le password sono salvate solo come hash (PBKDF2 di ASP.NET Core Identity).

- **Primo amministratore**: all'avvio, se la tabella `Utenti` è vuota, viene creato con
  le variabili d'ambiente `ADMIN_EMAIL` e `ADMIN_PASSWORD` (almeno 8 caratteri), come
  **Super admin**.
  Dopo il primo avvio queste variabili non servono più e si possono togliere.
- **Nome e cognome**: obbligatori per i nuovi utenti; gli utenti creati prima di questa
  funzione compaiono come "Nome non inserito" finché non vengono completati con *Modifica*.
- **Gestione utenti** (Admin → Utenti): creare utenti, disattivarli, reimpostare la
  password, eliminarli. Le autorizzazioni si danno da Admin → Moduli.
- **Password dimenticata / account bloccato**: imposta `RESET_EMAIL` (l'email dell'utente)
  e `RESET_PASSWORD` (la nuova password, almeno 8 caratteri) e riavvia: all'avvio l'utente
  riceve la nuova password e viene sbloccato e riattivato. Dopo l'accesso **togli le due
  variabili**.
- **Uscite condivise**: chi ha accesso a Gestione finanze vede e gestisce tutte le uscite; per
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
| POST   | `/api/categorie`    | Crea categoria con nome, icona, colore (Amministratore di Gestione finanze) |
| PUT/DELETE | `/api/categorie/{id}` | Modifica / elimina categoria (Amministratore di Gestione finanze) |
| GET    | `/api/stampa3d/catalogo` | Oggetti del catalogo con le loro misure (Stampa 3D) |
| POST   | `/api/stampa3d/stl` | File STL di un oggetto, es. `{"oggetto":"targhetta","parametri":{"testo":"CASA"}}` |
| GET    | `/api/stampa3d/progetti?tutti=` | I miei progetti (tutti, per l'Amministratore) |
| GET/POST/PUT/DELETE | `/api/stampa3d/progetti/{id}` | Leggi / crea / modifica / elimina un progetto |
| POST   | `/api/auth/login`   | Accesso (email, password) |
| POST   | `/api/auth/logout`  | Uscita               |
| GET    | `/api/auth/me`      | Utente corrente      |
| POST   | `/api/auth/password`| Cambio della propria password |
| GET    | `/api/moduli`       | Moduli dell'app e loro livelli (con il numero di utenti per il Super admin) |
| GET    | `/api/moduli/{codice}/autorizzazioni` | Utenti con accesso al modulo e livello (Super admin) |
| PUT    | `/api/moduli/{codice}/autorizzazioni/{utenteId}` | Dà accesso / cambia livello, es. `{"livello":"Utente"}` (Super admin) |
| DELETE | `/api/moduli/{codice}/autorizzazioni/{utenteId}` | Toglie l'accesso al modulo (Super admin) |
| GET/POST | `/api/utenti`     | Elenco / crea utenti con autorizzazioni (Super admin) |
| PUT/DELETE | `/api/utenti/{id}` | Modifica / elimina utente (Super admin) |
| PUT    | `/api/utenti/{id}/autorizzazioni` | Solo le autorizzazioni, es. `{"uscite":"Utente","admin":null}` (Super admin) |

Un'uscita richiede data, importo maggiore di zero e categoria; le note (max 1000
caratteri) sono facoltative. Se i dati non sono validi l'API risponde
`400 Bad Request` con i dettagli dell'errore.
