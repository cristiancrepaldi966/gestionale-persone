// Pagina Classifica del modulo Fantapolitica (fantapolitica-classifica.html), per tutti:
// - Squadre: tutte le squadre di tutte le leghe per punti (a pari punti, meno crediti
//   spesi), con filtro per lega;
// - Politici: tutti i politici per punti, con gli eventi che li hanno dati o tolti.
// Ordine e posizioni arrivano dal server (GET /api/fantapolitica/classifica).
// Componenti comuni: componenti.js e js/fantapolitica-comune.js.

const $ = id => document.getElementById(id);

let classifica = { squadre: [], politici: [] };
let utenteCorrente = null;

// Cerchio con la posizione (oro, argento, bronzo sul podio).
function cerchioPosizione(posizione) {
    const c = el("span", "posizione-classifica" + (posizione <= 3 ? ` podio-${posizione}` : ""), String(posizione));
    c.title = `${posizione}° posto`;
    return c;
}

// Riga che si apre: posizione, testo al centro, punti a destra; sotto il dettaglio.
function rigaApribile(posizione, centro, puntiTotali, dettaglio) {
    const li = el("li", "list-group-item px-3 py-2");
    const dettagli = el("details", "squadra-lega");
    const sommario = el("summary", "d-flex align-items-center gap-3");
    sommario.append(cerchioPosizione(posizione), centro, etichettaPunti(puntiTotali));
    dettaglio.classList.add("mt-2", "small", "dettaglio-squadra");
    dettagli.append(sommario, dettaglio);
    li.appendChild(dettagli);
    return li;
}

// ---------------------------------------------------------------------------
// Squadre
// ---------------------------------------------------------------------------

// Con il filtro su una lega le posizioni si ricalcolano (stessi punti e crediti = pari merito).
function mostraSquadre() {
    const lega = Number($("filtroLega").value) || null;
    const elenco = classifica.squadre.filter(x => !lega || x.lega.id === lega);

    let posizioni = elenco.map(x => x.posizione);
    if (lega) {
        posizioni = [];
        elenco.forEach((x, i) => {
            const prec = elenco[i - 1];
            posizioni.push(prec && prec.squadra.punti === x.squadra.punti && prec.squadra.spesi === x.squadra.spesi
                ? posizioni[i - 1] : i + 1);
        });
    }

    if (elenco.length === 0) {
        $("classificaSquadre").replaceChildren(el("li", "list-group-item text-body-secondary p-3",
            "Nessuna squadra ancora. Le squadre si creano da La mia squadra."));
        return;
    }

    $("classificaSquadre").replaceChildren(...elenco.map((x, i) => {
        const io = x.utente.id === utenteCorrente.id;
        const centro = el("span", "d-flex flex-column min-w-0 me-auto");
        const nome = el("span", "d-flex align-items-center gap-1 fw-semibold text-break" + (io ? " text-primary" : ""));
        nome.append(iconaBi("shield-fill", "text-body-secondary"), el("span", null, x.squadra.nome));
        centro.append(nome, el("span", "small text-body-secondary text-break",
            `${x.utente.nome}${io ? " (tu)" : ""}${lega ? "" : " · " + titoloLega(x.lega.nome)}`));

        const dettaglio = el("div");
        dettaglio.appendChild(elencoPoliticiSquadra(x.squadra.politici));
        dettaglio.appendChild(el("div", "text-body-secondary mt-2", `${crediti(x.squadra.spesi)} spesi`));
        return rigaApribile(posizioni[i], centro, x.squadra.punti, dettaglio);
    }));
}

// ---------------------------------------------------------------------------
// Politici
// ---------------------------------------------------------------------------

function mostraPolitici() {
    if (classifica.politici.length === 0) {
        $("classificaPolitici").replaceChildren(el("li", "list-group-item text-body-secondary p-3", "Nessun politico."));
        return;
    }

    $("classificaPolitici").replaceChildren(...classifica.politici.map(x => {
        const p = x.politico;
        const centro = el("span", "d-flex flex-column min-w-0 me-auto");
        const nome = el("span", "d-flex align-items-center gap-2 flex-wrap");
        nome.append(el("span", "fw-semibold text-break", nomePolitico(p)), etichettaRuolo(p.ruolo));
        centro.append(nome, el("span", "small text-body-secondary",
            [p.partito, crediti(p.costo),
                p.squadre === 0 ? "in nessuna squadra" : p.squadre === 1 ? "in 1 squadra" : `in ${p.squadre} squadre`]
                .filter(Boolean).join(" · ")));

        const dettaglio = el("div");
        if (x.eventi.length === 0) {
            dettaglio.appendChild(el("div", "text-body-secondary", "Nessun evento finora."));
        } else {
            const lista = el("ul", "list-unstyled mb-0 d-flex flex-column gap-1");
            for (const e of x.eventi) {
                const li = el("li", "d-flex align-items-center gap-2 flex-wrap");
                li.append(el("span", "text-body-secondary", formatoData(e.data)), etichettaPunti(e.punti),
                    el("span", "text-break", e.titolo), el("span", "text-body-secondary", `voto ${testoVoto(e.voto)}`));
                for (const b of e.bonus) li.appendChild(el("span", "text-body-secondary", `· ${b.voce} ${conSegno(b.punti)}`));
                lista.appendChild(li);
            }
            dettaglio.appendChild(lista);
        }
        return rigaApribile(x.posizione, centro, p.punti, dettaglio);
    }));
}

// ---------------------------------------------------------------------------
// Schede e caricamento
// ---------------------------------------------------------------------------

// La scheda aperta resta nell'indirizzo (#politici), così si può condividere.
function mostraScheda(politici) {
    $("schedaSquadre").classList.toggle("active", !politici);
    $("schedaPolitici").classList.toggle("active", politici);
    $("schedaSquadre").setAttribute("aria-selected", String(!politici));
    $("schedaPolitici").setAttribute("aria-selected", String(politici));
    $("pannelloSquadre").hidden = politici;
    $("pannelloPolitici").hidden = !politici;
    $("filtroLega").hidden = politici;
    history.replaceState(null, "", politici ? "#politici" : location.pathname);
}

$("schedaSquadre").addEventListener("click", () => mostraScheda(false));
$("schedaPolitici").addEventListener("click", () => mostraScheda(true));
$("filtroLega").addEventListener("change", mostraSquadre);

async function carica() {
    try {
        const response = await api(`${API_FANTAPOLITICA}/classifica`);
        if (!response.ok) throw new Error();
        classifica = await response.json();

        // Filtro con le leghe che hanno almeno una squadra.
        const leghe = new Map(classifica.squadre.map(x => [x.lega.id, x.lega.nome]));
        $("filtroLega").replaceChildren(new Option("Tutte le leghe", ""),
            ...[...leghe].sort((a, b) => a[1].localeCompare(b[1], "it")).map(([id, nome]) => new Option(titoloLega(nome), id)));

        mostraSquadre();
        mostraPolitici();
    } catch {
        $("classificaSquadre").replaceChildren(el("li", "list-group-item text-danger p-3", "Errore nel caricamento della classifica."));
    }
}

// Modulo Fantapolitica, qualsiasi livello.
avviaPagina({ modulo: MODULI.fantapolitica }).then(async utente => {
    utenteCorrente = utente;
    mostraScheda(location.hash === "#politici");
    await carica();
});
