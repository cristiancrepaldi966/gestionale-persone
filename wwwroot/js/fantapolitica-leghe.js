// Pagina Leghe del modulo Fantapolitica (fantapolitica.html): tutte le leghe con le
// persone che ne fanno parte, le loro squadre e la classifica (ordine e posizioni dal server). Ognuno può creare una lega, di cui fa
// sempre parte; modifica ed eliminazione solo per chi l'ha creata e per il Super admin
// (il server lo indica con "gestibile").
// Componenti comuni: componenti.js e js/fantapolitica-comune.js.

const API = API_FANTAPOLITICA;
const $ = id => document.getElementById(id);

let utenteCorrente = null;
let utenti = [];        // utenti abilitati alla Fantapolitica, tra cui scegliere i membri
let inModifica = null;  // lega in modifica, null se è nuova

const modulo = moduloFinestra($("finestra"), { primoCampo: $("nome") });

const persone = n => `${n} ${n === 1 ? "persona" : "persone"}`;

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

    corpo.appendChild(testa);
    scheda.appendChild(corpo);

    // Una riga per persona con la sua squadra: toccandola si vedono i politici.
    const membri = el("ul", "list-group list-group-flush border-top");
    membri.setAttribute("aria-label", `Classifica della lega ${l.nome}`);
    membri.append(...l.membri.map(m => rigaMembro(l, m)));
    scheda.appendChild(membri);
    return scheda;
}

// Riga della classifica: posizione, persona, squadra e punti. Toccandola (se ha una
// squadra) si vedono i politici con i punti che hanno portato.
function rigaMembro(l, m) {
    const li = el("li", "list-group-item px-3 py-2");
    const creatore = l.creatoDa && m.id === l.creatoDa.id;
    const io = m.id === utenteCorrente.id;

    const posizione = el("span", "posizione-classifica" + (m.posizione <= 3 && m.squadra ? ` podio-${m.posizione}` : ""),
        String(m.posizione));
    posizione.title = `${m.posizione}° posto`;

    const persona = el("span", "d-flex align-items-center gap-1 min-w-0" + (io ? " fw-semibold text-primary" : ""));
    if (creatore) {
        persona.title = "Ha creato la lega";
        persona.appendChild(iconaBi("star-fill", "small text-warning"));
    }
    persona.appendChild(el("span", "text-break", m.nome + (io ? " (tu)" : "")));

    const centro = el("span", "d-flex flex-column min-w-0 me-auto");
    centro.appendChild(persona);
    const sotto = el("span", "small text-body-secondary d-flex align-items-center gap-1 flex-wrap");
    if (m.squadra) {
        sotto.append(iconaBi("shield-fill"), el("span", "fw-semibold", m.squadra.nome),
            el("span", null, `· ${crediti(m.squadra.spesi)} spesi`));
    } else {
        sotto.appendChild(el("span", "fst-italic", "nessuna squadra"));
        if (io) {
            const link = el("a", null, "Crea la tua squadra");
            link.href = "/fantapolitica-squadra.html";
            sotto.append(document.createTextNode("·"), link);
        }
    }
    centro.appendChild(sotto);

    const riga = [posizione, centro, etichettaPunti(m.punti)];

    if (!m.squadra) {
        const contenitore = el("div", "d-flex align-items-center gap-3");
        contenitore.append(...riga);
        li.appendChild(contenitore);
        return li;
    }

    const dettagli = el("details", "squadra-lega");
    const sommario = el("summary", "d-flex align-items-center gap-3");
    sommario.append(...riga);
    const corpo = el("div", "mt-2 small dettaglio-squadra");
    corpo.appendChild(elencoPoliticiSquadra(m.squadra.politici));
    dettagli.append(sommario, corpo);
    li.appendChild(dettagli);
    return li;
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
