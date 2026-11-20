// Pagina Politici del modulo Fantapolitica (fantapolitica-gestione.html), solo per il
// Super admin: elenco dei politici che i giocatori scelgono per le squadre.
// Componenti comuni: componenti.js e js/fantapolitica-comune.js.

const API = `${API_FANTAPOLITICA}/politici`;
const $ = id => document.getElementById(id);

let inModifica = null; // politico in modifica, null se è nuovo

const modulo = moduloFinestra($("finestra"), { primoCampo: $("nome") });

async function caricaPolitici() {
    try {
        const response = await api(API);
        if (!response.ok) throw new Error();
        mostraPolitici(await response.json());
    } catch {
        $("listaPolitici").replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento dei politici."));
    }
}

function mostraPolitici(politici) {
    $("riepilogo").textContent = `${politici.length} ${politici.length === 1 ? "politico" : "politici"}`;

    if (politici.length === 0) {
        $("listaPolitici").replaceChildren(el("p", "p-3 mb-0 text-body-secondary",
            "Nessun politico. Usa il pulsante + in alto a destra."));
        return;
    }

    $("listaPolitici").replaceChildren(...politici.map(rigaPolitico));
}

function rigaPolitico(p) {
    const riga = el("div", "list-group-item d-flex justify-content-between align-items-center gap-3 py-3");

    const info = el("div", "min-w-0");
    const titolo = el("div", "d-flex align-items-center gap-2 flex-wrap");
    titolo.append(el("span", "fw-bold text-break", nomePolitico(p)), etichettaRuolo(p.ruolo));
    info.appendChild(titolo);
    const sotto = [p.partito, p.squadre === 0 ? "in nessuna squadra" : p.squadre === 1 ? "in 1 squadra" : `in ${p.squadre} squadre`]
        .filter(Boolean).join(" · ");
    info.appendChild(el("div", "small text-body-secondary", sotto));

    const azioni = el("div", "d-flex gap-2 flex-shrink-0");
    azioni.appendChild(pulsante("Modifica", "btn btn-sm btn-light border", () => apri(p)));
    azioni.appendChild(pulsante("Elimina", "btn btn-sm btn-danger", () => elimina(p)));

    riga.append(info, azioni);
    return riga;
}

function apri(p) {
    inModifica = p || null;
    $("nome").value = p ? p.nome : "";
    $("cognome").value = p ? p.cognome : "";
    $("ruolo").value = p ? p.ruolo : "Consigliere";
    $("partito").value = p ? p.partito || "" : "";
    $("erroreFinestra").textContent = "";
    modulo.apri(p ? "Modifica politico" : "Nuovo politico");
}

async function salva() {
    const errore = $("erroreFinestra");
    const dati = {
        nome: $("nome").value.trim(),
        cognome: $("cognome").value.trim(),
        ruolo: $("ruolo").value,
        partito: $("partito").value.trim() || null
    };

    if (!dati.nome || !dati.cognome) {
        errore.textContent = "Inserisci nome e cognome.";
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
        await caricaPolitici();
    } catch {
        errore.textContent = "Errore nel collegamento al server.";
    }
}

async function elimina(p) {
    const avviso = p.squadre > 0
        ? `\n\nÈ in ${p.squadre === 1 ? "1 squadra" : p.squadre + " squadre"}: verrà tolto e quelle squadre resteranno incomplete.`
        : "";
    if (!confirm(`Eliminare ${nomePolitico(p)}?${avviso}`)) return;

    try {
        const response = await api(`${API}/${p.id}`, { method: "DELETE" });
        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'eliminazione."));
            return;
        }
        await caricaPolitici();
    } catch {
        alert("Errore nel collegamento al server.");
    }
}

$("nuovo").addEventListener("click", () => apri(null));
$("annulla").addEventListener("click", () => modulo.chiudi());
alInvio($("formPolitico"), salva);

// Modulo Fantapolitica, solo Super admin.
avviaPagina({ modulo: MODULI.fantapolitica, superAdmin: true }).then(caricaPolitici);
