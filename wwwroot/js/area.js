// Pagina principale di un modulo (area.html?modulo=codice): una card per ogni pagina
// del modulo che l'utente può aprire, due per riga (anche su telefono). Chi non ha accesso al modulo torna alla pagina di
// ingresso (avviaPagina). Componenti e funzioni comuni: componenti.js.

const codiceModulo = new URLSearchParams(location.search).get("modulo") || "";

// Pagina riservata (Amministratore del modulo o Super admin): sulla card c'è "Admin".
const paginaAdmin = p => !!(p.amministratore || p.superAdmin);

// Card verticale, due per riga anche su telefono: icona, titolo (con "Admin" sulle pagine
// riservate) e descrizione breve (al massimo due righe).
function riquadroPagina(p, colore) {
    const col = el("div", "col-6");
    const card = el("a", "card card-body shadow-sm border-0 h-100 text-reset text-decoration-none card-pagina");
    card.href = p.href;
    const titolo = el("div", "d-flex align-items-center gap-2 flex-wrap mt-3");
    titolo.appendChild(el("span", "fw-bold text-break", p.testo));
    if (paginaAdmin(p)) titolo.appendChild(etichetta("Admin", "dark"));
    card.append(iconaCategoria(p.icona, colore), titolo);
    if (p.descrizione) card.appendChild(el("div", "small text-body-secondary mt-1 descrizione-pagina", p.descrizione));
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

        // Prima le pagine per tutti (nell'ordine del menu), in fondo quelle Admin.
        const pagine = pagineAbilitate(utente, codiceModulo);
        document.getElementById("pagine").replaceChildren(
            ...[...pagine.filter(p => !paginaAdmin(p)), ...pagine.filter(paginaAdmin)].map(p => riquadroPagina(p, colore)));
    });
}
