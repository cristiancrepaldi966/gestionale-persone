// Pagina di ingresso (index.html), per tutti: i moduli abilitati per l'utente,
// ognuno con le sue pagine. Componenti e funzioni comuni: componenti.js.

function riquadroModulo(utente, codice) {
    const def = DEFINIZIONE_MODULI[codice];
    const pagine = pagineAbilitate(utente, codice);

    const col = el("div", "col-sm-6");
    const card = el("div", "card shadow-sm border-0 h-100");
    const corpo = el("div", "card-body d-flex flex-column gap-3");

    // Intestazione cliccabile: porta alla pagina principale del modulo.
    const testa = el("a", "d-flex align-items-center gap-3 text-reset text-decoration-none");
    testa.href = paginaModulo(codice);
    const info = el("div");
    info.append(el("div", "fw-bold fs-5", def.nome), el("div", "small text-body-secondary", def.descrizione));
    testa.append(iconaCategoria(def.icona, def.colore || "#6c757d"), info);

    // Il livello nel modulo; il Super admin lo vede una volta sola, sotto il saluto.
    const parti = [testa];
    if (!utente.superAdmin) {
        const livelloModulo = livello(utente, codice);
        const badge = etichetta(livelloModulo, livelloModulo === "Amministratore" ? "primary" : "secondary");
        badge.classList.add("align-self-start");
        parti.push(badge);
    }

    const collegamenti = el("div", "d-flex flex-wrap gap-2 mt-auto");
    for (const p of pagine) {
        const a = el("a", "btn btn-light border d-inline-flex align-items-center gap-2");
        a.href = p.href;
        const i = el("i", "bi bi-" + p.icona);
        i.setAttribute("aria-hidden", "true");
        a.append(i, el("span", null, p.testo));
        collegamenti.appendChild(a);
    }

    corpo.append(...parti, collegamenti);
    card.appendChild(corpo);
    col.appendChild(card);
    return col;
}

avviaPagina().then(utente => {
    const nome = (utente.nome || "").trim();
    $id("saluto").textContent = nome ? `Ciao, ${nome}` : "Ciao";

    const moduli = moduliAbilitati(utente);
    if (moduli.length === 0) {
        $id("sottotitolo").textContent = "";
        const col = el("div", "col-12");
        const card = el("div", "card card-body shadow-sm border-0 p-4 text-center");
        const icona = el("i", "bi bi-lock fs-1 text-body-secondary");
        icona.setAttribute("aria-hidden", "true");
        card.append(icona, el("h3", "h5 fw-bold mt-2", "Nessun modulo abilitato"),
            el("p", "text-body-secondary mb-0",
                "Il tuo utente non ha ancora accesso a nessun modulo. Chiedi a un amministratore di abilitarti."));
        col.appendChild(card);
        $id("moduli").replaceChildren(col);
        return;
    }

    $id("sottotitolo").textContent = moduli.length === 1 ? "Ecco il tuo modulo." : "Scegli un modulo.";
    if (utente.superAdmin) {
        const badge = etichetta("Super admin", "dark");
        badge.classList.add("ms-2", "align-middle");
        $id("sottotitolo").appendChild(badge);
    }
    $id("moduli").replaceChildren(...moduli.map(m => riquadroModulo(utente, m)));
});

function $id(id) { return document.getElementById(id); }
