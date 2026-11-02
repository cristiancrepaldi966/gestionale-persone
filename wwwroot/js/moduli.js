// Pagina Admin → Moduli (moduli.html): elenco dei moduli; cliccando se ne gestiscono
// le autorizzazioni (modulo.html). Componenti e funzioni comuni: componenti.js.

async function caricaModuli() {
    const lista = document.getElementById("listaModuli");
    try {
        const response = await api("/api/moduli");
        if (!response.ok) throw new Error();
        const moduli = await response.json();

        lista.replaceChildren(...moduli.map(m => {
            const a = el("a", "list-group-item list-group-item-action d-flex align-items-center gap-3 py-3");
            a.href = `/modulo.html?codice=${encodeURIComponent(m.codice)}`;
            const info = el("div", "flex-grow-1");
            info.append(
                el("div", "fw-bold fs-5", m.nome),
                el("div", "small text-body-secondary", m.descrizione),
                el("div", "small mt-1", m.numeroUtenti === 1 ? "1 utente con accesso" : `${m.numeroUtenti} utenti con accesso`));
            const freccia = el("i", "bi bi-chevron-right text-body-secondary");
            freccia.setAttribute("aria-hidden", "true");
            a.append(iconaCategoria(ICONE_MODULI[m.codice] || "grid", "#212529"), info, freccia);
            return a;
        }));
    } catch {
        lista.replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento dei moduli."));
    }
}

avviaPagina({ modulo: MODULI.admin, amministratore: true }).then(caricaModuli);
