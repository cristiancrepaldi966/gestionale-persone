// Scheda Categorie del modulo Uscite (categorie.html; Admin → Moduli → Uscite). Componenti e funzioni comuni: componenti.js.

const API = "/api/categorie";
let categorie = [];
let inModifica = null; // categoria da rinominare, null se è nuova

const $ = id => document.getElementById(id);

const ricerca = pannelloRicerca({
    bottone: $("cerca"),
    pannello: $("pannelloFiltri"),
    pallino: $("pallinoFiltri"),
    azzera: $("azzera"),
    campi: [$("filtroNome")],
    applica: () => mostraCategorie()
});

const modulo = moduloFinestra($("finestra"), { primoCampo: $("nome") });

let aspetto = { icone: [], colori: [] };   // icone e colori ammessi (dal server)

const scelto = nome => (document.querySelector(`input[name="${nome}"]:checked`) || {}).value;

// Griglia di icone e campioni di colore (pulsanti radio di Bootstrap).
function preparaScelte() {
    $("sceltaIcone").replaceChildren(...aspetto.icone.flatMap(icona => {
        const input = el("input", "btn-check");
        Object.assign(input, { type: "radio", name: "icona", id: "icona-" + icona, value: icona });
        const label = el("label", "btn btn-outline-secondary");
        label.htmlFor = input.id;
        label.title = icona;
        label.appendChild(el("i", "bi bi-" + icona));
        label.appendChild(el("span", "visually-hidden", icona));
        return [input, label];
    }));

    $("sceltaColori").replaceChildren(...aspetto.colori.map((colore, i) => {
        const span = el("span");
        const input = el("input", "btn-check");
        Object.assign(input, { type: "radio", name: "colore", id: "colore-" + i, value: colore });
        const label = el("label", "campione-scelta");
        label.htmlFor = input.id;
        label.title = colore;
        label.style.setProperty("--colore", colore);
        label.appendChild(el("span", "visually-hidden", "Colore " + (i + 1)));
        span.append(input, label);
        return span;
    }));

    $("finestra").addEventListener("change", aggiornaAnteprima);
}

function aggiornaAnteprima() {
    $("anteprima").replaceChildren(iconaCategoria(scelto("icona") || "tag", scelto("colore") || "#6c757d"));
}

function seleziona(nome, valore) {
    const input = document.querySelector(`input[name="${nome}"][value="${valore}"]`);
    if (input) input.checked = true;
}

async function caricaCategorie() {
    try {
        const response = await api(API);
        if (!response.ok) throw new Error();
        categorie = await response.json();
        mostraCategorie();
    } catch {
        $("listaCategorie").replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento delle categorie."));
    }
}

// Elenco filtrato per nome (la ricerca avviene qui, le categorie sono poche).
function mostraCategorie() {

    const testo = ricerca.valori().filtroNome.toLocaleLowerCase("it-IT");
    const trovate = categorie.filter(c => c.nome.toLocaleLowerCase("it-IT").includes(testo));
    const lista = $("listaCategorie");

    $("riepilogo").replaceChildren(
        el("span", "text-body-secondary", `${trovate.length} ${trovate.length === 1 ? "categoria" : "categorie"}`));

    if (trovate.length === 0) {
        lista.replaceChildren(el("p", "p-3 mb-0 text-body-secondary", ricerca.attivi()
            ? "Nessuna categoria corrisponde alla ricerca."
            : "Nessuna categoria. Usa il pulsante + in alto a destra."));
        return;
    }

    lista.replaceChildren(...trovate.map(rigaCategoria));
}

function rigaCategoria(c) {

    const riga = coloraRiga(
        el("div", "list-group-item riga d-flex justify-content-between align-items-center gap-3 py-3"), c.colore);

    const sinistra = el("div", "d-flex align-items-center gap-3");
    sinistra.appendChild(iconaCategoria(c.icona, c.colore));

    const info = el("div", "text-break");
    sinistra.appendChild(info);
    info.appendChild(el("div", "fw-bold fs-5", c.nome));
    info.appendChild(el("div", "small text-body-secondary",
        c.numeroUscite === 0 ? "Nessuna uscita" :
        c.numeroUscite === 1 ? "1 uscita" : `${c.numeroUscite} uscite`));

    const azioni = el("div", "d-flex gap-2 flex-shrink-0");
    azioni.appendChild(pulsante("Modifica", "btn btn-sm btn-light border", () => apri(c)));
    azioni.appendChild(pulsante("Elimina", "btn btn-sm btn-danger", () => elimina(c)));

    riga.append(sinistra, azioni);
    return riga;
}

function apri(c) {
    inModifica = c || null;
    $("nome").value = c ? c.nome : "";
    seleziona("icona", c ? c.icona : "tag");
    seleziona("colore", c ? c.colore : aspetto.colori[0]);
    aggiornaAnteprima();
    $("erroreFinestra").textContent = "";
    modulo.apri(c ? "Modifica categoria" : "Nuova categoria");
}

async function salva() {

    const errore = $("erroreFinestra");
    const nome = $("nome").value.trim();

    if (!nome) {
        errore.textContent = "Inserisci il nome della categoria.";
        return;
    }

    try {
        const response = inModifica
            ? await api(`${API}/${inModifica.id}`, jsonBody("PUT", { nome, icona: scelto("icona"), colore: scelto("colore") }))
            : await api(API, jsonBody("POST", { nome, icona: scelto("icona"), colore: scelto("colore") }));

        if (!response.ok) {
            errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
            return;
        }

        modulo.chiudi();
        await caricaCategorie();

    } catch {
        errore.textContent = "Errore nel collegamento al server.";
    }
}

async function elimina(c) {

    if (!confirm(`Eliminare la categoria "${c.nome}"?`)) return;

    try {
        const response = await api(`${API}/${c.id}`, { method: "DELETE" });
        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'eliminazione."));
            return;
        }
        await caricaCategorie();
    } catch {
        alert("Errore nel collegamento al server.");
    }
}

$("nuova").addEventListener("click", () => apri(null));
$("annulla").addEventListener("click", () => modulo.chiudi());
alInvio($("formCategoria"), salva);

avviaPagina({ modulo: MODULI.admin, amministratore: true })
    .then(async () => {
        // Le categorie appartengono al modulo Uscite: Admin → Moduli → Uscite → Categorie.
        intestazioneModulo($("intestazioneModulo"), MODULI.uscite, "categorie");
        aspetto = await (await api(`${API}/aspetto`)).json();
        preparaScelte();
    })
    .then(caricaCategorie);
