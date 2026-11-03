// Configuratore del modulo Stampa 3D (stampa3d-configuratore.html):
//   ?oggetto=codice → nuovo oggetto dal catalogo
//   ?progetto=id    → progetto salvato da modificare
// Le misure si regolano nel modulo; il server genera l'STL (POST /api/stampa3d/stl),
// mostrato in anteprima 3D e scaricabile. Il progetto si salva con /api/stampa3d/progetti.
// Componenti e funzioni comuni: componenti.js (script classico, caricato prima).

import { Anteprima } from "./stampa3d-anteprima.js";

const $ = id => document.getElementById(id);
const parametriPagina = new URLSearchParams(location.search);

let oggetto = null;        // voce del catalogo
let progetto = null;       // progetto aperto (null = nuovo)
let ultimoStl = null;      // ArrayBuffer dell'ultima generazione riuscita
let anteprima = null;
let richiestaCorrente = 0;
let attesa;

const numero = new Intl.NumberFormat("it-IT", { maximumFractionDigits: 1 });

// ---------------------------------------------------------------------------
// Campi delle misure
// ---------------------------------------------------------------------------

function campo(p) {
    const id = "p_" + p.nome;
    const gruppo = el("div");

    if (p.tipo === "si-no") {
        const riga = el("div", "form-check form-switch");
        const input = el("input", "form-check-input");
        input.type = "checkbox";
        input.id = id;
        input.setAttribute("role", "switch");
        const etichettaCampo = el("label", "form-check-label small fw-semibold", p.etichetta);
        etichettaCampo.htmlFor = id;
        riga.append(input, etichettaCampo);
        gruppo.appendChild(riga);
    } else {
        const etichettaCampo = el("label", "form-label small fw-semibold mb-1", p.etichetta);
        etichettaCampo.htmlFor = id;
        let input;
        if (p.tipo === "scelta") {
            input = el("select", "form-select");
            for (const s of p.scelte) input.appendChild(new Option(s.etichetta, s.valore));
        } else {
            input = el("input", "form-control");
            if (p.tipo === "numero") {
                input.type = "number";
                input.min = p.min;
                input.max = p.max;
                input.step = p.passo ?? "any";
                input.inputMode = "decimal";
                input.required = true;
            } else {
                input.type = "text";
                input.maxLength = p.maxLunghezza;
                input.autocomplete = "off";
            }
        }
        input.id = id;

        if (p.tipo === "numero" && p.unita) {
            const ig = el("div", "input-group");
            ig.append(input, el("span", "input-group-text", p.unita));
            gruppo.append(etichettaCampo, ig);
        } else {
            gruppo.append(etichettaCampo, input);
        }
    }

    const aiuto = p.tipo === "numero"
        ? `Da ${numero.format(p.min)} a ${numero.format(p.max)}${p.unita ? " " + p.unita : ""}.${p.aiuto ? " " + p.aiuto : ""}`
        : p.aiuto;
    if (aiuto) gruppo.appendChild(el("div", "form-text", aiuto));
    return gruppo;
}

function imposta(valori) {
    for (const p of oggetto.parametri) {
        const input = $("p_" + p.nome);
        const v = valori[p.nome] ?? p.predefinito;
        if (p.tipo === "si-no") input.checked = !!v;
        else input.value = v;
    }
}

function predefiniti() {
    return Object.fromEntries(oggetto.parametri.map(p => [p.nome, p.predefinito]));
}

function leggi() {
    return Object.fromEntries(oggetto.parametri.map(p => {
        const input = $("p_" + p.nome);
        if (p.tipo === "si-no") return [p.nome, input.checked];
        if (p.tipo === "numero") return [p.nome, input.value === "" ? null : Number(input.value)];
        return [p.nome, input.value.trim()];
    }));
}

// Primo campo non valido (controllo del browser), con un messaggio chiaro.
function erroreCampi() {
    for (const p of oggetto.parametri) {
        const input = $("p_" + p.nome);
        if (input.checkValidity()) continue;
        return p.tipo === "numero"
            ? `${p.etichetta}: inserisci un valore tra ${numero.format(p.min)} e ${numero.format(p.max)}.`
            : `${p.etichetta}: valore non valido.`;
    }
    return null;
}

// ---------------------------------------------------------------------------
// Anteprima e download
// ---------------------------------------------------------------------------

function statoAnteprima(testo, inCorso) {
    const stato = $("statoAnteprima");
    stato.hidden = !testo;
    if (!testo) return;
    const figli = [el("span", null, testo)];
    if (inCorso) figli.unshift(cerchietto());
    stato.replaceChildren(...figli);
}

async function aggiornaAnteprima({ reinquadra = false } = {}) {
    const errore = erroreCampi();
    if (errore) {
        mostraMessaggio($("messaggioAnteprima"), errore);
        return;
    }
    $("messaggioAnteprima").textContent = "";

    const numeroRichiesta = ++richiestaCorrente;
    statoAnteprima("Generazione del modello...", true);
    $("scarica").disabled = true;

    try {
        const response = await api("/api/stampa3d/stl",
            jsonBody("POST", { oggetto: oggetto.codice, parametri: leggi() }));
        if (numeroRichiesta !== richiestaCorrente) return;
        if (!response.ok) {
            statoAnteprima(ultimoStl ? "" : "Anteprima non disponibile", false);
            mostraMessaggio($("messaggioAnteprima"), await messaggioErrore(response, "Generazione non riuscita."));
            $("scarica").disabled = !ultimoStl;
            return;
        }
        const buffer = await response.arrayBuffer();
        if (numeroRichiesta !== richiestaCorrente) return;

        ultimoStl = buffer;
        const d = anteprima.mostra(buffer, { reinquadra });
        $("dimensioni").textContent = `Ingombro ${numero.format(d.x)} × ${numero.format(d.y)} × ${numero.format(d.z)} mm`;
        statoAnteprima("", false);
        $("scarica").disabled = false;
    } catch {
        if (numeroRichiesta === richiestaCorrente) {
            statoAnteprima(ultimoStl ? "" : "Anteprima non disponibile", false);
            mostraMessaggio($("messaggioAnteprima"), "Errore nel collegamento al server.");
        }
    }
}

