// Pagina Eventi del modulo Fantapolitica (fantapolitica-eventi.html): listino dei bonus e
// malus ed elenco degli eventi, visibili a tutti. Solo il Super admin li aggiunge,
// modifica ed elimina. Componenti comuni: componenti.js e js/fantapolitica-comune.js.

const $ = id => document.getElementById(id);

let superAdmin = false;
let voci = [];
let politici = [];
let eventoInModifica = null;
let voceInModifica = null;

const finestraEvento = moduloFinestra($("finestraEvento"), { primoCampo: $("voce") });
const finestraVoce = moduloFinestra($("finestraVoce"), { primoCampo: $("nomeVoce") });

const dataLunga = new Intl.DateTimeFormat("it-IT", { weekday: "long", day: "numeric", month: "long", year: "numeric" });
const daIso = iso => { const [a, m, g] = iso.split("-").map(Number); return new Date(a, m - 1, g); };

function pulsanteIcona(icona, titolo, classi, azione) {
    const b = pulsante("", `btn btn-sm ${classi}`, azione);
    b.title = titolo;
    b.setAttribute("aria-label", titolo);
    b.appendChild(iconaBi(icona));
    return b;
}

// ---------------------------------------------------------------------------
// Listino
// ---------------------------------------------------------------------------

async function caricaVoci() {
    const response = await api(`${API_FANTAPOLITICA}/voci`);
    if (!response.ok) throw new Error();
    voci = await response.json();
    $("contaVoci").textContent = `${voci.length} ${voci.length === 1 ? "voce" : "voci"}`;
    $("listaVoci").replaceChildren(...voci.map(rigaVoce));
}

function rigaVoce(v) {
    const riga = el("div", "list-group-item d-flex justify-content-between align-items-center gap-2");
    const info = el("div", "d-flex align-items-center gap-2 flex-wrap min-w-0");
    info.append(etichettaPunti(v.punti), el("span", "text-break", v.nome));
    if (superAdmin) info.appendChild(el("span", "small text-body-secondary",
        v.eventi === 0 ? "mai usata" : v.eventi === 1 ? "1 evento" : `${v.eventi} eventi`));
    riga.appendChild(info);
    if (superAdmin) {
        const azioni = el("div", "d-flex gap-1 flex-shrink-0");
        azioni.append(pulsanteIcona("pencil", `Modifica ${v.nome}`, "btn-light border", () => apriVoce(v)));
        azioni.append(pulsanteIcona("trash", `Elimina ${v.nome}`, "btn-outline-danger", () => eliminaVoce(v)));
        riga.appendChild(azioni);
    }
    return riga;
}

function apriVoce(v) {
    voceInModifica = v || null;
    $("nomeVoce").value = v ? v.nome : "";
    $("puntiVoce").value = v ? v.punti : "";
    $("erroreVoce").textContent = "";
    finestraVoce.apri(v ? "Modifica voce" : "Nuova voce");
}

async function salvaVoce() {
    const errore = $("erroreVoce");
    const dati = { nome: $("nomeVoce").value.trim(), punti: Number($("puntiVoce").value) };
    if (!dati.nome) { errore.textContent = "Inserisci il nome della voce."; return; }
    if ($("puntiVoce").value === "" || !Number.isInteger(dati.punti)) {
        errore.textContent = "Inserisci i punti (un numero intero, negativo per i malus).";
        return;
    }
    try {
        const response = voceInModifica
            ? await api(`${API_FANTAPOLITICA}/voci/${voceInModifica.id}`, jsonBody("PUT", dati))
            : await api(`${API_FANTAPOLITICA}/voci`, jsonBody("POST", dati));
        if (!response.ok) {
            errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
            return;
        }
        finestraVoce.chiudi();
        await caricaVoci();
        await caricaEventi();
    } catch {
        errore.textContent = "Errore nel collegamento al server.";
    }
}

async function eliminaVoce(v) {
    if (!confirm(`Eliminare la voce "${v.nome}"?`)) return;
    try {
        const response = await api(`${API_FANTAPOLITICA}/voci/${v.id}`, { method: "DELETE" });
        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'eliminazione."));
            return;
        }
        await caricaVoci();
    } catch {
        alert("Errore nel collegamento al server.");
    }
}

// ---------------------------------------------------------------------------
// Eventi
// ---------------------------------------------------------------------------

async function caricaEventi() {
    try {
        const response = await api(`${API_FANTAPOLITICA}/eventi`);
        if (!response.ok) throw new Error();
        mostraEventi(await response.json());
    } catch {
        $("elencoEventi").replaceChildren(el("p", "card card-body shadow-sm border-0 mb-0 text-danger",
            "Errore nel caricamento degli eventi."));
    }
}

// Un riquadro per giorno, dal più recente.
function mostraEventi(eventi) {
    $("riepilogo").textContent = `${eventi.length} ${eventi.length === 1 ? "evento" : "eventi"}`;
    if (eventi.length === 0) {
        $("elencoEventi").replaceChildren(el("p", "card card-body shadow-sm border-0 mb-0 text-body-secondary",
            superAdmin ? "Nessun evento. Usa il pulsante + in alto a destra per registrarne uno."
                       : "Nessun evento registrato per ora."));
        return;
    }

    const giorni = new Map();
    for (const e of eventi) {
        if (!giorni.has(e.data)) giorni.set(e.data, []);
        giorni.get(e.data).push(e);
    }

    $("elencoEventi").replaceChildren(...[...giorni].map(([data, delGiorno]) => {
        const scheda = el("section", "card shadow-sm border-0");
        const titolo = el("h3", "card-header bg-body h6 fw-semibold mb-0 py-2 text-capitalize", dataLunga.format(daIso(data)));
        const lista = el("div", "list-group list-group-flush");
        lista.append(...delGiorno.map(rigaEvento));
        scheda.append(titolo, lista);
        return scheda;
    }));
}

