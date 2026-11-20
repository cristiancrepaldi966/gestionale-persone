// Pagina La mia squadra del modulo Fantapolitica (fantapolitica-squadra.html): per ogni
// lega di cui faccio parte, la mia squadra (nome e politici comprati con i crediti).
// La squadra si crea una volta sola: salvarla vale come conferma, poi nome e politici
// restano bloccati. Solo il Super admin e l'Amministratore del modulo possono cambiarla
// o eliminarla (anche la propria). I punti di un politico contano dal giorno in cui entra
// in squadra; se esce, i suoi punti vengono tolti.
// Componenti comuni: componenti.js, js/fantapolitica-comune.js e js/fantapolitica-scelta-squadra.js.

const $ = id => document.getElementById(id);

let gestore = false; // posso cambiare la squadra anche dopo la conferma

const scelta = finestraSquadra({ dopoSalvataggio: () => caricaSquadre() });

async function caricaSquadre() {
    try {
        const response = await api(`${API_FANTAPOLITICA}/squadre/mie`);
        if (!response.ok) throw new Error();
        const dati = await response.json();
        gestore = dati.gestioneSquadre;
        $("spiegazione").textContent =
            `In ogni lega di cui fai parte compri una squadra di ${dati.politiciPerSquadra} politici ` +
            `con ${crediti(dati.creditiPerSquadra)}. Scegli bene: una volta confermata non si può più cambiare.`;
        mostraSquadre(dati.leghe, dati);
    } catch {
        $("elencoSquadre").replaceChildren(el("p", "card card-body shadow-sm border-0 mb-0 text-danger",
            "Errore nel caricamento delle squadre."));
    }
}

function mostraSquadre(leghe, dati) {
    if (leghe.length === 0) {
        const vuoto = el("p", "card card-body shadow-sm border-0 mb-0 text-body-secondary");
        vuoto.append(document.createTextNode("Non fai ancora parte di nessuna lega. "));
        const link = el("a", null, "Crea una lega");
        link.href = "/fantapolitica.html";
        vuoto.append(link, document.createTextNode(" o chiedi di essere aggiunto a una."));
        $("elencoSquadre").replaceChildren(vuoto);
        return;
    }
    $("elencoSquadre").replaceChildren(...leghe.map(l => schedaSquadra(l, dati)));
}

function schedaSquadra(lega, { politiciPerSquadra, creditiPerSquadra }) {
    const s = lega.squadra;
    const scheda = el("section", "card shadow-sm border-0");
    const corpo = el("div", "card-body");

    const testa = el("div", "d-flex justify-content-between align-items-start gap-3 flex-wrap");
    const titolo = el("div", "d-flex align-items-center gap-3 min-w-0");
    titolo.appendChild(iconaCategoria(s ? "person-check" : "person-plus", s ? DEFINIZIONE_MODULI.fantapolitica.colore : "#6c757d"));
    const info = el("div", "min-w-0");
    info.appendChild(el("div", "small text-body-secondary", titoloLega(lega.nome)));
    info.appendChild(el("h3", "h5 fw-bold mb-0 text-break", s ? s.nome : "Nessuna squadra"));
    if (s) {
        info.appendChild(el("div", "small text-body-secondary",
            `Spesi ${s.spesi} di ${crediti(creditiPerSquadra)} · ne restano ${creditiPerSquadra - s.spesi}`));
        const etichette = el("div", "d-flex flex-wrap gap-1 mt-1");
        const bloccata = el("span", "badge rounded-pill text-bg-secondary fw-normal d-inline-flex align-items-center gap-1");
        bloccata.append(iconaBi("lock-fill"), document.createTextNode("Squadra confermata"));
        etichette.append(etichettaPunti(s.punti), bloccata);
        info.appendChild(etichette);
    }
    titolo.appendChild(info);
    testa.appendChild(titolo);

    const url = `${API_FANTAPOLITICA}/leghe/${lega.id}/squadra`;
    const azioni = el("div", "d-flex gap-2 flex-shrink-0");
    if (!s) {
        azioni.appendChild(pulsante("Crea la squadra", "btn btn-sm btn-primary",
            () => scelta.apri({ legaNome: lega.nome, squadra: null, url, conferma: !gestore })));
    } else if (gestore) {
        azioni.appendChild(pulsante("Modifica", "btn btn-sm btn-light border",
            () => scelta.apri({ legaNome: lega.nome, squadra: s, url })));
        azioni.appendChild(pulsante("Elimina", "btn btn-sm btn-danger", () => elimina(lega)));
    }
    testa.appendChild(azioni);
    corpo.appendChild(testa);

    if (s) {
        const lista = elencoPoliticiSquadra(s.politici);
        lista.classList.add("mt-3");
        corpo.appendChild(lista);
        if (s.politici.length < politiciPerSquadra) {
            corpo.appendChild(el("div", "small text-danger mt-2",
                `Squadra incompleta: mancano ${politiciPerSquadra - s.politici.length} politici ` +
                "(qualcuno è stato tolto dall'elenco). Chiedi all'amministratore di completarla."));
        }
    }

    scheda.appendChild(corpo);
    return scheda;
}

async function elimina(lega) {
    if (!confirm(`Eliminare la squadra "${lega.squadra.nome}" (${titoloLega(lega.nome)})?`)) return;

    try {
        const response = await api(`${API_FANTAPOLITICA}/leghe/${lega.id}/squadra`, { method: "DELETE" });
        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'eliminazione."));
            return;
        }
        await caricaSquadre();
    } catch {
        alert("Errore nel collegamento al server.");
    }
}

// Modulo Fantapolitica, qualsiasi livello.
avviaPagina({ modulo: MODULI.fantapolitica })
    .then(() => scelta.prepara().catch(() => { }))
    .then(caricaSquadre);
