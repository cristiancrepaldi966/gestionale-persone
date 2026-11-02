// Pagina Progetti del modulo Stampa 3D (stampa3d-progetti.html): i progetti salvati,
// con Apri (configuratore) ed Elimina. L'Amministratore può mostrare quelli di tutti.
// Dati da GET /api/stampa3d/progetti. Componenti e funzioni comuni: componenti.js.

const $ = id => document.getElementById(id);
const quando = new Intl.DateTimeFormat("it-IT", { dateStyle: "medium", timeStyle: "short" });

let catalogo = [];

function riga(p) {
    const oggetto = catalogo.find(o => o.codice === p.oggetto) || { icona: "box", nome: p.nomeOggetto };
    const li = el("li", "list-group-item d-flex align-items-center gap-3 py-3");

    const info = el("a", "d-flex align-items-center gap-3 me-auto text-reset text-decoration-none flex-grow-1 min-w-0");
    info.href = `/stampa3d-configuratore.html?progetto=${p.id}`;
    const testo = el("div", "min-w-0");
    testo.append(el("div", "fw-semibold text-break", p.nome),
        el("div", "small text-body-secondary",
            `${oggetto.nome} · ${quando.format(new Date(p.aggiornatoIl))}${!p.mio && p.creatoDa ? " · " + p.creatoDa : ""}`));
    info.append(iconaCategoria(oggetto.icona, "#eb6834", true), testo);

    const elimina = pulsante("", "btn btn-sm btn-outline-danger", () => eliminaProgetto(p));
    elimina.setAttribute("aria-label", `Elimina ${p.nome}`);
    elimina.title = "Elimina";
    const icona = el("i", "bi bi-trash");
    icona.setAttribute("aria-hidden", "true");
    elimina.appendChild(icona);

    li.append(info, elimina);
    return li;
}

async function eliminaProgetto(p) {
    if (!confirm(`Eliminare il progetto "${p.nome}"?`)) return;
    const response = await api(`/api/stampa3d/progetti/${p.id}`, { method: "DELETE" });
    if (!response.ok) {
        alert(await messaggioErrore(response, "Eliminazione non riuscita."));
        return;
    }
    await caricaProgetti();
}

async function caricaProgetti() {
    const primaVolta = !!$("elenco").querySelector(".caricamento-elenco");
    if (!primaVolta) elencoInAggiornamento($("elenco"), $("cerchiettoAggiornamento"), true);
    try {
        const response = await api(`/api/stampa3d/progetti?tutti=${$("tutti").checked}`);
        if (!response.ok) throw new Error();
        const progetti = await response.json();

        $("riepilogo").textContent = `${progetti.length} ${progetti.length === 1 ? "progetto" : "progetti"}`;
        if (progetti.length === 0) {
            const vuoto = el("li", "list-group-item text-body-secondary p-3");
            vuoto.append(document.createTextNode("Nessun progetto salvato. "));
            const link = el("a", null, "Scegli un oggetto dal catalogo");
            link.href = "/stampa3d.html";
            vuoto.append(link, document.createTextNode(" e salvalo dal configuratore."));
            $("elenco").replaceChildren(vuoto);
        } else {
            $("elenco").replaceChildren(...progetti.map(riga));
        }
    } catch {
        $("elenco").replaceChildren(el("li", "list-group-item text-body-secondary p-3", "Errore nel caricamento dei progetti."));
    } finally {
        elencoInAggiornamento($("elenco"), $("cerchiettoAggiornamento"), false);
    }
}

avviaPagina({ modulo: MODULI.stampa3d }).then(async utente => {
    $("filtroTutti").hidden = livello(utente, MODULI.stampa3d) !== "Amministratore";
    $("tutti").addEventListener("change", caricaProgetti);
    catalogo = await (await api("/api/stampa3d/catalogo")).json();
    await caricaProgetti();
});