function rigaEvento(e) {
    const riga = el("div", "list-group-item py-3");
    const testa = el("div", "d-flex justify-content-between align-items-start gap-2");
    const info = el("div", "min-w-0");
    const titolo = el("div", "d-flex align-items-center gap-2 flex-wrap");
    titolo.append(etichettaPunti(e.punti), el("span", "fw-semibold text-break", e.voce));
    info.appendChild(titolo);
    if (e.descrizione) info.appendChild(el("div", "text-body-secondary small mt-1 text-break", e.descrizione));
    testa.appendChild(info);

    if (superAdmin) {
        const azioni = el("div", "d-flex gap-1 flex-shrink-0");
        azioni.append(pulsanteIcona("pencil", "Modifica evento", "btn-light border", () => apriEvento(e)));
        azioni.append(pulsanteIcona("trash", "Elimina evento", "btn-outline-danger", () => eliminaEvento(e)));
        testa.appendChild(azioni);
    }

    const coinvolti = el("ul", "list-unstyled d-flex flex-wrap gap-1 mt-2 mb-0");
    coinvolti.setAttribute("aria-label", "Politici coinvolti");
    for (const p of e.politici) {
        const li = el("li");
        li.appendChild(el("span", "badge rounded-pill text-bg-light border fw-normal", nomePolitico(p)));
        coinvolti.appendChild(li);
    }

    riga.append(testa, coinvolti);
    return riga;
}

function apriEvento(e) {
    eventoInModifica = e || null;
    $("data").value = e ? e.data : oggi();
    $("data").max = oggi();
    $("voce").replaceChildren(...voci.map(v => new Option(`${v.nome} (${conSegno(v.punti)})`, v.id)));
    $("voce").value = e ? e.vocePunteggioId : (voci[0] ? voci[0].id : "");
    $("descrizione").value = e ? e.descrizione || "" : "";
    $("erroreEvento").textContent = "";

    const scelti = new Set(e ? e.politici.map(p => p.id) : []);
    $("sceltaPoliticiEvento").replaceChildren(...politici.map(p => {
        const voce = el("label", "list-group-item d-flex align-items-center gap-2");
        const casella = el("input", "form-check-input m-0 flex-shrink-0");
        Object.assign(casella, { type: "checkbox", value: p.id, name: "politicoEvento" });
        casella.checked = scelti.has(p.id);
        voce.append(casella, el("span", "me-auto", nomePolitico(p)), etichettaRuolo(p.ruolo));
        return voce;
    }));
    aggiornaContaEvento();

    finestraEvento.apri(e ? "Modifica evento" : "Nuovo evento");
}

const politiciEvento = () =>
    [...document.querySelectorAll('#sceltaPoliticiEvento input[name="politicoEvento"]:checked')].map(c => Number(c.value));

function aggiornaContaEvento() {
    const n = politiciEvento().length;
    $("contaPoliticiEvento").textContent = n === 1 ? "1 scelto" : `${n} scelti`;
}

async function salvaEvento() {
    const errore = $("erroreEvento");
    const dati = {
        data: $("data").value,
        vocePunteggioId: Number($("voce").value) || null,
        descrizione: $("descrizione").value.trim() || null,
        politici: politiciEvento()
    };
    if (!dati.data) { errore.textContent = "Inserisci la data."; return; }
    if (!dati.vocePunteggioId) { errore.textContent = "Scegli una voce del listino."; return; }
    if (dati.politici.length === 0) { errore.textContent = "Scegli almeno un politico."; return; }

    try {
        const response = eventoInModifica
            ? await api(`${API_FANTAPOLITICA}/eventi/${eventoInModifica.id}`, jsonBody("PUT", dati))
            : await api(`${API_FANTAPOLITICA}/eventi`, jsonBody("POST", dati));
        if (!response.ok) {
            errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
            return;
        }
        finestraEvento.chiudi();
        await Promise.all([caricaEventi(), caricaVoci()]);
    } catch {
        errore.textContent = "Errore nel collegamento al server.";
    }
}

async function eliminaEvento(e) {
    if (!confirm(`Eliminare l'evento "${e.voce}" del ${formatoData(e.data)}? I punti verranno tolti.`)) return;
    try {
        const response = await api(`${API_FANTAPOLITICA}/eventi/${e.id}`, { method: "DELETE" });
        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'eliminazione."));
            return;
        }
        await Promise.all([caricaEventi(), caricaVoci()]);
    } catch {
        alert("Errore nel collegamento al server.");
    }
}

$("nuovoEvento").addEventListener("click", () => apriEvento(null));
$("annullaEvento").addEventListener("click", () => finestraEvento.chiudi());
$("sceltaPoliticiEvento").addEventListener("change", aggiornaContaEvento);
alInvio($("formEvento"), salvaEvento);

$("nuovaVoce").addEventListener("click", () => apriVoce(null));
$("annullaVoce").addEventListener("click", () => finestraVoce.chiudi());
alInvio($("formVoce"), salvaVoce);

// Modulo Fantapolitica, qualsiasi livello; gestione solo per il Super admin.
avviaPagina({ modulo: MODULI.fantapolitica }).then(async utente => {
    superAdmin = !!utente.superAdmin;
    $("nuovoEvento").hidden = !superAdmin;
    $("azioniListino").hidden = !superAdmin;
    if (superAdmin) $("listino").open = true;
    try {
        const [, risposta] = await Promise.all([caricaVoci(), api(`${API_FANTAPOLITICA}/politici`)]);
        if (risposta.ok) politici = await risposta.json();
    } catch {
        $("listaVoci").replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento del listino."));
    }
    await caricaEventi();
});
