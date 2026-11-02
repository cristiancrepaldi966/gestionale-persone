// Pagina Scheda di mercato (mercato-scheda.html?modulo=codice): idea e posizionamento,
// clienti e mercato, concorrenti, piano di evoluzione, canali e simulazione dei soldi.
// Dati da GET /api/mercato/{modulo}; le prove con POST /api/mercato/{modulo}/simulazione;
// l'Amministratore del modulo salva (PUT) o ripristina (DELETE) /api/mercato/{modulo}/parametri.
// Funzioni comuni: componenti.js e js/mercato-comune.js.

const codiceModulo = new URLSearchParams(location.search).get("modulo") || "";

// Ipotesi della simulazione: campo, etichetta, unità, passo, limiti e spiegazione.
const CAMPI = [
    { nome: "utentiIniziali", etichetta: "Utenti di partenza", unita: "utenti", passo: 1, min: 0, max: 1000000, aiuto: "Chi lo usa già oggi." },
    { nome: "prezzoMensile", etichetta: "Prezzo Premium", unita: "€/mese", passo: 0.01, min: 0, max: 1000, aiuto: "Quanto paga al mese chi sceglie la versione a pagamento." },
    { nome: "quotaPaganti", etichetta: "Utenti che pagano", unita: "%", passo: 0.1, min: 0, max: 100, aiuto: "Di solito tra il 2% e il 6% nelle app freemium." },
    { nome: "ricavoPubblicitario", etichetta: "Pubblicità e sponsor", unita: "€/utente", passo: 0.01, min: 0, max: 10, aiuto: "Ricavo al mese per ogni utente attivo." },
    { nome: "budgetMarketing", etichetta: "Budget marketing", unita: "€/mese", passo: 10, min: 0, max: 1000000, aiuto: "Pubblicità e collaborazioni a pagamento." },
    { nome: "costoPerUtente", etichetta: "Costo per nuovo utente", unita: "€", passo: 0.1, min: 0.1, max: 1000, aiuto: "Quanto costa in media portare un utente con il marketing." },
    { nome: "crescitaOrganica", etichetta: "Passaparola", unita: "%/mese", passo: 0.1, min: 0, max: 50, aiuto: "Nuovi utenti portati da quelli esistenti (inviti, leghe, condivisioni)." },
    { nome: "abbandono", etichetta: "Abbandono", unita: "%/mese", passo: 0.1, min: 0, max: 100, aiuto: "Utenti che smettono ogni mese." },
    { nome: "oreSettimanali", etichetta: "Ore di lavoro", unita: "ore/sett.", passo: 0.5, min: 0, max: 100, aiuto: "Tempo dedicato al modulo ogni settimana." },
    { nome: "costoOrario", etichetta: "Valore di un'ora", unita: "€", passo: 1, min: 0, max: 500, aiuto: "Per calcolare quanto vale il lavoro." },
    { nome: "costiFissi", etichetta: "Costi fissi", unita: "€/mese", passo: 1, min: 0, max: 1000000, aiuto: "Server, dominio, strumenti, store." },
    { nome: "mesi", etichetta: "Durata", unita: "mesi", passo: 1, min: 6, max: 60, aiuto: "Orizzonte della simulazione." },
];

let utente = null;
let amministratore = false;
let scheda = null;          // risposta di GET /api/mercato/{modulo}
let graficoUtenti = null;
let graficoSoldi = null;
let graficoRadar = null;

// ---------------------------------------------------------------------------
// Idea, posizionamento, clienti, concorrenti
// ---------------------------------------------------------------------------

function disegnaIntestazione(dati) {
    const def = DEFINIZIONE_MODULI[dati.scheda.modulo] || { icona: "grid" };
    document.title = `${dati.nome} - Analisi di mercato - Gestionale`;
    $("percorsoNome").textContent = dati.nome;
    $("titolo").replaceChildren(iconaCategoria(def.icona, "#212529"), el("h2", "h3 fw-bold mb-0", dati.nome));
}

