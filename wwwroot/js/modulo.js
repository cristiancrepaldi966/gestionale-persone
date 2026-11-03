// Pagina di un modulo (modulo.html?codice=...), scheda Autorizzazioni: chi accede al
// modulo e con che livello. Si arriva da Admin → Moduli. Componenti comuni: componenti.js.

const $ = id => document.getElementById(id);

const codice = new URLSearchParams(location.search).get("codice") || "";
const API = `/api/moduli/${encodeURIComponent(codice)}/autorizzazioni`;

let definizione = null;   // { codice, nome, descrizione, livelli }
let accessi = [];         // utenti con accesso: { utenteId, nome, cognome, email, attivo, livello }
let utenti = [];          // tutti gli utenti (per aggiungerli al modulo)
let idCorrente = null;
let inModifica = null;    // accesso in modifica, null se si aggiunge un utente

const nomeCompleto = u => `${u.nome} ${u.cognome}`.trim();

const ricerca = pannelloRicerca({
    bottone: $("cerca"),
    pannello: $("pannelloFiltri"),
    pallino: $("pallinoFiltri"),
    azzera: $("azzera"),
    campi: [$("filtroLivello"), $("filtroTesto")],
    applica: () => mostra()
});

const modulo = moduloFinestra($("finestra"), { primoCampo: $("utente") });


async function carica() {
    try {
        const response = await api(API);
        if (!response.ok) throw new Error();
        accessi = await response.json();
        mostra();
    } catch {
        $("listaAccessi").replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento delle autorizzazioni."));
    }
}

function mostra() {
    const { filtroLivello, filtroTesto } = ricerca.valori();
    const testo = filtroTesto.toLocaleLowerCase("it-IT");
    const trovati = accessi.filter(a =>
        (filtroLivello === "" || a.livello === filtroLivello) &&
        `${a.nome} ${a.cognome} ${a.cognome} ${a.nome} ${a.email}`.toLocaleLowerCase("it-IT").includes(testo));

    $("riepilogo").replaceChildren(el("span", "text-body-secondary",
        trovati.length === 1 ? "1 utente con accesso" : `${trovati.length} utenti con accesso`));

    $("listaAccessi").replaceChildren(...(trovati.length ? trovati.map(riga) : [
        el("p", "p-3 mb-0 text-body-secondary", ricerca.attivi()
            ? "Nessun utente corrisponde alla ricerca."
            : "Nessun utente ha accesso a questo modulo. Usa il pulsante + per aggiungerne uno.")]));
}

function riga(a) {
    const div = el("div", "list-group-item riga d-flex flex-column flex-sm-row justify-content-between gap-2 py-3");

    const info = el("div", "text-break");
    // 1. Livello
    const livello = el("div", "d-flex flex-wrap align-items-center gap-1 mb-1");
    livello.appendChild(etichetta(a.livello, a.livello === "Amministratore" ? "primary" : "secondary"));
    if (a.utenteId === idCorrente) livello.appendChild(etichetta("tu", "light"));
    if (!a.attivo) livello.appendChild(etichetta("Disattivato", "danger"));
    info.appendChild(livello);
    // 2. Nome e cognome, 3. email
    info.appendChild(nomeCompleto(a)
        ? el("div", "fw-bold fs-5", nomeCompleto(a))
        : el("div", "fs-5 fst-italic text-body-secondary", "Nome non inserito"));
    info.appendChild(el("div", null, a.email));

    const azioni = el("div", "d-flex gap-2 flex-shrink-0 align-self-sm-center");
    if (definizione.livelli.length > 1) {
        azioni.appendChild(pulsante("Modifica", "btn btn-sm btn-light border", () => apri(a)));
    }
    azioni.appendChild(pulsante("Togli accesso", "btn btn-sm btn-danger", () => togli(a)));

    div.append(info, azioni);
    return div;
}

function apri(a) {
    inModifica = a || null;

    // Aggiunta: solo gli utenti che non hanno ancora accesso al modulo.
    const senzaAccesso = utenti.filter(u => !accessi.some(x => x.utenteId === u.id));
    $("sceltaUtente").hidden = !!a;
    $("utenteFisso").hidden = !a;
    if (a) {
        $("nomeUtente").textContent = nomeCompleto(a) || "Nome non inserito";
        $("emailUtente").textContent = a.email;
    } else {
        $("utente").replaceChildren(new Option("Scegli un utente", ""),
            ...senzaAccesso.map(u => new Option(`${nomeCompleto(u) || "Nome non inserito"} — ${u.email}`, u.id)));
        $("aiutoUtente").textContent = senzaAccesso.length
            ? "Compaiono solo gli utenti che non hanno ancora accesso al modulo."
            : "Tutti gli utenti hanno già accesso. Per crearne uno nuovo vai in Admin → Utenti.";
    }

    $("livello").replaceChildren(...definizione.livelli.map(l => new Option(l, l)));
    $("livello").value = a ? a.livello : definizione.livelli[0];
    $("erroreFinestra").textContent = "";

    modulo.apri(a ? "Modifica autorizzazione" : `Aggiungi utente al modulo ${definizione.nome}`);
}

async function salva() {
    const errore = $("erroreFinestra");
    const utenteId = inModifica ? inModifica.utenteId : Number($("utente").value);

    if (!utenteId) {
        errore.textContent = "Scegli un utente.";
        return;
    }

    try {
        const response = await api(`${API}/${utenteId}`, jsonBody("PUT", { livello: $("livello").value }));
        if (!response.ok) {
            errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
            return;
        }
        modulo.chiudi();
        await carica();
    } catch {
        errore.textContent = "Errore nel collegamento al server.";
    }
}

async function togli(a) {
    if (!confirm(`Togliere a ${nomeCompleto(a) || a.email} l'accesso al modulo ${definizione.nome}?`)) return;
    try {
        const response = await api(`${API}/${a.utenteId}`, { method: "DELETE" });
        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'operazione."));
            return;
        }
        await carica();
    } catch {
        alert("Errore nel collegamento al server.");
    }
}

$("nuova").addEventListener("click", () => apri(null));
$("annulla").addEventListener("click", () => modulo.chiudi());
alInvio($("formAccesso"), salva);

avviaPagina({ modulo: MODULI.admin, amministratore: true }).then(async u => {
    idCorrente = u.id;

    const [rm, ru] = await Promise.all([api("/api/moduli"), api("/api/utenti")]);
    definizione = (await rm.json()).find(m => m.codice === codice);
    if (!definizione || definizione.livelli.length === 0) {
        location.href = "/moduli.html";
        return;
    }
    utenti = await ru.json();

    document.title = `Modulo ${definizione.nome}`;
    intestazioneModulo($("intestazioneModulo"), codice);
    $("filtroLivello").replaceChildren(new Option("Livello", ""), ...definizione.livelli.map(l => new Option(l, l)));
    if (definizione.livelli.length < 2) $("filtroLivello").closest(".col-6").hidden = true;

    return carica();
});
