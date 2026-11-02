// Pagina Dashboard (dashboard.html): grafico delle spese per data e torta per categoria.
// Grafici: Chart.js (lib/chartjs). Componenti e funzioni comuni: componenti.js.

const $ = id => document.getElementById(id);

// Ogni categoria ha il suo colore e la sua icona (scelti in Admin → Categorie):
// il colore segue la categoria, non la sua posizione, quindi filtrando non cambia.
const COLORE_ALTRE = "#a3a29b";     // fetta "Altre" (categorie minori)
const COLORE_BARRE = "#2a78d6";
const SUPERFICIE = "#ffffff";
const TESTO_SECONDARIO = "#6c757d";
const GRIGLIA = "#e9ecef";
const MAX_FETTE = 6;                // oltre: le minori finiscono in "Altre"

let graficoDate = null;
let graficoCategorie = null;

Chart.defaults.font.family = getComputedStyle(document.body).fontFamily;
Chart.defaults.color = TESTO_SECONDARIO;

const ricerca = pannelloRicerca({
    bottone: $("cerca"),
    pannello: $("pannelloFiltri"),
    pallino: $("pallinoFiltri"),
    azzera: $("azzera"),
    campi: [$("filtroCategoria"), $("dal"), $("al")],
    applica: () => caricaRiepilogo()
});

async function caricaCategorie() {
    const response = await api("/api/categorie");
    const categorie = await response.json();
    riempiCategorie($("filtroCategoria"), categorie, "Categoria");
}

// ---------------------------------------------------------------------------
// Date: giorno per giorno fino a 2 mesi, altrimenti mese per mese
// ---------------------------------------------------------------------------

const daIso = iso => { const [a, m, g] = iso.split("-").map(Number); return new Date(a, m - 1, g); };
const aIso = d => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
const nomeMese = new Intl.DateTimeFormat("it-IT", { month: "short", year: "numeric" });
const nomeGiorno = new Intl.DateTimeFormat("it-IT", { day: "numeric", month: "short" });

function serieDate(perGiorno, filtri) {
    const inizio = daIso(filtri.dal || perGiorno[0].data);
    const fine = daIso(filtri.al || perGiorno[perGiorno.length - 1].data);
    const giorni = Math.round((fine - inizio) / 86400000) + 1;
    const mensile = giorni > 62;

    const totali = new Map();
    for (const g of perGiorno) {
        const chiave = mensile ? g.data.slice(0, 7) : g.data;
        totali.set(chiave, (totali.get(chiave) || 0) + g.totale);
    }

    // Tutti i periodi dell'intervallo, anche quelli senza spese (a zero).
    const punti = [];
    if (mensile) {
        const d = new Date(inizio.getFullYear(), inizio.getMonth(), 1);
        while (d <= fine) {
            const chiave = aIso(d).slice(0, 7);
            punti.push({ etichetta: nomeMese.format(d), valore: totali.get(chiave) || 0 });
            d.setMonth(d.getMonth() + 1);
        }
    } else {
        for (const d = new Date(inizio); d <= fine; d.setDate(d.getDate() + 1)) {
            punti.push({ etichetta: nomeGiorno.format(d), dataCompleta: formatoData(aIso(d)), valore: totali.get(aIso(d)) || 0 });
        }
    }

    return { mensile, punti };
}

function disegnaDate(perGiorno, filtri) {

    const { mensile, punti } = serieDate(perGiorno, filtri);
    $("sottotitoloDate").textContent = mensile ? "Totale per mese" : "Totale per giorno";
    $("intestazionePeriodo").textContent = mensile ? "Mese" : "Giorno";

    // Tabella (alternativa accessibile al grafico): solo i periodi con spese.
    $("tabellaDate").replaceChildren(...punti.filter(p => p.valore > 0).map(p => {
        const tr = el("tr");
        tr.append(el("td", null, p.dataCompleta || p.etichetta), el("td", "text-end", euro.format(p.valore)));
        return tr;
    }));

    const dati = {
        labels: punti.map(p => p.etichetta),
        datasets: [{
            label: "Speso",
            data: punti.map(p => p.valore),
            backgroundColor: COLORE_BARRE,
            hoverBackgroundColor: "#1c5cab",
            borderRadius: { topLeft: 4, topRight: 4 },
            borderSkipped: "bottom",
            categoryPercentage: 0.85,
            barPercentage: 0.9,
            maxBarThickness: 36
        }]
    };

    const opzioni = {
        responsive: true,
        maintainAspectRatio: false,
        animation: { duration: 250 },
        interaction: { mode: "index", intersect: false },
        plugins: {
            legend: { display: false },
            tooltip: {
                callbacks: {
                    title: el => punti[el[0].dataIndex].dataCompleta || punti[el[0].dataIndex].etichetta,
                    label: c => " " + euro.format(c.parsed.y)
                }
            }
        },
        scales: {
            x: { grid: { display: false }, ticks: { maxRotation: 0, autoSkip: true, autoSkipPadding: 12 } },
            y: {
                beginAtZero: true,
                border: { display: false },
                grid: { color: GRIGLIA },
                ticks: { maxTicksLimit: 5, callback: v => euro.format(v).replace(/,00\s€$/, " €") }
            }
        }
    };

    if (graficoDate) {
        graficoDate.data = dati;
        graficoDate.options = opzioni;
        graficoDate.update();
    } else {
        graficoDate = new Chart($("graficoDate"), { type: "bar", data: dati, options: opzioni });
    }
}