// Divide un'etichetta lunga in righe di circa 12 caratteri (il radar non va a capo da solo).
function aCapo(testo) {
    const righe = [];
    for (const parola of testo.split(" ")) {
        const ultima = righe[righe.length - 1];
        if (ultima && (ultima + " " + parola).length <= 12) righe[righe.length - 1] = ultima + " " + parola;
        else righe.push(parola);
    }
    return righe;
}

function disegnaPosizionamento(s) {
    $("settore").textContent = s.settore;
    $("idea").textContent = s.idea;
    $("testoPosizionamento").textContent = s.posizionamento;
    $("modelloRicavi").textContent = s.modelloRicavi;

    // Elenco (alternativa al grafico): punteggio del modulo e dei concorrenti.
    $("assi").replaceChildren(...s.assi.map(a => {
        const li = el("li", "list-group-item px-0 d-flex align-items-center gap-2");
        const differenza = a.modulo - a.concorrenti;
        li.append(el("span", "me-auto", a.nome),
            el("span", "fw-semibold text-nowrap", `${a.modulo}/5`),
            el("span", "text-body-secondary text-nowrap", `concorrenti ${a.concorrenti}/5`),
            etichetta(differenza > 0 ? "Meglio" : differenza < 0 ? "Peggio" : "Pari",
                differenza > 0 ? "success" : differenza < 0 ? "danger" : "secondary"));
        return li;
    }));

    if (graficoRadar) graficoRadar.destroy();
    graficoRadar = new Chart($("graficoRadar"), {
        type: "radar",
        data: {
            labels: s.assi.map(a => aCapo(a.nome)),
            datasets: [
                {
                    label: scheda.nome, data: s.assi.map(a => a.modulo),
                    borderColor: COLORI_GRAFICI.primario, backgroundColor: "rgba(42, 120, 214, 0.2)",
                    pointBackgroundColor: COLORI_GRAFICI.primario, borderWidth: 2
                },
                {
                    label: "Media concorrenti", data: s.assi.map(a => a.concorrenti),
                    borderColor: COLORI_GRAFICI.secondario, backgroundColor: "rgba(163, 162, 155, 0.15)",
                    pointBackgroundColor: COLORI_GRAFICI.secondario, borderWidth: 2, borderDash: [5, 4]
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            animation: { duration: 250 },
            plugins: {
                legend: { position: "bottom", labels: { boxWidth: 12, boxHeight: 12 } },
                tooltip: { callbacks: { title: el => s.assi[el[0].dataIndex].nome, label: c => ` ${c.dataset.label}: ${c.parsed.r}/5` } }
            },
            scales: {
                r: {
                    min: 0, max: 5,
                    ticks: { stepSize: 1, display: false },
                    grid: { color: COLORI_GRAFICI.griglia },
                    angleLines: { color: COLORI_GRAFICI.griglia },
                    pointLabels: { font: { size: 11 } }
                }
            }
        }
    });
}

function disegnaClienti(s) {
    const livelli = [
        ["Mercato totale", s.mercato.totale, COLORI_GRAFICI.secondario],
        ["Mercato raggiungibile", s.mercato.raggiungibile, COLORI_GRAFICI.primario],
        ["Obiettivo a 3 anni", s.mercato.obiettivo, COLORI_GRAFICI.verde],
    ];
    // Barre in scala logaritmica: i tre numeri sono di ordini di grandezza diversi.
    const massimo = Math.log10(Math.max(...livelli.map(l => l[1].persone), 10));
    $("mercato").replaceChildren(...livelli.map(([nome, d, colore]) => {
        const riga = el("div");
        const testa = el("div", "d-flex justify-content-between align-items-baseline gap-2");
        testa.append(el("span", "fw-semibold small", nome), el("span", "fw-bold", persone(d.persone) + " persone"));
        const barra = el("div", "barra-mercato my-1");
        barra.style.setProperty("--colore", colore);
        barra.style.width = Math.max(4, Math.log10(Math.max(d.persone, 10)) / massimo * 100) + "%";
        riga.append(testa, barra, el("div", "small text-body-secondary", d.descrizione));
        return riga;
    }));

    $("segmenti").replaceChildren(...s.segmenti.map(seg => {
        const li = el("li", "list-group-item px-0");
        const testa = el("div", "d-flex justify-content-between gap-2");
        testa.append(el("span", "fw-semibold", seg.nome), el("span", "text-nowrap text-body-secondary", "≈ " + persone(seg.persone)));
        li.append(testa, el("div", "small text-body-secondary", seg.descrizione));
        return li;
    }));
}

function disegnaConcorrenti(s) {
    $("elencoConcorrenti").replaceChildren(...s.concorrenti.map(c => {
        const box = el("div", "border rounded p-2 px-3");
        const testa = el("div", "d-flex flex-wrap justify-content-between gap-2");
        const nome = el("div");
        nome.append(el("span", "fw-semibold", c.nome), el("span", "small text-body-secondary ms-2", c.tipo));
        testa.append(nome, etichetta(c.prezzo, "light"));
        testa.lastChild.classList.add("border", "text-body");
        const forza = el("div", "small mt-1");
        forza.append(icona("plus-circle", "text-success me-1"), document.createTextNode(c.puntiForza));
        const debolezza = el("div", "small");
        debolezza.append(icona("dash-circle", "text-danger me-1"), document.createTextNode(c.puntiDeboli));
        box.append(testa, forza, debolezza);
        return box;
    }));

    const quadrante = (titolo, voci, colore, nomeIcona) => {
        const col = el("div", "col-md-6");
        const box = el("div", `h-100 rounded p-3 bg-${colore}-subtle`);
        const testa = el("div", `fw-semibold small mb-1 text-${colore}-emphasis`);
        testa.append(icona(nomeIcona, "me-1"), document.createTextNode(titolo));
        const lista = el("ul", "small mb-0 ps-3");
        lista.append(...voci.map(v => el("li", null, v)));
        box.append(testa, lista);
        col.appendChild(box);
        return col;
    };
    $("swot").replaceChildren(
        quadrante("Forze", s.swot.forze, "success", "hand-thumbs-up"),
        quadrante("Debolezze", s.swot.debolezze, "danger", "exclamation-triangle"),
        quadrante("Opportunità", s.swot.opportunita, "primary", "lightbulb"),
        quadrante("Minacce", s.swot.minacce, "warning", "shield-exclamation"));
}

// ---------------------------------------------------------------------------
// Piano di evoluzione e canali
// ---------------------------------------------------------------------------

function disegnaPiano(s) {
    const ore = s.fasi.reduce((t, f) => t + f.ore, 0);
    const costo = s.fasi.reduce((t, f) => t + f.costo, 0);
    const durata = Math.max(...s.fasi.map(f => f.alMese));
    const settimane = durata * 52 / 12;
    $("totaliPiano").replaceChildren(
        numeroChiave("Durata", `${durata} mesi`, `${s.fasi.length} fasi`, "calendar-range", "col-6 col-md-3"),
        numeroChiave("Lavoro", `${numero.format(ore)} ore`, `≈ ${numero.format(Math.round(ore / settimane))} ore a settimana`, "clock-history", "col-6 col-md-3"),
        numeroChiave("Soldi da investire", euroIntero.format(costo), "marketing, strumenti, servizi", "cash-stack", "col-6 col-md-3"),
        numeroChiave("Utenti obiettivo", numero.format(Math.max(...s.fasi.map(f => f.utentiObiettivo))), "a fine piano", "people", "col-6 col-md-3"));

    $("fasi").replaceChildren(...s.fasi.map((f, i) => {
        const box = el("div", "fase-piano border rounded p-3");
        const testa = el("div", "d-flex flex-wrap justify-content-between align-items-baseline gap-2");
        const nome = el("div");
        nome.append(el("span", "badge rounded-pill text-bg-primary me-2", `Fase ${i + 1}`), el("span", "fw-bold", f.nome));
        testa.append(nome, el("span", "small text-body-secondary", `Mesi ${f.dalMese}–${f.alMese}`));

        const obiettivo = el("div", "small mt-1");
        obiettivo.append(el("span", "fw-semibold", "Obiettivo: "), document.createTextNode(f.obiettivo));

        const attivita = el("ul", "small mb-2 mt-1 ps-3");
        attivita.append(...f.attivita.map(a => el("li", null, a)));

        const numeri = el("div", "d-flex flex-wrap gap-3 small");
        const voce = (nomeIcona, testo) => {
            const s = el("span", "text-nowrap");
            s.append(icona(nomeIcona, "text-body-secondary me-1"), document.createTextNode(testo));
            return s;
        };
        numeri.append(voce("clock", `${numero.format(f.ore)} ore`),
            voce("cash", euroIntero.format(f.costo)),
            voce("people", `${numero.format(f.utentiObiettivo)} utenti`));

        box.append(testa, obiettivo, attivita, numeri);
        return box;
    }));

    $("prossimiPassi").replaceChildren(...s.prossimiPassi.map(p => el("li", null, p)));
}

function disegnaCanali(s) {
    const canali = [...s.canali].sort((a, b) => a.costoPerUtente - b.costoPerUtente);
    $("elencoCanali").replaceChildren(...canali.map(c => {
        const li = el("li", "list-group-item px-3");
        const testa = el("div", "d-flex justify-content-between gap-2");
        testa.append(el("span", "fw-semibold", c.nome),
            el("span", "text-nowrap fw-semibold " + (c.costoPerUtente === 0 ? "text-success" : ""),
                c.costoPerUtente === 0 ? "Gratis" : `${euro.format(c.costoPerUtente)} / utente`));
        li.append(testa, el("div", "small", c.descrizione), el("div", "small text-body-secondary", c.quando));
        return li;
    }));
}

function disegnaFonti(s) {
    $("fonti").replaceChildren(...s.fonti.map(f => {
        const li = el("li");
        const a = el("a", null, f.titolo);
        a.href = f.url;
        a.target = "_blank";
        a.rel = "noopener noreferrer";
        li.appendChild(a);
        return li;
    }));
}

// ---------------------------------------------------------------------------
// Simulazione
// ---------------------------------------------------------------------------

function costruisciCampi() {
    $("parametri").replaceChildren(...CAMPI.map(c => {
        const col = el("div", "col-6 col-md-4");
        const etichettaCampo = el("label", "form-label small fw-semibold mb-1", c.etichetta);
        etichettaCampo.htmlFor = "p_" + c.nome;
        const gruppo = el("div", "input-group input-group-sm");
        const input = el("input", "form-control");
        input.type = "number";
        input.id = "p_" + c.nome;
        input.name = c.nome;
        input.step = c.passo;
        input.min = c.min;
        input.max = c.max;
        input.required = true;
        input.inputMode = c.passo < 1 ? "decimal" : "numeric";
        input.setAttribute("aria-describedby", "a_" + c.nome);
        gruppo.append(input, el("span", "input-group-text", c.unita));
        const aiuto = el("div", "form-text mt-1", c.aiuto);
        aiuto.id = "a_" + c.nome;
        col.append(etichettaCampo, gruppo, aiuto);
        return col;
    }));
}

function riempiCampi(parametri) {
    for (const c of CAMPI) $("p_" + c.nome).value = parametri[c.nome];
}

function leggiCampi() {
    return Object.fromEntries(CAMPI.map(c => [c.nome, Number($("p_" + c.nome).value)]));
}

function statoIpotesi() {
    const s = scheda.salvate;
    const badge = s ? etichetta("Ipotesi salvate", "primary") : etichetta("Ipotesi predefinite", "secondary");
    if (s) {
        const quando = new Date(s.aggiornatoIl).toLocaleDateString("it-IT");
        badge.title = `Salvate il ${quando}${s.aggiornatoDa ? " da " + s.aggiornatoDa : ""}`;
    }
    $("statoIpotesi").replaceChildren(badge);
}

function costruisciAzioni() {
    const azioni = [
        pulsante("Annulla modifiche", "btn btn-sm btn-light border", () => {
            riempiCampi(scheda.parametri);
            $("messaggioParametri").textContent = "";
            disegnaProiezione(scheda.proiezione);
        })
    ];
    if (amministratore) {
        azioni.unshift(pulsante("Salva per tutti", "btn btn-sm btn-primary", salvaParametri));
        azioni.push(pulsante("Ripristina predefinite", "btn btn-sm btn-outline-danger", ripristinaParametri));
    } else {
        azioni.push(el("span", "small text-body-secondary align-self-center",
            "Puoi provare ipotesi diverse; solo l'Amministratore del modulo le salva."));
    }
    $("azioniParametri").replaceChildren(...azioni);
}

function campiValidi() {
    const form = $("formParametri");
    if (form.checkValidity()) return true;
    const sbagliato = CAMPI.find(c => !$("p_" + c.nome).checkValidity());
    mostraMessaggio($("messaggioParametri"),
        `${sbagliato.etichetta}: inserisci un valore tra ${sbagliato.min} e ${numero.format(sbagliato.max)}.`);
    return false;
}

let richiestaCorrente = 0;

async function simula() {
    if (!campiValidi()) return;
    $("messaggioParametri").textContent = "";
    const numeroRichiesta = ++richiestaCorrente;
    $("risultati").classList.add("opacity-50");
    try {
        const response = await api(`/api/mercato/${encodeURIComponent(codiceModulo)}/simulazione`, jsonBody("POST", leggiCampi()));
        if (!response.ok) {
            mostraMessaggio($("messaggioParametri"), await messaggioErrore(response, "Ipotesi non valide."));
            return;
        }
        const risultato = await response.json();
        if (numeroRichiesta === richiestaCorrente) disegnaProiezione(risultato);
    } catch {
        mostraMessaggio($("messaggioParametri"), "Errore nel calcolo della simulazione.");
    } finally {
        if (numeroRichiesta === richiestaCorrente) $("risultati").classList.remove("opacity-50");
    }
}

async function salvaParametri() {
    if (!campiValidi()) return;
    const response = await api(`/api/mercato/${encodeURIComponent(codiceModulo)}/parametri`, jsonBody("PUT", leggiCampi()));
    if (!response.ok) {
        mostraMessaggio($("messaggioParametri"), await messaggioErrore(response, "Salvataggio non riuscito."));
        return;
    }
    await caricaScheda(false);
    mostraMessaggio($("messaggioParametri"), "Ipotesi salvate: valgono per tutti e nel confronto tra moduli.", "successo");
}

async function ripristinaParametri() {
    if (!confirm("Tornare alle ipotesi predefinite? Quelle salvate verranno cancellate.")) return;
    const response = await api(`/api/mercato/${encodeURIComponent(codiceModulo)}/parametri`, { method: "DELETE" });
    if (!response.ok) {
        mostraMessaggio($("messaggioParametri"), await messaggioErrore(response, "Ripristino non riuscito."));
        return;
    }
    await caricaScheda(false);
    mostraMessaggio($("messaggioParametri"), "Ripristinate le ipotesi predefinite.", "successo");
}

function disegnaProiezione(p) {
    const r = p.riepilogo;
    const mesi = p.mesi;
    const usciteMese = mesi.length ? mesi[0].usciteCassa : 0;

    $("risultati").replaceChildren(
        numeroChiave("Utenti a fine periodo", numero.format(r.utentiFinali),
            `${r.quotaMercato.toLocaleString("it-IT")}% del mercato raggiungibile`, "people", "col-6 col-md-4"),
        numeroChiave("Soldi da investire", euroIntero.format(r.totaleUsciteCassa),
            `${euroIntero.format(usciteMese)} al mese per ${mesi.length} mesi`, "cash-stack", "col-6 col-md-4"),
        numeroChiave("Soldi da tenere pronti", euroIntero.format(r.fabbisognoMassimo),
            "la perdita più alta accumulata", "piggy-bank", "col-6 col-md-4"),
        numeroChiave("Ricavi dell'ultimo mese", euro.format(r.ricaviMensiliFinali),
            `${numero.format(r.pagantiFinali)} paganti · totale ${euroIntero.format(r.totaleRicavi)}`, "graph-up", "col-6 col-md-4"),
        numeroChiave("Pareggio", testoMese(r.mesePareggio),
            r.meseRientro ? `rientro dell'investimento al mese ${r.meseRientro}` : "investimento non ancora rientrato",
            "flag", "col-6 col-md-4"),
        numeroChiave("Lavoro", `${numero.format(r.oreTotali)} ore`,
            `valore ${euroIntero.format(r.valoreLavoro)}`, "clock-history", "col-6 col-md-4"));

    disegnaGraficoUtenti(mesi);
    disegnaGraficoSoldi(mesi);

    $("tabellaMesi").replaceChildren(...mesi.filter(m => m.numero % 3 === 0 || m.numero === mesi.length).map(m => {
        const tr = el("tr");
        tr.append(el("td", null, String(m.numero)),
            el("td", "text-end", numero.format(m.utenti)),
            el("td", "text-end", numero.format(m.paganti)),
            el("td", "text-end", euro.format(m.ricavi)),
            el("td", "text-end", euro.format(m.usciteCassa)),
            el("td", "text-end " + (m.saldoCumulato < 0 ? "text-danger" : "text-success"), euro.format(m.saldoCumulato)));
        return tr;
    }));
}

function disegnaGraficoUtenti(mesi) {
    const etichette = mesi.map(m => `Mese ${m.numero}`);
    // Obiettivi delle fasi del piano, alla fine di ogni fase: si vede se la simulazione ci arriva.
    const obiettivi = mesi.map(m => {
        const fase = scheda.scheda.fasi.find(f => f.alMese === m.numero);
        return fase ? fase.utentiObiettivo : null;
    });

    const dati = {
        labels: etichette,
        datasets: [
            {
                type: "line", label: "Utenti", data: mesi.map(m => m.utenti),
                borderColor: COLORI_GRAFICI.primario, backgroundColor: "rgba(42, 120, 214, 0.12)",
                fill: true, tension: 0.25, pointRadius: 0, borderWidth: 2
            },
            {
                type: "line", label: "Paganti", data: mesi.map(m => m.paganti),
                borderColor: COLORI_GRAFICI.verde, pointRadius: 0, borderWidth: 2, tension: 0.25
            },
            {
                type: "line", label: "Obiettivi del piano", data: obiettivi,
                showLine: false, pointRadius: 6, pointHoverRadius: 7, pointStyle: "rectRot",
                borderColor: COLORI_GRAFICI.arancio, backgroundColor: COLORI_GRAFICI.arancio
            }
        ]
    };
    const opzioni = {
        responsive: true,
        maintainAspectRatio: false,
        animation: { duration: 250 },
        interaction: { mode: "index", intersect: false },
        plugins: {
            legend: { position: "bottom", labels: { boxWidth: 12, boxHeight: 12 } },
            tooltip: {
                filter: c => c.parsed.y != null,
                callbacks: { label: c => ` ${c.dataset.label}: ${numero.format(c.parsed.y)}` }
            }
        },
        scales: {
            x: { grid: { display: false }, ticks: { maxRotation: 0, autoSkip: true, autoSkipPadding: 16, callback: v => v + 1 } },
            y: {
                beginAtZero: true, border: { display: false }, grid: { color: COLORI_GRAFICI.griglia },
                ticks: { maxTicksLimit: 5, callback: v => numero.format(v) }
            }
        }
    };
    if (graficoUtenti) graficoUtenti.destroy();
    graficoUtenti = new Chart($("graficoUtenti"), { type: "line", data: dati, options: opzioni });
}

function disegnaGraficoSoldi(mesi) {
    const dati = {
        labels: mesi.map(m => `Mese ${m.numero}`),
        datasets: [
            {
                type: "bar", label: "Ricavi", data: mesi.map(m => m.ricavi),
                backgroundColor: COLORI_GRAFICI.verde, borderRadius: { topLeft: 3, topRight: 3 },
                borderSkipped: "bottom", maxBarThickness: 18, yAxisID: "mese"
            },
            {
                type: "bar", label: "Uscite", data: mesi.map(m => m.usciteCassa),
                backgroundColor: COLORI_GRAFICI.rosso, borderRadius: { topLeft: 3, topRight: 3 },
                borderSkipped: "bottom", maxBarThickness: 18, yAxisID: "mese"
            },
            {
                type: "line", label: "Saldo cumulato", data: mesi.map(m => m.saldoCumulato),
                borderColor: COLORI_GRAFICI.primarioScuro, pointRadius: 0, borderWidth: 2, tension: 0.2, yAxisID: "cumulato"
            }
        ]
    };
    const opzioni = {
        responsive: true,
        maintainAspectRatio: false,
        animation: { duration: 250 },
        interaction: { mode: "index", intersect: false },
        plugins: {
            legend: { position: "bottom", labels: { boxWidth: 12, boxHeight: 12 } },
            tooltip: { callbacks: { label: c => ` ${c.dataset.label}: ${euro.format(c.parsed.y)}` } }
        },
        scales: {
            x: { grid: { display: false }, ticks: { maxRotation: 0, autoSkip: true, autoSkipPadding: 16, callback: v => v + 1 } },
            mese: {
                position: "left", beginAtZero: true, border: { display: false }, grid: { color: COLORI_GRAFICI.griglia },
                ticks: { maxTicksLimit: 5, callback: tickEuro }
            },
            cumulato: {
                position: "right", border: { display: false }, grid: { display: false },
                ticks: { maxTicksLimit: 5, callback: tickEuro }
            }
        }
    };
    if (graficoSoldi) graficoSoldi.destroy();
    graficoSoldi = new Chart($("graficoSoldi"), { type: "bar", data: dati, options: opzioni });
}

// ---------------------------------------------------------------------------
// Caricamento
// ---------------------------------------------------------------------------

async function caricaScheda(primaVolta = true) {
    const response = await api(`/api/mercato/${encodeURIComponent(codiceModulo)}`);
    if (!response.ok) {
        $("caricamento").hidden = true;
        $("errore").hidden = false;
        $("errore").textContent = await messaggioErrore(response, "Errore nel caricamento della scheda.");
        return;
    }
    scheda = await response.json();

    if (primaVolta) {
        const s = scheda.scheda;
        disegnaIntestazione(scheda);
        disegnaPosizionamento(s);
        disegnaClienti(s);
        disegnaConcorrenti(s);
        disegnaPiano(s);
        disegnaCanali(s);
        disegnaFonti(s);
        costruisciCampi();
        costruisciAzioni();

        // Ogni modifica a un campo ricalcola (con una breve attesa se si scrive di seguito).
        let attesa;
        $("formParametri").addEventListener("input", () => { clearTimeout(attesa); attesa = setTimeout(simula, 400); });
        $("formParametri").addEventListener("submit", e => { e.preventDefault(); simula(); });

        $("caricamento").hidden = true;
        $("contenuto").hidden = false;
    }

    riempiCampi(scheda.parametri);
    statoIpotesi();
    disegnaProiezione(scheda.proiezione);
}

if (!codiceModulo) {
    location.href = "/mercato.html";
} else {
    avviaPagina({ modulo: MODULI.mercato })
        .then(u => {
            utente = u;
            amministratore = livello(u, MODULI.mercato) === "Amministratore";
            return caricaScheda();
        })
        .catch(() => {
            $("caricamento").hidden = true;
            $("errore").hidden = false;
            $("errore").textContent = "Errore nel collegamento al server.";
        });
}
