// Pagina Autorizzazioni (autorizzazioni.html, modulo Admin): per ogni persona il
// livello in ciascun modulo. Ogni modifica si salva subito.
// Componenti e funzioni comuni: componenti.js.

const $ = id => document.getElementById(id);

let moduli = [];     // da /api/moduli: { codice, nome, descrizione, livelli }
let utenti = [];
let idCorrente = null;

const nomeCompleto = u => `${u.nome} ${u.cognome}`.trim();

const ricerca = pannelloRicerca({
    bottone: $("cerca"),
    pannello: $("pannelloFiltri"),
    pallino: $("pallinoFiltri"),
    azzera: $("azzera"),
    campi: [$("filtroTesto")],
    applica: () => mostra()
});

async function carica() {
    try {
        const [rm, ru] = await Promise.all([api("/api/moduli"), api("/api/utenti")]);
        if (!rm.ok || !ru.ok) throw new Error();
        moduli = await rm.json();
        utenti = await ru.json();

        $("spiegazione").textContent = moduli.map(m => `${m.nome}: ${m.descrizione}.`).join(" ") +
            " Le modifiche si salvano subito e valgono dalla richiesta successiva della persona.";
        mostra();
    } catch {
        $("listaAutorizzazioni").replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento."));
    }
}

function mostra() {
    const testo = ricerca.valori().filtroTesto.toLocaleLowerCase("it-IT");
    const trovati = utenti.filter(u =>
        `${u.nome} ${u.cognome} ${u.cognome} ${u.nome} ${u.email}`.toLocaleLowerCase("it-IT").includes(testo));

    $("listaAutorizzazioni").replaceChildren(...(trovati.length
        ? trovati.map(riga)
        : [el("p", "p-3 mb-0 text-body-secondary", "Nessuna persona corrisponde alla ricerca.")]));
}

function riga(u) {

    const div = el("div", "list-group-item py-3");
    const intestazione = el("div", "d-flex flex-wrap align-items-baseline gap-2 mb-2");
    intestazione.appendChild(nomeCompleto(u)
        ? el("span", "fw-bold fs-5", nomeCompleto(u))
        : el("span", "fs-5 fst-italic text-body-secondary", "Nome non inserito"));
    intestazione.appendChild(el("span", "text-body-secondary text-break", u.email));
    if (u.id === idCorrente) intestazione.appendChild(etichetta("tu", "light"));
    if (!u.attivo) intestazione.appendChild(etichetta("Disattivato", "danger"));

    const griglia = el("div", "row g-2");
    const stato = el("div", "small mt-1");

    for (const m of moduli) {
        const col = el("div", "col-6 col-md-4");
        const id = `aut-${u.id}-${m.codice}`;
        const label = el("label", "form-label small fw-semibold mb-1", m.nome);
        label.htmlFor = id;

        const select = el("select", "form-select");
        select.id = id;
        select.append(new Option("Nessuno", ""), ...m.livelli.map(l => new Option(l, l)));
        select.value = u.autorizzazioni[m.codice] || "";

        // Non si può togliere a se stessi l'amministrazione del modulo Admin.
        if (u.id === idCorrente && m.codice === MODULI.admin) {
            select.disabled = true;
            select.title = "Non puoi togliere a te stesso l'amministrazione del modulo Admin.";
        }

        select.addEventListener("change", () => salva(u, select, m.codice, stato, griglia));
        col.append(label, select);
        griglia.appendChild(col);
    }

    div.append(intestazione, griglia, stato);
    return div;
}

async function salva(u, select, modulo, stato, griglia) {

    const precedente = u.autorizzazioni[modulo] || "";
    const nuove = { ...u.autorizzazioni, [modulo]: select.value || null };

    const selects = [...griglia.querySelectorAll("select")];
    selects.forEach(s => s.disabled = true);
    stato.className = "small mt-1 text-body-secondary";
    stato.replaceChildren(cerchietto(), document.createTextNode(" Salvataggio..."));

    try {
        const response = await api(`/api/utenti/${u.id}/autorizzazioni`, jsonBody("PUT", nuove));
        if (!response.ok) {
            select.value = precedente;
            mostraMessaggio(stato, await messaggioErrore(response, "Errore durante il salvataggio."));
            return;
        }
        const aggiornato = await response.json();
        u.autorizzazioni = aggiornato.autorizzazioni;
        mostraMessaggio(stato, "Salvato.", "successo");
        setTimeout(() => { if (stato.textContent === "Salvato.") stato.textContent = ""; }, 2500);
    } catch {
        select.value = precedente;
        mostraMessaggio(stato, "Errore nel collegamento al server.");
    } finally {
        selects.forEach(s => {
            s.disabled = u.id === idCorrente && s.id.endsWith("-" + MODULI.admin);
        });
    }
}

avviaPagina({ modulo: MODULI.admin, amministratore: true }).then(u => {
    idCorrente = u.id;
    return carica();
});
