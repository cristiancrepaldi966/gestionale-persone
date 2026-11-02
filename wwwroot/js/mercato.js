// Pagina Confronto moduli (mercato.html): totali, priorità, grafico e tabella di confronto,
// una card per ogni modulo analizzato. Dati da GET /api/mercato.
// Funzioni comuni: componenti.js e js/mercato-comune.js.

const COLORI_MODULI_MERCATO = { finanze: "#2a78d6", fantapolitica: "#6250d6" };

// Utenti raggiunti ogni 1.000 € di uscite di cassa (più alto = più efficiente).
const efficienza = m => m.riepilogo.totaleUsciteCassa > 0
    ? m.riepilogo.utentiFinali / (m.riepilogo.totaleUsciteCassa / 1000)
    : m.riepilogo.utentiFinali;

function disegnaTotali(moduli) {
    const somma = f => moduli.reduce((s, m) => s + f(m), 0);
    $("totali").replaceChildren(
        numeroChiave("Utenti previsti", numero.format(somma(m => m.riepilogo.utentiFinali)),
            "a fine simulazione, tutti i moduli", "people"),
        numeroChiave("Soldi da investire", euroIntero.format(somma(m => m.riepilogo.totaleUsciteCassa)),
            "marketing e costi fissi", "cash-stack"),
        numeroChiave("Soldi da tenere pronti", euroIntero.format(somma(m => m.riepilogo.fabbisognoMassimo)),
            "la perdita più alta prima dei ricavi", "piggy-bank"),
        numeroChiave("Lavoro", `${numero.format(somma(m => m.riepilogo.oreTotali))} ore`,
            `valore ${euroIntero.format(somma(m => m.riepilogo.valoreLavoro))}`, "clock-history"));
}

function disegnaPriorita(moduli) {
    const ordinati = [...moduli].sort((a, b) => efficienza(b) - efficienza(a));
    $("priorita").replaceChildren(...ordinati.map((m, i) => {
        const li = el("li", "list-group-item px-0 d-flex gap-2");
        const testo = el("div", "ms-1 me-auto");
        const titolo = el("a", "fw-semibold", m.nome);
        titolo.href = `/mercato-scheda.html?modulo=${encodeURIComponent(m.modulo)}`;
        const pareggio = m.riepilogo.mesePareggio
            ? `ricavi che coprono le spese dal mese ${m.riepilogo.mesePareggio}`
            : "i ricavi non coprono ancora le spese nel periodo";
        testo.append(titolo, el("div", "small text-body-secondary",
            `${numero.format(Math.round(efficienza(m)))} utenti ogni 1.000 € · ${pareggio}`));
        li.appendChild(testo);
        if (i === 0 && ordinati.length > 1) {
            const badge = etichetta("Priorità", "success");
            badge.classList.add("align-self-center");
            li.appendChild(badge);
        }
        return li;
    }));
}

let grafico = null;

function disegnaGrafico(moduli) {
    $("sottotitoloGrafico").textContent = "Barre: utenti a fine simulazione (asse sinistro) e soldi da investire (asse destro).";
    const dati = {
        labels: moduli.map(m => m.nome),
        datasets: [
            {
                label: "Utenti previsti",
                data: moduli.map(m => m.riepilogo.utentiFinali),
                backgroundColor: COLORI_GRAFICI.primario,
                borderRadius: { topLeft: 4, topRight: 4 },
                borderSkipped: "bottom",
                maxBarThickness: 48,
                yAxisID: "utenti"
            },
            {
                label: "Soldi da investire",
                data: moduli.map(m => m.riepilogo.totaleUsciteCassa),
                backgroundColor: COLORI_GRAFICI.arancio,
                borderRadius: { topLeft: 4, topRight: 4 },
                borderSkipped: "bottom",
                maxBarThickness: 48,
                yAxisID: "euro"
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
                callbacks: {
                    label: c => c.dataset.yAxisID === "euro"
                        ? ` ${c.dataset.label}: ${euroIntero.format(c.parsed.y)}`
                        : ` ${c.dataset.label}: ${numero.format(c.parsed.y)}`
                }
            }
        },
        scales: {
            x: { grid: { display: false } },
            utenti: {
                position: "left", beginAtZero: true, border: { display: false },
                grid: { color: COLORI_GRAFICI.griglia }, ticks: { maxTicksLimit: 5, callback: v => numero.format(v) }
            },
            euro: {
                position: "right", beginAtZero: true, border: { display: false },
                grid: { display: false }, ticks: { maxTicksLimit: 5, callback: tickEuro }
            }
        }
    };
    if (grafico) grafico.destroy();
    grafico = new Chart($("graficoConfronto"), { type: "bar", data: dati, options: opzioni });
}

