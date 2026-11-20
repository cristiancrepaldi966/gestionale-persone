// Finestra per creare o modificare una squadra (nome e politici entro i crediti), usata da
// La mia squadra e, per i gestori, dalla pagina Leghe. Da caricare dopo componenti.js e
// js/fantapolitica-comune.js. La finestra viene aggiunta alla pagina da qui.
//
// const scelta = finestraSquadra({ dopoSalvataggio: () => ricarica() });
// await scelta.prepara();   // politici e crediti dal server
// scelta.apri({ legaId, legaNome, squadra, url, perChi, conferma });
//   url:      dove salvare (PUT)
//   perChi:   nome della persona, se la squadra non è la mia (gestori)
//   conferma: chiedere conferma prima di salvare (squadra che poi resta bloccata)

function finestraSquadra({ dopoSalvataggio }) {

    const dialog = el("dialog", "finestra");
    dialog.innerHTML = `
        <form method="dialog" novalidate>
            <h2 class="h4 fw-bold mb-1" tabindex="-1" autofocus>Nuova squadra</h2>
            <p class="small text-body-secondary mb-3" data-ref="sottotitolo"></p>
            <label for="sq-nome" class="form-label fw-semibold">Nome della squadra</label>
            <input type="text" id="sq-nome" class="form-control" maxlength="50" autocomplete="off">
            <div class="d-flex justify-content-between align-items-baseline mt-3 mb-2">
                <span class="fw-semibold" id="sq-etichetta">Politici</span>
                <span class="small" data-ref="conta"></span>
            </div>
            <div class="list-group overflow-auto scelta-membri" data-ref="scelta" role="group" aria-labelledby="sq-etichetta"></div>
            <div class="alert alert-warning small py-2 mt-3 mb-0" data-ref="avviso" hidden>
                <i class="bi bi-lock" aria-hidden="true"></i>
                Dopo il salvataggio la squadra è confermata: nome e politici non si potranno più cambiare.
            </div>
            <div class="small mt-2 text-danger" data-ref="errore"></div>
            <div class="d-flex justify-content-end gap-2 mt-4">
                <button type="button" class="btn btn-light" data-ref="annulla">Annulla</button>
                <button type="submit" class="btn btn-primary px-4">Salva</button>
            </div>
        </form>`;
    document.body.appendChild(dialog);

    const ref = nome => dialog.querySelector(`[data-ref="${nome}"]`);
    const campoNome = dialog.querySelector("#sq-nome");
    const finestra = moduloFinestra(dialog, { primoCampo: campoNome });

    let politici = [];
    let politiciPerSquadra = 5;
    let creditiPerSquadra = 100;
    let aperta = null;            // { url, conferma }
    let prezziPagati = new Map(); // politici già in squadra → prezzo pagato

    // Quanto costa un politico per la squadra aperta (chi c'è già resta al prezzo pagato).
    const prezzo = p => prezziPagati.has(p.id) ? prezziPagati.get(p.id) : p.costo;

    const caselle = () => [...ref("scelta").querySelectorAll('input[type="checkbox"]')];
    const scelti = () => caselle().filter(c => c.checked).map(c => Number(c.value));
    const spesa = ids => ids.reduce((totale, id) => totale + prezzo(politici.find(p => p.id === id)), 0);

    // Si bloccano i politici che non si possono più prendere: squadra completa o crediti finiti.
    function aggiorna() {
        const ids = scelti();
        const restano = creditiPerSquadra - spesa(ids);
        const completa = ids.length >= politiciPerSquadra;
        for (const c of caselle()) {
            const p = politici.find(x => x.id === Number(c.value));
            c.disabled = !c.checked && (completa || prezzo(p) > restano);
            c.closest("label").classList.toggle("text-body-tertiary", c.disabled);
        }
        const conta = ref("conta");
        conta.textContent = `${ids.length} di ${politiciPerSquadra} · restano ${crediti(restano)}`;
        conta.className = "small " + (restano < 0 ? "text-danger fw-semibold"
            : ids.length === politiciPerSquadra ? "text-success fw-semibold" : "text-body-secondary");
    }

    async function salva() {
        const errore = ref("errore");
        const nome = campoNome.value.trim();
        const ids = scelti();

        if (!nome) { errore.textContent = "Inserisci il nome della squadra."; return; }
        if (ids.length !== politiciPerSquadra) {
            errore.textContent = `Scegli ${politiciPerSquadra} politici (ne hai scelti ${ids.length}).`;
            return;
        }
        if (spesa(ids) > creditiPerSquadra) {
            errore.textContent = `Hai superato i crediti: la squadra costa ${spesa(ids)} su ${creditiPerSquadra}.`;
            return;
        }
        if (aperta.conferma && !confirm("Confermi la squadra? Dopo non potrai più cambiare nome e politici.")) return;

        try {
            const response = await api(aperta.url, jsonBody("PUT", { nome, politici: ids }));
            if (!response.ok) {
                errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
                return;
            }
            finestra.chiudi();
            await dopoSalvataggio();
        } catch {
            errore.textContent = "Errore nel collegamento al server.";
        }
    }

    ref("annulla").addEventListener("click", () => finestra.chiudi());
    ref("scelta").addEventListener("change", aggiorna);
    alInvio(dialog.querySelector("form"), salva);

    return {
        async prepara() {
            const [rPolitici, rImpostazioni] = await Promise.all([
                api(`${API_FANTAPOLITICA}/politici`), api(`${API_FANTAPOLITICA}/impostazioni`)]);
            if (rPolitici.ok) politici = await rPolitici.json();
            if (rImpostazioni.ok) ({ politiciPerSquadra, creditiPerSquadra } = await rImpostazioni.json());
        },

        apri({ legaNome, squadra, url, perChi = null, conferma = false }) {
            aperta = { url, conferma };
            prezziPagati = new Map(squadra ? squadra.politici.map(p => [p.id, p.prezzo]) : []);
            ref("sottotitolo").textContent = titoloLega(legaNome) + (perChi ? ` · squadra di ${perChi}` : "");
            campoNome.value = squadra ? squadra.nome : "";
            ref("errore").textContent = "";
            ref("avviso").hidden = !conferma;

            const giaScelti = new Set(squadra ? squadra.politici.map(p => p.id) : []);
            ref("scelta").replaceChildren(...politici.map(p => {
                const voce = el("label", "list-group-item d-flex align-items-center gap-2");
                const casella = el("input", "form-check-input m-0 flex-shrink-0");
                Object.assign(casella, { type: "checkbox", value: p.id });
                casella.checked = giaScelti.has(p.id);
                const testo = el("span", "me-auto min-w-0");
                testo.append(el("span", "d-block", nomePolitico(p)));
                if (p.partito) testo.append(el("span", "d-block small text-body-secondary", p.partito));
                testo.append(el("span", "d-block small text-body-secondary", `${punti(p.punti)} in totale`));
                const destra = el("span", "d-flex flex-column align-items-end gap-1 flex-shrink-0");
                destra.append(etichettaRuolo(p.ruolo), etichettaCrediti(prezzo(p)));
                if (prezziPagati.has(p.id) && prezzo(p) !== p.costo) {
                    destra.appendChild(el("span", "small text-body-secondary", `pagato; oggi ${crediti(p.costo)}`));
                }
                voce.append(casella, testo, destra);
                return voce;
            }));
            aggiorna();

            finestra.apri(squadra ? "Modifica squadra" : "Nuova squadra");
        },

        get politiciPerSquadra() { return politiciPerSquadra; },
        get creditiPerSquadra() { return creditiPerSquadra; },
    };
}
