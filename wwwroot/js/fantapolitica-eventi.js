// Pagina Eventi del modulo Fantapolitica (fantapolitica-eventi.html): listino dei bonus e
// malus ed elenco degli eventi con le pagelle (voto da 1 a 10 o s.v., più bonus e malus
// per ogni politico), visibili a tutti. Solo il Super admin li aggiunge,
// modifica ed elimina. Componenti comuni: componenti.js e js/fantapolitica-comune.js.

const $ = id => document.getElementById(id);

let superAdmin = false;
let voci = [];
let politici = [];
let eventoInModifica = null;
let voceInModifica = null;

let regole = { votoMinimo: 1, votoMassimo: 10, votoProposto: 6 };

const finestraEvento = moduloFinestra($("finestraEvento"), { primoCampo: $("titolo") });
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

// Un riquadro per evento, dal più recente.
function mostraEventi(eventi) {
    $("riepilogo").textContent = `${eventi.length} ${eventi.length === 1 ? "evento" : "eventi"}`;
    if (eventi.length === 0) {
        $("elencoEventi").replaceChildren(el("p", "card card-body shadow-sm border-0 mb-0 text-body-secondary",
            superAdmin ? "Nessun evento. Usa il pulsante + in alto a destra per registrarne uno."
                       : "Nessun evento registrato per ora."));
        return;
    }
    $("elencoEventi").replaceChildren(...eventi.map(schedaEvento));
}

function schedaEvento(e) {
    const scheda = el("section", "card shadow-sm border-0");
    const testa = el("div", "card-body d-flex justify-content-between align-items-start gap-2 pb-2");
    const info = el("div", "min-w-0");
    info.append(el("div", "small text-body-secondary text-capitalize", dataLunga.format(daIso(e.data))),
        el("h3", "h5 fw-bold mb-0 text-break", e.titolo));
    if (e.descrizione) info.appendChild(el("div", "small text-body-secondary mt-1 text-break", e.descrizione));
    testa.appendChild(info);
    if (superAdmin) {
        const azioni = el("div", "d-flex gap-1 flex-shrink-0");
        azioni.append(pulsanteIcona("pencil", "Modifica evento", "btn-light border", () => apriEvento(e)));
        azioni.append(pulsanteIcona("trash", "Elimina evento", "btn-outline-danger", () => eliminaEvento(e)));
        testa.appendChild(azioni);
    }

    // Pagelle: chi ha un voto o un bonus/malus, dal punteggio più alto (ordine dal server).
    const pagelle = el("ul", "list-group list-group-flush border-top");
    pagelle.setAttribute("aria-label", `Pagelle: ${e.titolo}`);
    for (const p of e.politici) {
        const li = el("li", "list-group-item d-flex align-items-center gap-2 flex-wrap py-2");
        const voto = el("span", "voto-pagella" + (p.voto == null ? " senza-voto" : ""), testoVoto(p.voto));
        voto.title = p.voto == null ? "Senza voto" : "Voto";
        li.append(voto, el("span", "me-auto text-break", nomePolitico(p)));
        for (const b of p.bonus) {
            li.appendChild(el("span", `badge rounded-pill fw-normal text-bg-${b.punti >= 0 ? "success" : "danger"} bg-opacity-75`,
                `${b.voce} ${conSegno(b.punti)}`));
        }
        li.appendChild(etichettaPunti(p.punti));
        pagelle.appendChild(li);
    }
    const senzaVoto = politici.length - e.politici.length;
    scheda.append(testa, pagelle);
    if (senzaVoto > 0) scheda.appendChild(el("div", "card-footer bg-body small text-body-secondary",
        senzaVoto === 1 ? "1 altro politico senza voto." : `${senzaVoto} altri politici senza voto.`));
    return scheda;
}

// ---------------------------------------------------------------------------
// Finestra dell'evento: una riga per politico con voto, bonus e malus, totale
// ---------------------------------------------------------------------------

// Voti possibili: s.v., poi da 1 a 10 di mezzo in mezzo.
function opzioniVoto() {
    const opzioni = [new Option("s.v.", "")];
    for (let v = regole.votoMinimo; v <= regole.votoMassimo; v += 0.5) opzioni.push(new Option(numero.format(v), v));
    return opzioni;
}

function rigaPagella(p, pagella) {
    const riga = el("div", "list-group-item py-2");
    riga.dataset.politico = p.id;

    const sopra = el("div", "d-flex align-items-center gap-2 flex-wrap");
    const nome = el("span", "me-auto min-w-0 d-flex align-items-center gap-2 flex-wrap");
    nome.append(el("span", "fw-semibold text-break", nomePolitico(p)), etichettaRuolo(p.ruolo));

    const voto = el("select", "form-select form-select-sm campo-voto");
    voto.setAttribute("aria-label", `Voto di ${nomePolitico(p)}`);
    voto.append(...opzioniVoto());
    voto.value = pagella ? (pagella.voto == null ? "" : String(pagella.voto)) : String(regole.votoProposto);

    const totale = el("span", "totale-pagella");
    sopra.append(nome, voto, totale);

    // Bonus e malus: etichette con la x per toglierle, più una tendina per aggiungerne.
    const sotto = el("div", "d-flex align-items-center gap-1 flex-wrap mt-1 bonus-pagella");
    const aggiungi = el("select", "form-select form-select-sm w-auto");
    aggiungi.setAttribute("aria-label", `Aggiungi bonus o malus a ${nomePolitico(p)}`);
    aggiungi.append(new Option("+ bonus / malus", ""), ...voci.map(v => new Option(`${v.nome} (${conSegno(v.punti)})`, v.id)));
    aggiungi.addEventListener("change", () => {
        if (aggiungi.value) sotto.insertBefore(etichettaBonus(Number(aggiungi.value)), aggiungi);
        aggiungi.value = "";
        aggiornaTotale(riga);
    });
    for (const b of pagella ? pagella.bonus : []) sotto.appendChild(etichettaBonus(b.vocePunteggioId));
    sotto.appendChild(aggiungi);

    voto.addEventListener("change", () => aggiornaTotale(riga));
    riga.append(sopra, sotto);
    aggiornaTotale(riga);
    return riga;
}