function disegnaTabella(moduli) {
    const testa = el("tr");
    testa.appendChild(el("th", "ps-3", ""));
    for (const m of moduli) testa.appendChild(el("th", "text-end pe-3", m.nome));
    $("intestazioneConfronto").replaceChildren(testa);

    const righe = [
        ["Mercato raggiungibile", m => persone(m.mercatoRaggiungibile) + " persone"],
        ["Obiettivo a 3 anni", m => numero.format(m.obiettivo) + " utenti"],
        ["Utenti a fine simulazione", m => numero.format(m.riepilogo.utentiFinali)],
        ["Quota del mercato raggiungibile", m => m.riepilogo.quotaMercato.toLocaleString("it-IT") + "%"],
        ["Utenti paganti", m => numero.format(m.riepilogo.pagantiFinali)],
        ["Ricavi dell'ultimo mese", m => euro.format(m.riepilogo.ricaviMensiliFinali)],
        ["Soldi da investire", m => euroIntero.format(m.riepilogo.totaleUsciteCassa)],
        ["Soldi da tenere pronti", m => euroIntero.format(m.riepilogo.fabbisognoMassimo)],
        ["Costo per utente raggiunto", m => euro.format(m.riepilogo.costoPerUtenteFinale)],
        ["Pareggio mensile", m => testoMese(m.riepilogo.mesePareggio)],
        ["Lavoro (simulazione)", m => numero.format(m.riepilogo.oreTotali) + " ore"],
        ["Piano di evoluzione: ore", m => numero.format(m.oreFasi) + " ore"],
        ["Piano di evoluzione: costi", m => euroIntero.format(m.costoFasi)],
        ["Ipotesi", m => m.ipotesiPersonalizzate ? "Salvate" : "Predefinite"],
    ];

    $("tabellaConfronto").replaceChildren(...righe.map(([nome, valore]) => {
        const tr = el("tr");
        tr.appendChild(el("th", "ps-3 fw-normal text-body-secondary", nome));
        for (const m of moduli) tr.appendChild(el("td", "text-end pe-3 text-nowrap", valore(m)));
        return tr;
    }));
}

function cardModulo(m) {
    const def = DEFINIZIONE_MODULI[m.modulo] || { icona: "grid" };
    const col = el("div", "col-md-6");
    const card = el("a", "card card-body shadow-sm border-0 h-100 text-reset text-decoration-none d-flex flex-column gap-2");
    card.href = `/mercato-scheda.html?modulo=${encodeURIComponent(m.modulo)}`;

    const testa = el("div", "d-flex align-items-center gap-3");
    const info = el("div");
    info.append(el("div", "fw-bold fs-5", m.nome), el("div", "small text-body-secondary", m.settore));
    testa.append(iconaCategoria(def.icona, COLORI_MODULI_MERCATO[m.modulo] || "#6c757d"), info);

    const idea = el("p", "small mb-0", m.idea);
    const piede = el("div", "mt-auto d-flex justify-content-between align-items-center small");
    piede.append(el("span", "text-body-secondary",
        `${numero.format(m.riepilogo.utentiFinali)} utenti · ${euroIntero.format(m.riepilogo.totaleUsciteCassa)}`));
    const vai = el("span", "fw-semibold text-primary", "Apri la scheda ");
    vai.appendChild(icona("arrow-right"));
    piede.appendChild(vai);

    card.append(testa, idea, piede);
    col.appendChild(card);
    return col;
}

async function carica() {
    try {
        const response = await api("/api/mercato");
        if (!response.ok) throw new Error();
        const dati = await response.json();

        $("caricamento").hidden = true;
        $("contenuto").hidden = false;

        disegnaTotali(dati.moduli);
        disegnaPriorita(dati.moduli);
        disegnaGrafico(dati.moduli);
        disegnaTabella(dati.moduli);
        $("schede").replaceChildren(...dati.moduli.map(cardModulo));

        if (dati.senzaAnalisi.length > 0) {
            $("senzaAnalisi").hidden = false;
            $("senzaAnalisi").textContent = `Ancora senza analisi: ${dati.senzaAnalisi.join(", ")}. ` +
                "Si aggiungono in Services/SchedeMercato.cs.";
        }
    } catch {
        $("caricamento").hidden = true;
        $("errore").hidden = false;
        $("errore").textContent = "Errore nel caricamento dell'analisi di mercato.";
    }
}

avviaPagina({ modulo: MODULI.mercato }).then(carica);
