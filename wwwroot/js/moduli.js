// Pagina Admin → Moduli (moduli.html): elenco dei moduli; cliccando se ne gestiscono
// le autorizzazioni (modulo.html). Componenti e funzioni comuni: componenti.js.

async function caricaModuli() {
    const lista = document.getElementById("listaModuli");
    try {
        const response = await api("/api/moduli");
        if (!response.ok) throw new Error();
        const moduli = await response.json();

        lista.replaceChildren(...moduli.map(m => {
            const icona = iconaCategoria((DEFINIZIONE_MODULI[m.codice] || { icona: "grid" }).icona, "#212529");
            const info = el("div", "flex-grow-1");
            info.append(el("div", "fw-bold fs-5", m.nome), el("div", "small text-body-secondary", m.descrizione));

            // Modulo non assegnabile (Admin): riservato ai Super admin, che si scelgono in Utenti.
            if (m.livelli.length === 0) {
                const riga = el("div", "list-group-item d-flex align-items-center gap-3 py-3");
                info.appendChild(el("div", "small mt-1", "Riservato ai Super admin: si impostano in Admin → Utenti."));
                riga.append(icona, info);
                return riga;
            }

            const a = el("a", "list-group-item list-group-item-action d-flex align-items-center gap-3 py-3");
            a.href = `/modulo.html?codice=${encodeURIComponent(m.codice)}`;
            info.appendChild(el("div", "small mt-1",
                m.numeroUtenti === 1 ? "1 utente autorizzato" : `${m.numeroUtenti} utenti autorizzati`));
            const freccia = el("i", "bi bi-chevron-right text-body-secondary");
            freccia.setAttribute("aria-hidden", "true");
            a.append(icona, info, freccia);
            return a;
        }));
    } catch {
        lista.replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento dei moduli."));
    }
}

avviaPagina({ modulo: MODULI.admin, amministratore: true }).then(caricaModuli);