function etichettaBonus(voceId) {
    const v = voci.find(x => x.id === voceId) || { nome: "Voce eliminata", punti: 0 };
    const e = el("span", `badge rounded-pill fw-normal d-inline-flex align-items-center gap-1 text-bg-${v.punti >= 0 ? "success" : "danger"}`);
    e.dataset.voce = voceId;
    e.dataset.punti = v.punti;
    e.appendChild(document.createTextNode(`${v.nome} ${conSegno(v.punti)}`));
    const togli = el("button", "btn-close btn-close-white btn-sm ms-1");
    togli.type = "button";
    togli.setAttribute("aria-label", `Togli ${v.nome}`);
    togli.addEventListener("click", () => {
        const riga = e.closest("[data-politico]");
        e.remove();
        aggiornaTotale(riga);
    });
    e.appendChild(togli);
    return e;
}

// Voto + bonus e malus, come fa il server.
function letturaPagella(riga) {
    const valore = riga.querySelector(".campo-voto").value;
    const bonus = [...riga.querySelectorAll("[data-voce]")];
    return {
        politicoId: Number(riga.dataset.politico),
        voto: valore === "" ? null : Number(valore),
        bonus: bonus.map(b => Number(b.dataset.voce)),
        punti: (valore === "" ? 0 : Number(valore)) + bonus.reduce((t, b) => t + Number(b.dataset.punti), 0)
    };
}

function aggiornaTotale(riga) {
    const p = letturaPagella(riga);
    const vuota = p.voto == null && p.bonus.length === 0;
    riga.querySelector(".totale-pagella").replaceChildren(vuota
        ? el("span", "small text-body-secondary", "nessun punto")
        : etichettaPunti(p.punti));
}

function apriEvento(e) {
    eventoInModifica = e || null;
    $("data").value = e ? e.data : oggi();
    $("data").max = oggi();
    $("titolo").value = e ? e.titolo : "";
    $("descrizione").value = e ? e.descrizione || "" : "";
    $("erroreEvento").textContent = "";

    // In modifica: chi non ha la pagella è senza voto.
    $("pagelle").replaceChildren(...politici.map(p => {
        const pagella = e ? (e.politici.find(x => x.politicoId === p.id) || { voto: null, bonus: [] }) : null;
        return rigaPagella(p, pagella);
    }));

    finestraEvento.apri(e ? "Modifica evento" : "Nuovo evento");
}

function impostaTutti(valore) {
    for (const riga of $("pagelle").querySelectorAll("[data-politico]")) {
        riga.querySelector(".campo-voto").value = valore;
        aggiornaTotale(riga);
    }
}

async function salvaEvento() {
    const errore = $("erroreEvento");
    const pagelle = [...$("pagelle").querySelectorAll("[data-politico]")].map(letturaPagella)
        .filter(p => p.voto != null || p.bonus.length > 0)
        .map(({ politicoId, voto, bonus }) => ({ politicoId, voto, bonus }));
    const dati = {
        data: $("data").value,
        titolo: $("titolo").value.trim(),
        descrizione: $("descrizione").value.trim() || null,
        politici: pagelle
    };
    if (!dati.data) { errore.textContent = "Inserisci la data."; return; }
    if (!dati.titolo) { errore.textContent = "Inserisci il titolo dell'evento."; return; }
    if (pagelle.length === 0) { errore.textContent = "Dai almeno un voto o un bonus/malus."; return; }

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
    if (!confirm(`Eliminare l'evento "${e.titolo}" del ${formatoData(e.data)}? I punti verranno tolti.`)) return;
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
$("tuttiProposto").addEventListener("click", () => impostaTutti(String(regole.votoProposto)));
$("tuttiSv").addEventListener("click", () => impostaTutti(""));
alInvio($("formEvento"), salvaEvento);

$("nuovaVoce").addEventListener("click", () => apriVoce(null));
$("annullaVoce").addEventListener("click", () => finestraVoce.chiudi());
alInvio($("formVoce"), salvaVoce);

// Modulo Fantapolitica, qualsiasi livello; gestione solo per il Super admin.
avviaPagina({ modulo: MODULI.fantapolitica }).then(async utente => {
    superAdmin = !!utente.superAdmin;
    $("nuovoEvento").hidden = !superAdmin;
    $("azioniListino").hidden = !superAdmin;
    try {
        const [, rPolitici, rRegole] = await Promise.all([caricaVoci(),
            api(`${API_FANTAPOLITICA}/politici`), api(`${API_FANTAPOLITICA}/eventi/regole`)]);
        if (rPolitici.ok) politici = await rPolitici.json();
        if (rRegole.ok) regole = await rRegole.json();
    } catch {
        $("listaVoci").replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento del listino."));
    }
    $("tuttiProposto").textContent = `Tutti a ${numero.format(regole.votoProposto)}`;
    await caricaEventi();
});
