// Pagina Catalogo del modulo Stampa 3D (stampa3d.html): una card per ogni oggetto,
// che porta al configuratore. Dati da GET /api/stampa3d/catalogo.
// Componenti e funzioni comuni: componenti.js.

const COLORE_OGGETTI = "#eb6834";

function cardOggetto(o) {
    const col = el("div", "col-sm-6 col-lg-4");
    const card = el("a", "card card-body shadow-sm border-0 h-100 text-reset text-decoration-none d-flex flex-column gap-2");
    card.href = `/stampa3d-configuratore.html?oggetto=${encodeURIComponent(o.codice)}`;

    const testa = el("div", "d-flex align-items-center gap-3");
    testa.append(iconaCategoria(o.icona, COLORE_OGGETTI), el("div", "fw-bold", o.nome));

    const misure = el("div", "small text-body-secondary mt-auto",
        `${o.parametri.length} ${o.parametri.length === 1 ? "opzione" : "opzioni"} regolabili`);

    card.append(testa, el("p", "small mb-0", o.descrizione), misure);
    col.appendChild(card);
    return col;
}

async function carica() {
    try {
        const response = await api("/api/stampa3d/catalogo");
        if (!response.ok) throw new Error();
        const catalogo = await response.json();
        document.getElementById("catalogo").replaceChildren(...catalogo.map(cardOggetto));
    } catch {
        const col = el("div", "col-12");
        col.appendChild(el("p", "card card-body shadow-sm border-0 text-body-secondary mb-0", "Errore nel caricamento del catalogo."));
        document.getElementById("catalogo").replaceChildren(col);
    }
}

avviaPagina({ modulo: MODULI.stampa3d }).then(carica);
