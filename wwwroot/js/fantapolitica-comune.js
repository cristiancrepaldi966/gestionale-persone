// Funzioni comuni alle pagine del modulo Fantapolitica (Leghe, La mia squadra, Politici).
// Da caricare dopo componenti.js.

const API_FANTAPOLITICA = "/api/fantapolitica";

// Icona di Bootstrap Icons (solo decorativa).
function iconaBi(nome, classi = "") {
    const i = el("i", `bi bi-${nome} ${classi}`.trim());
    i.setAttribute("aria-hidden", "true");
    return i;
}

// "Lega del Bar" resta così; "Amici" diventa "Lega Amici".
const titoloLega = nome => /^lega\b/i.test(nome) ? nome : "Lega " + nome;

const nomePolitico = p => `${p.nome} ${p.cognome}`;

// Colore dell'etichetta del ruolo.
const COLORI_RUOLO = { Sindaco: "primary", Vicesindaco: "info", Assessore: "success", Consigliere: "secondary" };

function etichettaRuolo(ruolo) {
    return etichetta(ruolo, COLORI_RUOLO[ruolo] || "secondary");
}

const crediti = n => `${n} ${n === 1 ? "credito" : "crediti"}`;

// Etichetta con il costo o il prezzo in crediti.
function etichettaCrediti(n) {
    const e = el("span", "badge rounded-pill text-bg-warning fw-semibold d-inline-flex align-items-center gap-1");
    e.append(iconaBi("coin"), document.createTextNode(crediti(n)));
    return e;
}

// Elenco compatto dei politici di una squadra: "Nome Cognome", ruolo e prezzo pagato.
function elencoPoliticiSquadra(politici) {
    const ul = el("ul", "list-unstyled mb-0 d-flex flex-column gap-1");
    for (const p of politici) {
        const li = el("li", "d-flex align-items-center gap-2 flex-wrap");
        li.append(el("span", null, nomePolitico(p)), etichettaRuolo(p.ruolo), etichettaCrediti(p.prezzo));
        if (p.partito) li.appendChild(el("span", "small text-body-secondary", p.partito));
        ul.appendChild(li);
    }
    return ul;
}
