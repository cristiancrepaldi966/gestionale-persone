// Pagina Leghe del modulo Fantapolitica (fantapolitica.html): tutte le leghe con le
// persone che ne fanno parte. Ognuno può creare una lega, di cui fa sempre parte;
// modifica ed eliminazione solo per chi l'ha creata e per il Super admin
// (il server lo indica con "gestibile"). Componenti e funzioni comuni: componenti.js.

const API = "/api/fantapolitica";
const $ = id => document.getElementById(id);

let utenteCorrente = null;
let utenti = [];        // utenti abilitati alla Fantapolitica, tra cui scegliere i membri
let inModifica = null;  // lega in modifica, null se è nuova

const modulo = moduloFinestra($("finestra"), { primoCampo: $("nome") });

const persone = n => `${n} ${n === 1 ? "persona" : "persone"}`;

// Icona di Bootstrap Icons (solo decorativa).
function icona(nome, classi = "") {
    const i = el("i", `bi bi-${nome} ${classi}`.trim());
    i.setAttribute("aria-hidden", "true");
    return i;
}

async function caricaLeghe() {
    try {
        const response = await api(`${API}/leghe`);
        if (!response.ok) throw new Error();
        mostraLeghe(await response.json());
    } catch {
        $("elencoLeghe").replaceChildren(el("p", "card card-body shadow-sm border-0 mb-0 text-danger",
            "Errore nel caricamento delle leghe."));
    }
}

function mostraLeghe(leghe) {
    $("riepilogo").textContent = `${leghe.length} ${leghe.length === 1 ? "lega" : "leghe"}`;

    if (leghe.length === 0) {
        $("elencoLeghe").replaceChildren(el("p", "card card-body shadow-sm border-0 mb-0 text-body-secondary",
            "Nessuna lega. Usa il pulsante + in alto a destra per crearne una."));
        return;
    }

    $("elencoLeghe").replaceChildren(...leghe.map(schedaLega));
}

function schedaLega(l) {
    const scheda = el("section", "card shadow-sm border-0");
    const corpo = el("div", "card-body");

    const testa = el("div", "d-flex justify-content-between align-items-start gap-3");
    const titolo = el("div", "d-flex align-items-center gap-3 min-w-0");
    titolo.appendChild(iconaCategoria("people", DEFINIZIONE_MODULI.fantapolitica.colore));
    const info = el("div", "min-w-0");
    info.appendChild(el("h3", "h5 fw-bold mb-0 text-break", l.nome));
    info.appendChild(el("div", "small text-body-secondary",
        `${persone(l.membri.length)}${l.creatoDa ? " · creata da " + l.creatoDa.nome : ""}`));
    titolo.appendChild(info);
    testa.appendChild(titolo);

    if (l.gestibile) {
        const azioni = el("div", "d-flex gap-2 flex-shrink-0");
        azioni.appendChild(pulsante("Modifica", "btn btn-sm btn-light border", () => apri(l)));
        azioni.appendChild(pulsante("Elimina", "btn btn-sm btn-danger", () => elimina(l)));
        testa.appendChild(azioni);
    }

    const membri = el("ul", "list-unstyled d-flex flex-wrap gap-2 mt-3 mb-0");
    membri.setAttribute("aria-label", `Persone della lega ${l.nome}`);
    for (const m of l.membri) {
        const li = el("li");
        const creatore = l.creatoDa && m.id === l.creatoDa.id;
        const io = m.id === utenteCorrente.id;
        const voce = el("span", "badge rounded-pill fw-normal fs-6 d-inline-flex align-items-center gap-1 " +
            (io ? "text-bg-primary" : "text-bg-light border"));
        if (creatore) {
            voce.appendChild(icona("star-fill", "small"));
            voce.title = "Ha creato la lega";
        }
        voce.appendChild(el("span", null, m.nome + (io ? " (tu)" : "")));
        li.appendChild(voce);
        membri.appendChild(li);
    }

    corpo.append(testa, membri);
    scheda.appendChild(corpo);
    return scheda;
}

// Finestra: nome e caselle per scegliere le persone. Chi ha creato la lega è sempre
// selezionato e non si può togliere.
function apri(l) {
    inModifica = l || null;
    $("nome").value = l ? l.nome : "";
    $("erroreFinestra").textContent = "";

    const creatoreId = l ? l.creatoDa?.id : utenteCorrente.id;
    const scelti = new Set(l ? l.membri.map(m => m.id) : [creatoreId]);

    // In modifica compaiono anche i membri che nel frattempo non sono più abilitati.
    const elenco = [...utenti];
    if (l) {
        for (const m of l.membri) {
            if (!elenco.some(u => u.id === m.id)) elenco.push(m);
        }
    }

    $("sceltaMembri").replaceChildren(...elenco.map(u => {
        const voce = el("label", "list-group-item d-flex align-items-center gap-2");
        const casella = el("input", "form-check-input m-0");
        Object.assign(casella, { type: "checkbox", value: u.id, name: "membro" });
        casella.checked = scelti.has(u.id);
        const creatore = u.id === creatoreId;
        if (creatore) casella.disabled = true;
        voce.append(casella, el("span", "me-auto", u.nome + (u.id === utenteCorrente.id ? " (tu)" : "")));
        if (creatore) voce.appendChild(el("span", "small text-body-secondary", "ha creato la lega"));
        return voce;
    }));
    aggiornaConteggio();

    modulo.apri(l ? "Modifica lega" : "Nuova lega");
}

const membriScelti = () =>
    [...document.querySelectorAll('#sceltaMembri input[name="membro"]:checked')].map(c => Number(c.value));

function aggiornaConteggio() {
    const n = membriScelti().length;
    $("contaMembri").textContent = n === 1 ? "1 persona scelta" : `${n} persone scelte`;
}

async function salva() {
    const errore = $("erroreFinestra");
    const nome = $("nome").value.trim();

    if (!nome) {
        errore.textContent = "Inserisci il nome della lega.";
        return;
    }

    const dati = { nome, membri: membriScelti() };

    try {
        const response = inModifica
            ? await api(`${API}/leghe/${inModifica.id}`, jsonBody("PUT", dati))
            : await api(`${API}/leghe`, jsonBody("POST", dati));

        if (!response.ok) {
            errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
            return;
        }

        modulo.chiudi();
        await caricaLeghe();
    } catch {
        errore.textContent = "Errore nel collegamento al server.";
    }
}

async function elimina(l) {
    if (!confirm(`Eliminare la lega "${l.nome}"?`)) return;

    try {
        const response = await api(`${API}/leghe/${l.id}`, { method: "DELETE" });
        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'eliminazione."));
            return;
        }
        await caricaLeghe();
    } catch {
        alert("Errore nel collegamento al server.");
    }
}

$("nuova").addEventListener("click", () => apri(null));
$("annulla").addEventListener("click", () => modulo.chiudi());
$("sceltaMembri").addEventListener("change", aggiornaConteggio);
alInvio($("formLega"), salva);

// Modulo Fantapolitica, qualsiasi livello.
avviaPagina({ modulo: MODULI.fantapolitica })
    .then(async utente => {
        utenteCorrente = utente;
        try {
            const response = await api(`${API}/utenti`);
            if (response.ok) utenti = await response.json();
        } catch { }
    })
    .then(caricaLeghe);
