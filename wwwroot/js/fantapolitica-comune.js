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

// Numeri con la virgola e al massimo un decimale: 8.5 → "8,5", 6 → "6".
const numero = new Intl.NumberFormat("it-IT", { maximumFractionDigits: 1 });

// "+5", "-2", "+8,5", "0"
const conSegno = n => (n > 0 ? "+" : "") + numero.format(n);

const punti = n => `${conSegno(n)} ${Math.abs(n) === 1 ? "punto" : "punti"}`;

// Voto di una pagella: "6,5" oppure "s.v." (senza voto).
const testoVoto = v => v == null ? "s.v." : numero.format(v);

// Etichetta con i punti: verde se positivi, rossa se negativi, grigia se zero.
function etichettaPunti(n) {
    const colore = n > 0 ? "success" : n < 0 ? "danger" : "light border text-body-secondary";
    const e = el("span", `badge rounded-pill text-bg-${colore} fw-semibold d-inline-flex align-items-center gap-1`);
    e.append(iconaBi("trophy"), document.createTextNode(punti(n)));
    return e;
}

// Elenco compatto dei politici di una squadra: "Nome Cognome", ruolo, prezzo pagato e
// punti portati alla squadra.
function elencoPoliticiSquadra(politici) {
    const ul = el("ul", "list-unstyled mb-0 d-flex flex-column gap-1");
    for (const p of politici) {
        const li = el("li", "d-flex align-items-center gap-2 flex-wrap");
        li.append(el("span", null, nomePolitico(p)), etichettaRuolo(p.ruolo), etichettaCrediti(p.prezzo), etichettaPunti(p.punti));
        if (p.partito) li.appendChild(el("span", "small text-body-secondary", p.partito));
        ul.appendChild(li);
    }
    return ul;
}