function nomeFile() {
    const base = ($("nomeProgetto").value.trim() || oggetto.codice)
        .normalize("NFD").replace(/[̀-ͯ]/g, "")
        .replace(/[^A-Za-z0-9-_]+/g, "-").replace(/^-+|-+$/g, "").toLowerCase();
    return (base || oggetto.codice) + ".stl";
}

function scarica() {
    if (!ultimoStl) return;
    const url = URL.createObjectURL(new Blob([ultimoStl], { type: "model/stl" }));
    const a = el("a");
    a.href = url;
    a.download = nomeFile();
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// ---------------------------------------------------------------------------
// Progetto
// ---------------------------------------------------------------------------

function aggiornaAzioniProgetto() {
    $("titoloProgetto").textContent = progetto ? "Progetto" : "Salva come progetto";
    $("salva").textContent = progetto ? "Salva modifiche" : "Salva progetto";
    const azioni = [$("salva")];
    if (progetto) {
        azioni.push(pulsante("Salva come nuovo", "btn btn-light border", () => salva(true)));
    }
    $("azioniProgetto").replaceChildren(...azioni);
}

async function salva(comeNuovo = false) {
    const messaggio = $("messaggioProgetto");
    const nome = $("nomeProgetto").value.trim();
    if (!nome) {
        mostraMessaggio(messaggio, "Scrivi il nome del progetto.");
        $("nomeProgetto").focus();
        return;
    }
    const errore = erroreCampi();
    if (errore) {
        mostraMessaggio(messaggio, errore);
        return;
    }

    const dati = { nome, oggetto: oggetto.codice, parametri: leggi() };
    const nuovo = !progetto || comeNuovo;
    const response = await api(nuovo ? "/api/stampa3d/progetti" : `/api/stampa3d/progetti/${progetto.id}`,
        jsonBody(nuovo ? "POST" : "PUT", dati));
    if (!response.ok) {
        mostraMessaggio(messaggio, await messaggioErrore(response, "Salvataggio non riuscito."));
        return;
    }

    if (nuovo) {
        progetto = await response.json();
        history.replaceState(null, "", `/stampa3d-configuratore.html?progetto=${progetto.id}`);
        aggiornaAzioniProgetto();
    }
    mostraMessaggio(messaggio, nuovo ? "Progetto salvato. Lo trovi in Progetti." : "Modifiche salvate.", "successo");
}

// ---------------------------------------------------------------------------
// Avvio
// ---------------------------------------------------------------------------

function mostraErrore(testo) {
    $("caricamento").hidden = true;
    $("errore").hidden = false;
    $("errore").textContent = testo;
}

async function carica() {
    const catalogo = await (await api("/api/stampa3d/catalogo")).json();

    let valori = null;
    const idProgetto = parametriPagina.get("progetto");
    if (idProgetto) {
        const response = await api(`/api/stampa3d/progetti/${encodeURIComponent(idProgetto)}`);
        if (!response.ok) return mostraErrore(await messaggioErrore(response, "Progetto non trovato."));
        progetto = await response.json();
        valori = progetto.parametri;
    }

    const codice = progetto ? progetto.oggetto : parametriPagina.get("oggetto");
    oggetto = catalogo.find(o => o.codice === codice);
    if (!oggetto) return mostraErrore("Oggetto non presente nel catalogo.");

    document.title = `${oggetto.nome} - Stampa 3D`;
    $("percorsoNome").textContent = progetto ? progetto.nome : oggetto.nome;
    $("titolo").replaceChildren(iconaCategoria(oggetto.icona, "#eb6834"),
        el("h2", "h3 fw-bold mb-0", progetto ? progetto.nome : oggetto.nome));
    $("descrizione").textContent = progetto ? `${oggetto.nome} · ${oggetto.descrizione}` : oggetto.descrizione;
    if (progetto) $("nomeProgetto").value = progetto.nome;

    $("campi").replaceChildren(...oggetto.parametri.map(campo));
    imposta(valori || predefiniti());
    aggiornaAzioniProgetto();

    $("caricamento").hidden = true;
    $("contenuto").hidden = false;

    anteprima = new Anteprima($("anteprima"));

    // Ogni modifica rigenera l'anteprima, con una breve attesa mentre si scrive.
    $("formMisure").addEventListener("input", () => {
        clearTimeout(attesa);
        attesa = setTimeout(aggiornaAnteprima, 700);
    });
    $("formMisure").addEventListener("submit", e => { e.preventDefault(); aggiornaAnteprima(); });
    $("ripristina").addEventListener("click", () => {
        imposta(predefiniti());
        aggiornaAnteprima({ reinquadra: true });
    });
    $("scarica").addEventListener("click", scarica);
    alInvio($("formProgetto"), () => salva(false));

    await aggiornaAnteprima({ reinquadra: true });
}

avviaPagina({ modulo: MODULI.stampa3d })
    .then(carica)
    .catch(() => mostraErrore("Errore nel collegamento al server."));
