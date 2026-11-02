// Pagina Uscite (index.html). Componenti e funzioni comuni: componenti.js.

const API = "/api/uscite";
let inModifica = null; // uscita in modifica, null se è nuova

const $ = id => document.getElementById(id);

// Accetta "12,50", "12.50", "1.234,56", "€ 12".
function leggiImporto(testo) {
    let t = testo.replace(/[€\s]/g, "");
    if (t.includes(",")) t = t.replace(/\./g, "").replace(",", ".");
    if (!/^\d+(\.\d{1,2})?$/.test(t)) return null;
    const n = Number(t);
    return n > 0 ? n : null;
}

const ricerca = pannelloRicerca({
    bottone: $("cerca"),
    pannello: $("pannelloFiltri"),
    pallino: $("pallinoFiltri"),
    azzera: $("azzera"),
    campi: [$("filtroCategoria"), $("dal"), $("al")],
    applica: () => caricaUscite()
});

const modulo = moduloFinestra($("finestra"), { primoCampo: $("importo") });

async function caricaCategorie() {
    const response = await api("/api/categorie");
    const categorie = await response.json();
    riempiCategorie($("filtroCategoria"), categorie, "Categoria");
    riempiCategorie($("categoria"), categorie, "Scegli una categoria");
}

let richiestaCorrente = 0;

async function caricaUscite() {

    const lista = $("listaUscite");
    const numeroRichiesta = ++richiestaCorrente;
    const parametri = parametriUscite(ricerca.valori());

    elencoInAggiornamento(lista, $("cerchiettoAggiornamento"), true);

    try {
        const response = await api(`${API}?${parametri}`);
        if (!response.ok) throw new Error();
        const r = await response.json();

        // Ignora risposte arrivate in ritardo rispetto a una ricerca più recente.
        if (numeroRichiesta !== richiestaCorrente) return;

        $("riepilogo").replaceChildren(
            el("span", "text-body-secondary", `${r.numero} ${r.numero === 1 ? "uscita" : "uscite"}`),
            el("strong", "fs-5", `Totale ${euro.format(r.totale)}`));

        if (r.uscite.length === 0) {
            lista.replaceChildren(el("p", "p-3 mb-0 text-body-secondary", ricerca.attivi()
                ? "Nessuna uscita corrisponde alla ricerca."
                : "Nessuna uscita inserita. Usa il pulsante + in alto a destra."));
            return;
        }

        lista.replaceChildren(...r.uscite.map(rigaUscita));

        if (r.troncato) {
            lista.appendChild(el("p", "list-group-item small text-body-secondary mb-0",
                `Mostrate le ${r.uscite.length} più recenti: restringi la ricerca per vedere le altre. Il totale è calcolato su tutte.`));
        }

    } catch {
        if (numeroRichiesta === richiestaCorrente) {
            lista.replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento delle uscite."));
        }
    } finally {
        if (numeroRichiesta === richiestaCorrente) {
            elencoInAggiornamento(lista, $("cerchiettoAggiornamento"), false);
        }
    }
}

function rigaUscita(u) {

    const riga = coloraRiga(
        el("div", "list-group-item uscita d-flex flex-column flex-sm-row justify-content-between gap-2 py-3"),
        u.categoriaColore);

    const sinistra = el("div", "d-flex gap-3 align-items-start");
    sinistra.appendChild(iconaCategoria(u.categoriaIcona, u.categoriaColore));

    const info = el("div", "text-break");
    sinistra.appendChild(info);
    info.appendChild(el("div", "fw-bold fs-5", u.categoria));

    const dettagli = el("div", "d-flex flex-wrap gap-2 small");
    dettagli.appendChild(el("span", null, formatoData(u.data)));
    if (u.creatoDa) dettagli.appendChild(el("span", "text-body-secondary", u.creatoDa));
    info.appendChild(dettagli);

    if (u.note) info.appendChild(el("div", "small text-body-secondary testo-note mt-1", u.note));

    const destra = el("div", "d-flex flex-sm-column justify-content-between align-items-center align-items-sm-end gap-2");
    destra.appendChild(el("div", "fw-bold fs-5 text-nowrap", euro.format(u.importo)));

    const azioni = el("div", "d-flex gap-2");
    azioni.appendChild(pulsante("Modifica", "btn btn-sm btn-light border", () => apri(u)));
    azioni.appendChild(pulsante("Elimina", "btn btn-sm btn-danger", () => eliminaUscita(u)));
    destra.appendChild(azioni);

    riga.append(sinistra, destra);
    return riga;
}

function apri(u) {
    inModifica = u || null;
    $("data").value = u ? u.data : oggi();
    $("importo").value = u ? u.importo.toFixed(2).replace(".", ",") : "";
    $("categoria").value = u ? u.categoriaId : "";
    $("note").value = u && u.note ? u.note : "";
    $("erroreFinestra").textContent = "";
    modulo.apri(u ? "Modifica uscita" : "Nuova uscita");
}

async function salvaUscita() {

    const errore = $("erroreFinestra");
    const importo = leggiImporto($("importo").value);

    if (importo === null) {
        errore.textContent = "Inserisci un importo valido maggiore di zero (es. 12,50).";
        return;
    }

    const categoriaId = Number($("categoria").value);
    if (!categoriaId) {
        errore.textContent = "Scegli una categoria.";
        return;
    }

    const dati = {
        data: $("data").value,
        importo,
        categoriaId,
        note: $("note").value.trim() || null
    };

    if (!dati.data) {
        errore.textContent = "Inserisci la data.";
        return;
    }

    try {
        const response = inModifica
            ? await api(`${API}/${inModifica.id}`, jsonBody("PUT", dati))
            : await api(API, jsonBody("POST", dati));

        if (!response.ok) {
            errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
            return;
        }

        modulo.chiudi();
        await caricaUscite();

    } catch {
        errore.textContent = "Errore nel collegamento al server.";
    }
}

async function eliminaUscita(u) {

    if (!confirm(`Eliminare l'uscita di ${euro.format(u.importo)} del ${formatoData(u.data)} (${u.categoria})?`)) return;

    try {
        const response = await api(`${API}/${u.id}`, { method: "DELETE" });
        if (!response.ok) throw new Error();
        await caricaUscite();
    } catch {
        alert("Errore durante l'eliminazione.");
    }
}

$("nuova").addEventListener("click", () => apri(null));
$("annulla").addEventListener("click", () => modulo.chiudi());
alInvio($("formUscita"), salvaUscita);

avviaPagina()
    .then(caricaCategorie)
    .then(caricaUscite)
    .catch(() => {
        $("listaUscite").replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel collegamento al server."));
    });
