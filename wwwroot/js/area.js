// Pagina principale di un modulo (area.html?modulo=codice): un riquadro per ogni pagina
// del modulo che l'utente può aprire. Chi non ha accesso al modulo torna alla pagina di
// ingresso (avviaPagina). Componenti e funzioni comuni: componenti.js.

const codiceModulo = new URLSearchParams(location.search).get("modulo") || "";

function riquadroPagina(p, colore) {
    const col = el("div", "col-sm-6");
    const card = el("a", "card card-body shadow-sm border-0 h-100 text-reset text-decoration-none d-flex flex-row align-items-center gap-3");
    card.href = p.href;
    const testo = el("div", "min-w-0");
    testo.appendChild(el("div", "fw-bold", p.testo));
    if (p.descrizione) testo.appendChild(el("div", "small text-body-secondary", p.descrizione));
    const freccia = el("i", "bi bi-chevron-right ms-auto text-body-secondary");
    freccia.setAttribute("aria-hidden", "true");
    card.append(iconaCategoria(p.icona, colore), testo, freccia);
    col.appendChild(card);
    return col;
}

if (!DEFINIZIONE_MODULI[codiceModulo]) {
    location.href = "/";
} else {
    avviaPagina({ modulo: codiceModulo }).then(utente => {
        const def = DEFINIZIONE_MODULI[codiceModulo];
        const colore = def.colore || "#6c757d";
        document.title = `${def.nome}`;

        const titolo = document.getElementById("titolo");
        titolo.replaceChildren(iconaCategoria(def.icona, colore), el("h2", "h3 fw-bold mb-0", def.nome));
        document.getElementById("descrizione").textContent = def.descrizione;

        document.getElementById("pagine").replaceChildren(
            ...pagineAbilitate(utente, codiceModulo).map(p => riquadroPagina(p, colore)));
    });
}
