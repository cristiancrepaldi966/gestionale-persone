// Funzioni comuni alle pagine del modulo Analisi di mercato (mercato.html, mercato-scheda.html).

const $ = id => document.getElementById(id);

const numero = new Intl.NumberFormat("it-IT", { useGrouping: "always" });
const euroIntero = new Intl.NumberFormat("it-IT", { style: "currency", currency: "EUR", maximumFractionDigits: 0, useGrouping: "always" });

// 1234567 → "1,2 mln", 25000 → "25.000"
function persone(n) {
    if (n >= 1_000_000) return (Math.round(n / 100_000) / 10).toLocaleString("it-IT") + " mln";
    return numero.format(n);
}

const testoMese = m => m == null ? "Non raggiunto" : `Mese ${m}`;

const COLORI_GRAFICI = {
    primario: "#2a78d6",
    primarioScuro: "#1c5cab",
    secondario: "#a3a29b",
    verde: "#1baf7a",
    rosso: "#e34948",
    arancio: "#eb6834",
    griglia: "#e9ecef",
    testo: "#6c757d",
};

if (window.Chart) {
    Chart.defaults.font.family = getComputedStyle(document.body).fontFamily;
    Chart.defaults.color = COLORI_GRAFICI.testo;
}

// Etichette dell'asse in euro senza decimali ("1.500 €").
const tickEuro = v => euroIntero.format(v);

// Riquadro con un numero chiave: titolo piccolo, numero grande, nota sotto.
function numeroChiave(titolo, valore, nota, icona, classeColonna = "col-6 col-md-3") {
    const col = el("div", classeColonna);
    const card = el("div", "card card-body shadow-sm border-0 h-100 py-3");
    const testa = el("div", "small text-body-secondary d-flex align-items-center gap-1");
    if (icona) {
        const i = el("i", "bi bi-" + icona);
        i.setAttribute("aria-hidden", "true");
        testa.appendChild(i);
    }
    testa.appendChild(el("span", null, titolo));
    card.append(testa, el("div", "numero-chiave mt-1", valore));
    if (nota) card.appendChild(el("div", "small text-body-secondary mt-1", nota));
    col.appendChild(card);
    return col;
}

// Icona di Bootstrap Icons (decorativa).
function icona(nome, classi = "") {
    const i = el("i", `bi bi-${nome} ${classi}`.trim());
    i.setAttribute("aria-hidden", "true");
    return i;
}