// ---------------------------------------------------------------------------
// Torta per categoria (al massimo 6 fette) + elenco con importi e percentuali
// ---------------------------------------------------------------------------

function fetteCategorie(perCategoria) {
    const fetta = c => ({ nome: c.categoria, totale: c.totale, colore: c.colore, icona: c.icona });
    if (perCategoria.length <= MAX_FETTE) return perCategoria.map(fetta);
    const altre = perCategoria.slice(MAX_FETTE - 1);
    return [
        ...perCategoria.slice(0, MAX_FETTE - 1).map(fetta),
        { nome: `Altre (${altre.length})`, totale: altre.reduce((s, c) => s + c.totale, 0), colore: COLORE_ALTRE, icona: "three-dots" }
    ];
}

function disegnaCategorie(perCategoria, totale) {

    const fette = fetteCategorie(perCategoria);
    const percentuale = v => totale > 0 ? Math.round(v / totale * 1000) / 10 : 0;
    const formatoPerc = v => percentuale(v).toLocaleString("it-IT") + "%";

    // Elenco accanto alla torta: nome, importo, percentuale (non solo il colore).
    $("legendaCategorie").replaceChildren(...fette.map(f => {
        const li = el("li", "list-group-item d-flex align-items-center gap-2 px-0");
        li.append(iconaCategoria(f.icona, f.colore, true),
            el("span", "me-auto text-break", f.nome),
            el("span", "fw-semibold text-nowrap", euro.format(f.totale)),
            el("span", "text-body-secondary small text-nowrap text-end", formatoPerc(f.totale)));
        li.lastChild.style.minWidth = "3.2rem";
        return li;
    }));

    // Una sola categoria: una torta di una fetta non dice niente, basta l'elenco.
    $("contenitoreTorta").hidden = fette.length < 2;
    if (fette.length < 2) {
        if (graficoCategorie) { graficoCategorie.destroy(); graficoCategorie = null; }
        return;
    }

    const dati = {
        labels: fette.map(f => f.nome),
        datasets: [{
            data: fette.map(f => f.totale),
            backgroundColor: fette.map(f => f.colore),
            borderColor: SUPERFICIE,
            borderWidth: 2,
            hoverOffset: 6
        }]
    };

    const opzioni = {
        responsive: true,
        maintainAspectRatio: true,
        animation: { duration: 250 },
        layout: { padding: 6 },
        plugins: {
            legend: { display: false },
            tooltip: {
                callbacks: {
                    label: c => ` ${euro.format(c.parsed)} · ${formatoPerc(c.parsed)}`
                }
            }
        }
    };

    if (graficoCategorie) {
        graficoCategorie.data = dati;
        graficoCategorie.options = opzioni;
        graficoCategorie.update();
    } else {
        graficoCategorie = new Chart($("graficoCategorie"), { type: "pie", data: dati, options: opzioni });
    }
}

// ---------------------------------------------------------------------------
// Caricamento
// ---------------------------------------------------------------------------

let richiestaCorrente = 0;

async function caricaRiepilogo() {

    const numeroRichiesta = ++richiestaCorrente;
    const filtri = ricerca.valori();
    const primaVolta = !$("graficiCaricamento").hidden;

    if (!primaVolta) {
        $("grafici").classList.add("opacity-50");
        $("cerchiettoAggiornamento").hidden = false;
    }

    try {
        const response = await api(`/api/uscite/riepilogo?${parametriUscite(filtri)}`);
        if (!response.ok) throw new Error();
        const r = await response.json();
        if (numeroRichiesta !== richiestaCorrente) return;

        $("riepilogo").replaceChildren(
            el("span", "text-body-secondary", `${r.numero} ${r.numero === 1 ? "uscita" : "uscite"}`),
            el("strong", "fs-5", `Totale ${euro.format(r.totale)}`));

        $("graficiCaricamento").hidden = true;

        if (r.numero === 0) {
            $("grafici").hidden = true;
            $("nessunDato").hidden = false;
            $("nessunDato").textContent = ricerca.attivi()
                ? "Nessuna uscita corrisponde alla ricerca."
                : "Nessuna uscita inserita: i grafici compariranno dopo il primo inserimento.";
            return;
        }

        $("nessunDato").hidden = true;
        $("grafici").hidden = false;
        disegnaDate(r.perGiorno, filtri);
        disegnaCategorie(r.perCategoria, r.totale);

    } catch {
        if (numeroRichiesta === richiestaCorrente) {
            $("graficiCaricamento").hidden = true;
            $("grafici").hidden = true;
            $("nessunDato").hidden = false;
            $("nessunDato").textContent = "Errore nel caricamento dei dati.";
        }
    } finally {
        if (numeroRichiesta === richiestaCorrente) {
            $("grafici").classList.remove("opacity-50");
            $("cerchiettoAggiornamento").hidden = true;
        }
    }
}

carosello($("carosello"));

avviaPagina({ modulo: MODULI.uscite })
    .then(caricaCategorie)
    .then(caricaRiepilogo)
    .catch(() => {
        $("graficiCaricamento").hidden = true;
        $("nessunDato").hidden = false;
        $("nessunDato").textContent = "Errore nel collegamento al server.";
    });
