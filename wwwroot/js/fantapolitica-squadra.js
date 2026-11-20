// Pagina La mia squadra del modulo Fantapolitica (fantapolitica-squadra.html): per ogni
// lega di cui faccio parte, la mia squadra (nome e politici). Si crea, si modifica e si
// elimina quando si vuole. Componenti comuni: componenti.js e js/fantapolitica-comune.js.

const $ = id => document.getElementById(id);

let politici = [];          // tutti i politici tra cui scegliere
let politiciPerSquadra = 5; // dal server
let legaAperta = null;      // lega di cui si sta modificando la squadra

const modulo = moduloFinestra($("finestra"), { primoCampo: $("nome") });

async function caricaSquadre() {
    try {
        const response = await api(`${API_FANTAPOLITICA}/squadre/mie`);
        if (!response.ok) throw new Error();
        const dati = await response.json();
        politiciPerSquadra = dati.politiciPerSquadra;
        $("spiegazione").textContent =
            `In ogni lega di cui fai parte hai una squadra di ${politiciPerSquadra} politici. Puoi cambiarla quando vuoi.`;
        mostraSquadre(dati.leghe);
    } catch {
        $("elencoSquadre").replaceChildren(el("p", "card card-body shadow-sm border-0 mb-0 text-danger",
            "Errore nel caricamento delle squadre."));
    }
}

function mostraSquadre(leghe) {
    if (leghe.length === 0) {
        const vuoto = el("p", "card card-body shadow-sm border-0 mb-0 text-body-secondary");
        vuoto.append(document.createTextNode("Non fai ancora parte di nessuna lega. "));
        const link = el("a", null, "Crea una lega");
        link.href = "/fantapolitica.html";
        vuoto.append(link, document.createTextNode(" o chiedi di essere aggiunto a una."));
        $("elencoSquadre").replaceChildren(vuoto);
        return;
    }
    $("elencoSquadre").replaceChildren(...leghe.map(schedaSquadra));
}

function schedaSquadra(lega) {
    const s = lega.squadra;
    const scheda = el("section", "card shadow-sm border-0");
    const corpo = el("div", "card-body");

    const testa = el("div", "d-flex justify-content-between align-items-start gap-3 flex-wrap");
    const titolo = el("div", "d-flex align-items-center gap-3 min-w-0");
    titolo.appendChild(iconaCategoria(s ? "person-check" : "person-plus", s ? DEFINIZIONE_MODULI.fantapolitica.colore : "#6c757d"));
    const info = el("div", "min-w-0");
    info.appendChild(el("div", "small text-body-secondary", titoloLega(lega.nome)));
    info.appendChild(el("h3", "h5 fw-bold mb-0 text-break", s ? s.nome : "Nessuna squadra"));
    titolo.appendChild(info);
    testa.appendChild(titolo);

    const azioni = el("div", "d-flex gap-2 flex-shrink-0");
    if (s) {
        azioni.appendChild(pulsante("Modifica", "btn btn-sm btn-light border", () => apri(lega)));
        azioni.appendChild(pulsante("Elimina", "btn btn-sm btn-danger", () => elimina(lega)));
    } else {
        azioni.appendChild(pulsante("Crea la squadra", "btn btn-sm btn-primary", () => apri(lega)));
    }
    testa.appendChild(azioni);
    corpo.appendChild(testa);

    if (s) {
        const lista = elencoPoliticiSquadra(s.politici);
        lista.classList.add("mt-3");
        corpo.appendChild(lista);
        if (s.politici.length < politiciPerSquadra) {
            corpo.appendChild(el("div", "small text-danger mt-2",
                `Squadra incompleta: mancano ${politiciPerSquadra - s.politici.length} politici (qualcuno è stato tolto dall'elenco).`));
        }
    }

    scheda.appendChild(corpo);
    return scheda;
}

// Finestra: nome e caselle dei politici; arrivati al numero richiesto le altre si bloccano.
function apri(lega) {
    legaAperta = lega;
    const s = lega.squadra;
    $("nomeLega").textContent = titoloLega(lega.nome);
    $("nome").value = s ? s.nome : "";
    $("erroreFinestra").textContent = "";

    const scelti = new Set(s ? s.politici.map(p => p.id) : []);
    $("sceltaPolitici").replaceChildren(...politici.map(p => {
        const voce = el("label", "list-group-item d-flex align-items-center gap-2");
        const casella = el("input", "form-check-input m-0 flex-shrink-0");
        Object.assign(casella, { type: "checkbox", value: p.id, name: "politico" });
        casella.checked = scelti.has(p.id);
        const testo = el("span", "me-auto min-w-0");
        testo.append(el("span", "d-block", nomePolitico(p)));
        if (p.partito) testo.append(el("span", "d-block small text-body-secondary", p.partito));
        voce.append(casella, testo, etichettaRuolo(p.ruolo));
        return voce;
    }));
    aggiornaScelta();

    modulo.apri(s ? "Modifica squadra" : "Nuova squadra");
}

const politiciScelti = () =>
    [...document.querySelectorAll('#sceltaPolitici input[name="politico"]:checked')].map(c => Number(c.value));

function aggiornaScelta() {
    const n = politiciScelti().length;
    const completa = n >= politiciPerSquadra;
    for (const c of document.querySelectorAll('#sceltaPolitici input[name="politico"]')) {
        c.disabled = completa && !c.checked;
        c.closest("label").classList.toggle("text-body-tertiary", c.disabled);
    }
    $("contaPolitici").textContent = `${n} di ${politiciPerSquadra} scelti`;
    $("contaPolitici").className = "small " + (n === politiciPerSquadra ? "text-success fw-semibold" : "text-body-secondary");
}

async function salva() {
    const errore = $("erroreFinestra");
    const nome = $("nome").value.trim();
    const scelti = politiciScelti();

    if (!nome) {
        errore.textContent = "Inserisci il nome della squadra.";
        return;
    }
    if (scelti.length !== politiciPerSquadra) {
        errore.textContent = `Scegli ${politiciPerSquadra} politici (ne hai scelti ${scelti.length}).`;
        return;
    }

    try {
        const response = await api(`${API_FANTAPOLITICA}/leghe/${legaAperta.id}/squadra`,
            jsonBody("PUT", { nome, politici: scelti }));
        if (!response.ok) {
            errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
            return;
        }
        modulo.chiudi();
        await caricaSquadre();
    } catch {
        errore.textContent = "Errore nel collegamento al server.";
    }
}

async function elimina(lega) {
    if (!confirm(`Eliminare la squadra "${lega.squadra.nome}" (${titoloLega(lega.nome)})?`)) return;

    try {
        const response = await api(`${API_FANTAPOLITICA}/leghe/${lega.id}/squadra`, { method: "DELETE" });
        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'eliminazione."));
            return;
        }
        await caricaSquadre();
    } catch {
        alert("Errore nel collegamento al server.");
    }
}

$("annulla").addEventListener("click", () => modulo.chiudi());
$("sceltaPolitici").addEventListener("change", aggiornaScelta);
alInvio($("formSquadra"), salva);

// Modulo Fantapolitica, qualsiasi livello.
avviaPagina({ modulo: MODULI.fantapolitica })
    .then(async () => {
        try {
            const response = await api(`${API_FANTAPOLITICA}/politici`);
            if (response.ok) politici = await response.json();
        } catch { }
    })
    .then(caricaSquadre);
