// Pagina La mia squadra del modulo Fantapolitica (fantapolitica-squadra.html): per ogni
// lega di cui faccio parte, la mia squadra (nome e politici). Si crea, si modifica e si
// elimina quando si vuole. I politici si comprano con i crediti per squadra: chi è già in
// squadra resta al prezzo pagato, i nuovi costano quanto indica oggi il Super admin. Componenti comuni: componenti.js e js/fantapolitica-comune.js.

const $ = id => document.getElementById(id);

let politici = [];          // tutti i politici tra cui scegliere
let politiciPerSquadra = 5; // dal server
let creditiPerSquadra = 100; // dal server
let legaAperta = null;      // lega di cui si sta modificando la squadra
let prezziPagati = new Map(); // politici già nella squadra aperta → prezzo pagato

// Quanto costa un politico per la squadra aperta.
const prezzo = p => prezziPagati.has(p.id) ? prezziPagati.get(p.id) : p.costo;

const modulo = moduloFinestra($("finestra"), { primoCampo: $("nome") });

async function caricaSquadre() {
    try {
        const response = await api(`${API_FANTAPOLITICA}/squadre/mie`);
        if (!response.ok) throw new Error();
        const dati = await response.json();
        politiciPerSquadra = dati.politiciPerSquadra;
        creditiPerSquadra = dati.creditiPerSquadra;
        $("spiegazione").textContent =
            `In ogni lega di cui fai parte compri una squadra di ${politiciPerSquadra} politici ` +
            `con ${crediti(creditiPerSquadra)}. Puoi cambiarla quando vuoi.`;
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
    if (s) info.appendChild(el("div", "small text-body-secondary",
        `Spesi ${s.spesi} di ${crediti(creditiPerSquadra)} · ne restano ${creditiPerSquadra - s.spesi}`));
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

// Finestra: nome e caselle dei politici con il prezzo. Si bloccano i politici che non si
// possono più prendere: squadra già completa o crediti che non bastano.
function apri(lega) {
    legaAperta = lega;
    const s = lega.squadra;
    prezziPagati = new Map(s ? s.politici.map(p => [p.id, p.prezzo]) : []);
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
        const destra = el("span", "d-flex flex-column align-items-end gap-1 flex-shrink-0");
        destra.append(etichettaRuolo(p.ruolo), etichettaCrediti(prezzo(p)));
        if (prezziPagati.has(p.id) && prezzo(p) !== p.costo) {
            destra.appendChild(el("span", "small text-body-secondary", `pagato; oggi ${crediti(p.costo)}`));
        }
        voce.append(casella, testo, destra);
        return voce;
    }));
    aggiornaScelta();

    modulo.apri(s ? "Modifica squadra" : "Nuova squadra");
}

const politiciScelti = () =>
    [...document.querySelectorAll('#sceltaPolitici input[name="politico"]:checked')].map(c => Number(c.value));

const spesa = scelti => scelti.reduce((totale, id) => totale + prezzo(politici.find(p => p.id === id)), 0);

function aggiornaScelta() {
    const scelti = politiciScelti();
    const n = scelti.length;
    const restano = creditiPerSquadra - spesa(scelti);
    const completa = n >= politiciPerSquadra;
    for (const c of document.querySelectorAll('#sceltaPolitici input[name="politico"]')) {
        const p = politici.find(x => x.id === Number(c.value));
        c.disabled = !c.checked && (completa || prezzo(p) > restano);
        c.closest("label").classList.toggle("text-body-tertiary", c.disabled);
    }
    $("contaPolitici").textContent = `${n} di ${politiciPerSquadra} · restano ${crediti(restano)}`;
    $("contaPolitici").className = "small " + (restano < 0 ? "text-danger fw-semibold"
        : n === politiciPerSquadra ? "text-success fw-semibold" : "text-body-secondary");
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
    if (spesa(scelti) > creditiPerSquadra) {
        errore.textContent = `Hai superato i crediti: la squadra costa ${spesa(scelti)} su ${creditiPerSquadra}.`;
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
