// Pagina IA 3D del modulo IA (ia3d.html): si descrive un pezzo a parole e il server ne
// riconosce forma e misure (POST /api/ia/3d/riconosci). Se manca qualcosa di essenziale fa
// domande, una alla volta: la pagina rimanda ogni volta la descrizione con tutte le risposte.
// Poi mostra cosa ha capito, il disegno tecnico con le quote (SVG costruito qui) e l'anteprima
// 3D del file STL (POST /api/ia/3d/stl), e chiede "È il pezzo che volevi?": con "Sì" si può
// scaricare l'STL, con "Correggo" si cambiano le misure a mano (POST /api/ia/3d/pezzo).
// Componenti e funzioni comuni: componenti.js (script classico, caricato prima).

import { Anteprima } from "./stampa3d-anteprima.js";

const $ = id => document.getElementById(id);
const numero = new Intl.NumberFormat("it-IT", { maximumFractionDigits: 2 });
const mm = v => numero.format(v);

const ESEMPI = [
    "piastra 100x60 spessore 4 con 4 fori da 5 agli angoli",
    "cilindro diametro 30 alto 50 con un foro centrale da 8",
    "tubo diametro 20 interno 16 lungo 100",
    "cubo 20 con angoli arrotondati",
    "scatolina 50x30 con un buco",
    "sfera 4 cm",
];

let forme = [];             // forme e campi della correzione (GET /api/ia/3d/forme)
let testo = "";             // descrizione in corso
let risposte = {};          // codice della domanda → risposta data
let storia = [];            // domande già fatte: { domanda, risposta }
let riepilogo = null;       // cosa è stato capito della descrizione (capito, non capito, avvisi)
let pezzo = null;           // pezzo mostrato: parametri, fori, ingombro, misure
let ultimoStl = null;       // ArrayBuffer del suo STL
let confermato = false;
let anteprima = null;
let richiestaCorrente = 0;

const icona = nome => {
    const i = el("i", "bi bi-" + nome);
    i.setAttribute("aria-hidden", "true");
    return i;
};

// ---------------------------------------------------------------------------
// Riconoscimento e domande
// ---------------------------------------------------------------------------

// Nuova descrizione: si riparte da zero con le domande.
function nuovaDescrizione() {
    $("messaggio").textContent = "";
    testo = $("descrizione").value.trim();
    if (!testo) {
        mostraMessaggio($("messaggio"), "Descrivi il pezzo da creare.");
        $("descrizione").focus();
        return;
    }
    risposte = {};
    storia = [];
    return chiedi();
}

async function chiedi() {
    const numeroRichiesta = ++richiestaCorrente;
    let response;
    try {
        response = await api("/api/ia/3d/riconosci", jsonBody("POST", { testo, risposte }));
    } catch {
        if (numeroRichiesta === richiestaCorrente) mostraMessaggio($("messaggio"), "Errore nel collegamento al server.");
        return;
    }
    if (numeroRichiesta !== richiestaCorrente) return;

    if (!response.ok) {
        nascondiTutto();
        mostraMessaggio($("messaggio"), await messaggioErrore(response, "Non sono riuscito a riconoscere il pezzo."));
        return;
    }
    const r = await response.json();
    if (numeroRichiesta !== richiestaCorrente) return;

    if (r.domande.length > 0) {
        mostraDomanda(r);
        return;
    }

    $("dialogo").hidden = true;
    riepilogo = { capito: r.capito, nonCapito: r.nonCapito, avvisi: r.avvisi };
    if (r.errori.length > 0) {
        // Misure impossibili: si correggono a mano partendo da quelle capite.
        mostraRiepilogo(r.nomeForma, r.misure);
        $("vista").hidden = true;
        $("conferma").hidden = true;
        apriCorrezione(r.parametri, r.errori.join(" "));
        return;
    }
    mostraPezzo(r);
}

function nascondiTutto() {
    $("dialogo").hidden = true;
    $("risultato").hidden = true;
}

// Una domanda alla volta, con sopra quelle già fatte e le risposte date.
function mostraDomanda(r) {
    $("risultato").hidden = true;
    $("dialogo").hidden = false;
    const d = r.domande[0];

    $("storia").replaceChildren(...storia.map(s => {
        const riga = el("div", "small mb-2");
        riga.append(el("div", "text-body-secondary", s.domanda), el("div", "fw-semibold", "→ " + s.risposta));
        return riga;
    }));
    $("storia").hidden = storia.length === 0;

    const contenuto = [el("p", "fw-semibold mb-2", d.testo)];
    let primo = null;

    if (d.tipo === "numero") {
        const form = el("form", "d-flex flex-wrap align-items-start gap-2");
        form.noValidate = true;
        const gruppo = el("div", "input-group campo-domanda");
        const input = el("input", "form-control");
        input.type = "number";
        input.inputMode = "decimal";
        input.step = "any";
        if (d.min != null) input.min = d.min;
        if (d.max != null) input.max = d.max;
        if (d.suggerito != null) input.placeholder = "es. " + mm(d.suggerito);
        input.setAttribute("aria-label", d.testo);
        gruppo.appendChild(input);
        if (d.unita) gruppo.appendChild(el("span", "input-group-text", d.unita));
        const avanti = el("button", "btn btn-primary", "Avanti");
        avanti.type = "submit";
        form.append(gruppo, avanti);
        const errore = el("div", "w-100");
        form.appendChild(errore);
        alInvio(form, () => {
            const v = input.value === "" ? NaN : Number(input.value);
            if (!Number.isFinite(v) || (d.min != null && v < d.min) || (d.max != null && v > d.max)) {
                mostraMessaggio(errore, d.min != null && d.max != null
                    ? `Scrivi un numero tra ${mm(d.min)} e ${mm(d.max)}.` : "Scrivi un numero.");
                input.focus();
                return;
            }
            return rispondi(d, String(v), mm(v) + (d.unita ? " " + d.unita : ""));
        });
        contenuto.push(form);
        if (d.aiuto) contenuto.push(el("div", "form-text", d.aiuto));
        primo = input;
    }

    if (d.opzioni.length > 0) {
        const scelte = el("div", "d-flex flex-wrap gap-2" + (d.tipo === "numero" ? " mt-2" : ""));
        for (const o of d.opzioni) {
            const b = pulsante(o.etichetta, d.tipo === "numero" ? "btn btn-sm btn-light border" : "btn btn-outline-primary",
                () => rispondi(d, o.valore, o.etichetta));
            scelte.appendChild(b);
            primo ??= b;
        }
        contenuto.push(scelte);
    }

    $("domanda").replaceChildren(...contenuto);

    // Quello che ha già capito, per dare contesto alla domanda.
    const finora = r.capito.map(p => `«${p.testo}» → ${p.significato}`);
    $("finora").textContent = finora.length ? "Finora ho capito: " + finora.join(" · ") : "";
    $("finora").hidden = finora.length === 0;

    // Su telefono la domanda può finire sotto gli esempi: la si porta in vista.
    $("dialogo").scrollIntoView({ behavior: "smooth", block: "nearest" });
    if (primo && !schermoTouch) primo.focus({ preventScroll: true });
}

function rispondi(d, valore, etichetta) {
    if (d.codice === "parole" && valore === "riscrivi") {
        nascondiTutto();
        mostraMessaggio($("messaggio"), "Riscrivi la descrizione e premi Riconosci.", "successo");
        $("descrizione").focus();
        $("descrizione").select();
        return;
    }
    risposte[d.codice] = valore;
    // Nella storia la risposta senza il suggerimento "(dalle misure)".
    storia.push({ domanda: d.testo, risposta: etichetta.replace(/ \(dalle misure\)$/, "") });
    return chiedi();
}

function ricomincia() {
    risposte = {};
    storia = [];
    return chiedi();
}

// ---------------------------------------------------------------------------
// Riepilogo, conferma, correzione
// ---------------------------------------------------------------------------

function mostraRiepilogo(nomeForma, misure) {
    $("risultato").hidden = false;
    $("nomeForma").textContent = nomeForma;
    $("misure").replaceChildren(...misure.map(rigaMisura));

    const rigaElenco = (nomeIcona, classe, ...contenuto) => {
        const li = el("li", "d-flex gap-2 mb-1 " + classe);
        const testoRiga = el("span");
        testoRiga.append(...contenuto);
        li.append(icona(nomeIcona), testoRiga);
        return li;
    };

    $("capito").replaceChildren(
        ...riepilogo.capito.map(p => rigaElenco("check2 text-success", "",
            el("span", "fw-semibold", `«${p.testo}»`), ` → ${p.significato}`)),
        ...storia.map(s => rigaElenco("chat-left-text text-primary", "",
            el("span", "text-body-secondary", s.domanda + " "), el("span", "fw-semibold", s.risposta))));

    // L'attributo hidden sta sul contenitore: su un elemento con d-flex non avrebbe effetto.
    const nonCapito = $("nonCapito");
    const parole = el("div", "d-flex flex-wrap align-items-center gap-1");
    parole.append(el("span", "small text-body-secondary me-1", "Parole non usate:"),
        ...riepilogo.nonCapito.map(p => el("span", "badge rounded-pill text-bg-light border fw-normal", p)));
    nonCapito.replaceChildren(parole);
    nonCapito.hidden = riepilogo.nonCapito.length === 0;

    $("avvisi").replaceChildren(...riepilogo.avvisi.map(a => rigaElenco("info-circle", "text-body-secondary", a)));
}

function rigaMisura(m) {
    const riga = el("div", "d-flex justify-content-between align-items-baseline gap-3 py-1 border-bottom");
    const valore = el("span", "text-end text-nowrap " + (m.predefinita ? "text-body-secondary" : "fw-semibold"), m.valore);
    if (m.predefinita) {
        valore.title = "Non era nella descrizione: valore predefinito";
        valore.append(" ", el("span", "badge rounded-pill text-bg-light border fw-normal", "predefinito"));
    }
    riga.append(el("span", "text-body-secondary", m.etichetta), valore);
    return riga;
}

// Pezzo valido: riepilogo, disegno, 3D e domanda di conferma.
function mostraPezzo(p) {
    pezzo = p;
    mostraRiepilogo(p.nomeForma, p.misure);
    $("vista").hidden = false;
    $("conferma").hidden = false;
    $("correzione").hidden = true;
    disegna(p);
    impostaConferma(false);
    generaModello(p, richiestaCorrente);
    if (storia.length > 0) $("risultato").scrollIntoView({ behavior: "smooth", block: "start" });
}

function impostaConferma(valore) {
    confermato = valore;
    $("chiediConferma").hidden = valore;
    $("confermato").hidden = !valore;
    aggiornaScarica();
}

function aggiornaScarica() {
    $("scarica").disabled = !(confermato && ultimoStl);
    $("notaScarica").hidden = confermato;
}

// Campi con tutte le misure della forma (si può cambiare anche la forma).
function apriCorrezione(parametri, errore) {
    impostaConferma(false);
    $("correzione").hidden = false;
    $("cForma").replaceChildren(...forme.map(f => new Option(f.nome, f.codice)));
    $("cForma").value = parametri.forma;
    $("formCorrezione").dataset.base = JSON.stringify(parametri);
    campiCorrezione(parametri);
    if (errore) mostraMessaggio($("messaggioCorrezione"), errore);
    else $("messaggioCorrezione").textContent = "";
    $("correzione").scrollIntoView({ behavior: "smooth", block: "start" });
}

function campiCorrezione(parametri) {
    const forma = forme.find(f => f.codice === $("cForma").value);
    const campi = forma.misure.map(c => campoNumero("c_" + c.nome, c.etichetta, parametri[c.nome], c.min, c.max, "mm"));
    if (forma.fori) {
        campi.push(campoNumero("c_fori", "Numero di fori", parametri.fori, 0, 50, null, 1));
        campi.push(campoNumero("c_diametro_fori", "Diametro dei fori", parametri.diametro_fori, 0.5, 100, "mm"));
        const colonna = el("div", "col-sm-6");
        const etichettaCampo = el("label", "form-label small fw-semibold mb-1", "Posizione dei fori");
        etichettaCampo.htmlFor = "c_posizione_fori";
        const select = el("select", "form-select");
        select.id = "c_posizione_fori";
        select.append(...forma.posizioniFori.map(o => new Option(o.etichetta, o.valore)));
        select.value = forma.posizioniFori.some(o => o.valore === parametri.posizione_fori)
            ? parametri.posizione_fori : forma.posizioniFori[0].valore;
        colonna.append(etichettaCampo, select);
        campi.push(colonna);
    }
    $("campiCorrezione").replaceChildren(...campi);
}

function campoNumero(id, etichetta, valore, min, max, unita, passo = "any") {
    const colonna = el("div", "col-sm-6");
    const etichettaCampo = el("label", "form-label small fw-semibold mb-1", etichetta);
    etichettaCampo.htmlFor = id;
    const input = el("input", "form-control");
    input.type = "number";
    input.id = id;
    input.inputMode = "decimal";
    input.min = min;
    input.max = max;
    input.step = passo;
    input.required = true;
    input.value = valore ?? "";
    if (unita) {
        const gruppo = el("div", "input-group");
        gruppo.append(input, el("span", "input-group-text", unita));
        colonna.append(etichettaCampo, gruppo);
    } else {
        colonna.append(etichettaCampo, input);
    }
    colonna.appendChild(el("div", "form-text", `Da ${mm(min)} a ${mm(max)}${unita ? " " + unita : ""}.`));
    return colonna;
}

async function salvaCorrezione() {
    const messaggio = $("messaggioCorrezione");
    const forma = forme.find(f => f.codice === $("cForma").value);
    const parametri = { ...JSON.parse($("formCorrezione").dataset.base), forma: forma.codice };

    const nomi = forma.misure.map(c => c.nome).concat(forma.fori ? ["fori", "diametro_fori"] : []);
    for (const nome of nomi) {
        const input = $("c_" + nome);
        if (!input.checkValidity()) {
            mostraMessaggio(messaggio, `${input.closest(".col-sm-6").querySelector("label").textContent}: ` +
                `scrivi un numero tra ${mm(Number(input.min))} e ${mm(Number(input.max))}.`);
            input.focus();
            return;
        }
        parametri[nome] = Number(input.value);
    }
    if (forma.fori) parametri.posizione_fori = $("c_posizione_fori").value;
    else parametri.fori = 0;

    const response = await api("/api/ia/3d/pezzo", jsonBody("POST", { parametri }));
    if (!response.ok) {
        mostraMessaggio(messaggio, await messaggioErrore(response, "Misure non valide."));
        return;
    }
    const p = await response.json();
    ++richiestaCorrente;   // un'eventuale generazione in corso non serve più
    riepilogo = { ...riepilogo, avvisi: [...riepilogo.avvisi.filter(a => !a.startsWith("Hai corretto")), "Hai corretto le misure a mano."] };
    mostraPezzo(p);
    $("conferma").scrollIntoView({ behavior: "smooth", block: "nearest" });
}

// ---------------------------------------------------------------------------
// Modello 3D e download
// ---------------------------------------------------------------------------

function statoAnteprima(testoStato, inCorso) {
    const stato = $("statoAnteprima");
    stato.hidden = !testoStato;
    if (!testoStato) return;
    const figli = [el("span", null, testoStato)];
    if (inCorso) figli.unshift(cerchietto());
    stato.replaceChildren(...figli);
}

async function generaModello(p, numeroRichiesta) {
    ultimoStl = null;
    aggiornaScarica();
    $("dimensioni").textContent = "";
    $("messaggioAnteprima").textContent = "";
    statoAnteprima("Generazione del modello...", true);

    try {
        const response = await api("/api/ia/3d/stl", jsonBody("POST", { parametri: p.parametri }));
        if (numeroRichiesta !== richiestaCorrente) return;
        if (!response.ok) {
            statoAnteprima("Anteprima non disponibile", false);
            mostraMessaggio($("messaggioAnteprima"), await messaggioErrore(response, "Generazione non riuscita."));
            return;
        }
        const buffer = await response.arrayBuffer();
        if (numeroRichiesta !== richiestaCorrente) return;

        ultimoStl = buffer;
        anteprima ??= new Anteprima($("anteprima"));
        const d = anteprima.mostra(buffer, { reinquadra: true });
        $("dimensioni").textContent = `Ingombro ${mm(d.x)} × ${mm(d.y)} × ${mm(d.z)} mm`;
        statoAnteprima("", false);
        aggiornaScarica();
    } catch {
        if (numeroRichiesta === richiestaCorrente) {
            statoAnteprima("Anteprima non disponibile", false);
            mostraMessaggio($("messaggioAnteprima"), "Errore nel collegamento al server.");
        }
    }
}

function scarica() {
    if (!ultimoStl || !pezzo || !confermato) return;
    const url = URL.createObjectURL(new Blob([ultimoStl], { type: "model/stl" }));
    const a = el("a");
    a.href = url;
    a.download = `ia3d-${pezzo.forma}.stl`;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// ---------------------------------------------------------------------------
// Disegno tecnico (SVG): vista frontale e, sotto, vista dall'alto, come nei disegni
// europei. Stessa scala e stessa larghezza: le due viste restano allineate.
// Coordinate in mm con il centro del pezzo in x = 0 (y verso l'alto).
// ---------------------------------------------------------------------------

const SVG = "http://www.w3.org/2000/svg";
const LATO_DISEGNO = 240;                                      // lato più lungo del pezzo, in unità del disegno
const MARGINE = { sinistra: 56, destra: 90, sopra: 56, sotto: 48 };
const DISTANZA_QUOTA = 22;                                     // linea di quota dal bordo del pezzo

// Elemento SVG con i suoi attributi (il testo non viene interpretato come HTML).
function s(tag, attributi = {}, testo) {
    const e = document.createElementNS(SVG, tag);
    for (const [nome, valore] of Object.entries(attributi)) e.setAttribute(nome, valore);
    if (testo !== undefined) e.textContent = testo;
    return e;
}

class Vista {
    constructor(titolo, [minX, maxX, minY, maxY], scala) {
        Object.assign(this, { minX, maxX, minY, maxY, scala });
        const larghezza = MARGINE.sinistra + (maxX - minX) * scala + MARGINE.destra;
        const altezza = MARGINE.sopra + (maxY - minY) * scala + MARGINE.sotto;
        this.svg = s("svg", {
            viewBox: `0 0 ${larghezza.toFixed(1)} ${altezza.toFixed(1)}`,
            class: "disegno-tecnico", role: "img", "aria-label": titolo,
        });
        this.svg.append(s("text", { x: 4, y: 16, class: "titolo-vista" }, titolo));
    }

    // Da mm a unità del disegno.
    X(x) { return MARGINE.sinistra + (x - this.minX) * this.scala; }
    Y(y) { return MARGINE.sopra + (this.maxY - y) * this.scala; }

    aggiungi(tag, attributi, testo) {
        const e = s(tag, attributi, testo);
        this.svg.append(e);
        return e;
    }

    // --- Forme (in mm) ---

    rettangolo(x0, y0, x1, y1, raggio, classe) {
        const x = this.X(x0), y = this.Y(y1);
        this.aggiungi("rect", {
            x, y, width: this.X(x1) - x, height: this.Y(y0) - y, rx: raggio * this.scala, class: classe,
        });
    }

    cerchio(cx, cy, r, classe) {
        this.aggiungi("circle", { cx: this.X(cx), cy: this.Y(cy), r: r * this.scala, class: classe });
    }

    linea(x1, y1, x2, y2, classe) {
        this.aggiungi("line", { x1: this.X(x1), y1: this.Y(y1), x2: this.X(x2), y2: this.Y(y2), class: classe });
    }

    poligono(punti, classe) {
        this.aggiungi("polygon", { points: punti.map(([x, y]) => `${this.X(x)},${this.Y(y)}`).join(" "), class: classe });
    }

    // Assi di simmetria (tratto e punto) per (cx, cy), un poco più lunghi del pezzo.
    assi(cx, cy, semiX, semiY, { orizzontale = true, verticale = true } = {}) {
        const sporgenza = 6 / this.scala;
        if (orizzontale) this.linea(cx - semiX - sporgenza, cy, cx + semiX + sporgenza, cy, "asse");
        if (verticale) this.linea(cx, cy - semiY - sporgenza, cx, cy + semiY + sporgenza, "asse");
    }

    // --- Quote (in unità del disegno) ---

    lineaPx(x1, y1, x2, y2, classe) {
        this.aggiungi("line", { x1, y1, x2, y2, class: classe });
    }

    // Freccia con la punta in (x, y) rivolta nella direzione (dx, dy).
    freccia(x, y, dx, dy) {
        const lunga = 7, larga = 2.6;
        const bx = x - dx * lunga, by = y - dy * lunga;
        this.aggiungi("path", {
            d: `M${x},${y} L${bx - dy * larga},${by + dx * larga} L${bx + dy * larga},${by - dx * larga} Z`,
            class: "freccia",
        });
    }

    testo(x, y, contenuto, { ancora = "middle", ruota = false } = {}) {
        const attributi = { x, y, class: "testo-quota", "text-anchor": ancora };
        if (ruota) attributi.transform = `rotate(-90 ${x} ${y})`;
        this.aggiungi("text", attributi, contenuto);
    }

    // Linea di quota da a a b (b a destra o sotto di a), con frecce e misura.
    // Se è troppo corta, le frecce vanno fuori e puntano verso l'interno.
    lineaDiQuota(a, b, testo, verticale) {
        const [ux, uy] = verticale ? [0, 1] : [1, 0];
        const lunghezza = verticale ? b[1] - a[1] : b[0] - a[0];
        if (lunghezza < 20) {
            this.lineaPx(a[0] - ux * 14, a[1] - uy * 14, b[0] + ux * 14, b[1] + uy * 14, "quota");
            this.freccia(a[0], a[1], ux, uy);
            this.freccia(b[0], b[1], -ux, -uy);
        } else {
            this.lineaPx(a[0], a[1], b[0], b[1], "quota");
            this.freccia(a[0], a[1], -ux, -uy);
            this.freccia(b[0], b[1], ux, uy);
        }
        const mx = (a[0] + b[0]) / 2, my = (a[1] + b[1]) / 2;
        if (verticale) this.testo(mx - 5, my, testo, { ruota: true });
        else this.testo(mx, my - 5, testo);
    }

    // Quota orizzontale sotto (o sopra) il pezzo tra i punti (x1, y1) e (x2, y2) in mm.
    quotaOrizzontale(x1, y1, x2, y2, testo, sopra = false) {
        const yLinea = sopra ? this.Y(this.maxY) - DISTANZA_QUOTA : this.Y(this.minY) + DISTANZA_QUOTA;
        const verso = sopra ? -1 : 1;
        const punti = [[this.X(x1), this.Y(y1)], [this.X(x2), this.Y(y2)]].sort((p, q) => p[0] - q[0]);
        for (const [px, py] of punti) this.lineaPx(px, py + verso * 3, px, yLinea + verso * 4, "richiamo");
        this.lineaDiQuota([punti[0][0], yLinea], [punti[1][0], yLinea], testo, false);
    }

    // Quota verticale a destra (o a sinistra) del pezzo tra i punti (x1, y1) e (x2, y2) in mm.
    quotaVerticale(x1, y1, x2, y2, testo, sinistra = false) {
        const xLinea = sinistra ? this.X(this.minX) - DISTANZA_QUOTA : this.X(this.maxX) + DISTANZA_QUOTA;
        const verso = sinistra ? -1 : 1;
        const punti = [[this.X(x1), this.Y(y1)], [this.X(x2), this.Y(y2)]].sort((p, q) => p[1] - q[1]);
        for (const [px, py] of punti) this.lineaPx(px + verso * 3, py, xLinea + verso * 4, py, "richiamo");
        this.lineaDiQuota([xLinea, punti[0][1]], [xLinea, punti[1][1]], testo, true);
    }

    // Richiamo con testo (es. "4 × Ø5"): parte dal punto (x, y) in mm e sale in diagonale
    // a 45°, di 18 unità o fino all'altezza "fino" (unità del disegno) se indicata.
    nota(x, y, testo, { versoSinistra = false, fino = null } = {}) {
        const px = this.X(x), py = this.Y(y);
        const v = versoSinistra ? -1 : 1;
        const salita = fino === null ? 18 : Math.max(10, py - fino);
        const gx = px + v * salita, gy = py - salita;
        this.lineaPx(px, py, gx, gy, "quota");
        this.lineaPx(gx, gy, gx + v * 8, gy, "quota");
        this.freccia(px, py, -v * Math.SQRT1_2, Math.SQRT1_2);
        this.testo(gx + v * 11, gy + 4, testo, { ancora: versoSinistra ? "end" : "start" });
    }
}

// --- Fori ---

function foriInPianta(vista, fori, df) {
    for (const f of fori) {
        vista.cerchio(f.x, f.y, df / 2, "foro");
        vista.assi(f.x, f.y, df / 2, df / 2);
    }
}

// Nella vista frontale i fori sono nascosti: due linee tratteggiate e l'asse.
function foriInVista(vista, fori, df, z0, z1) {
    for (const x of new Set(fori.map(f => f.x))) {
        vista.linea(x - df / 2, z0, x - df / 2, z1, "nascosto");
        vista.linea(x + df / 2, z0, x + df / 2, z1, "nascosto");
        vista.assi(x, (z0 + z1) / 2, 0, (z1 - z0) / 2, { orizzontale: false });
    }
}

const testoFori = (fori, df) => (fori.length > 1 ? `${fori.length} × ` : "") + "Ø" + mm(df);

// Il richiamo con il diametro parte dal foro più in alto (il più vicino al centro, a destra)
// e il testo va appena sopra il pezzo: così non copre le quote laterali.
function notaFori(vista, fori, df, testo) {
    const piuAlto = Math.max(...fori.map(f => f.y));
    const f = fori.filter(f => f.y === piuAlto)
        .reduce((a, b) => (Math.abs(b.x) < Math.abs(a.x) || (Math.abs(b.x) === Math.abs(a.x) && b.x > a.x) ? b : a));
    vista.nota(f.x + df / 2 * Math.SQRT1_2, f.y + df / 2 * Math.SQRT1_2, testo, { fino: vista.Y(vista.maxY) - 8 });
}

// "90" tra due fori; "3 × 30 = 90" con più fori alla stessa distanza.
function testoInterasse(valori) {
    const totale = valori.at(-1) - valori[0];
    return valori.length > 2 ? `${valori.length - 1} × ${mm(totale / (valori.length - 1))} = ${mm(totale)}` : mm(totale);
}

// Distanze tra i centri dei fori: in orizzontale sopra il pezzo, in verticale a sinistra.
function interassi(vista, fori) {
    const distinti = valori => [...new Set(valori)].sort((a, b) => a - b);
    const xs = distinti(fori.map(f => f.x));
    const ys = distinti(fori.map(f => f.y));
    if (xs.length >= 2) {
        const yDi = x => Math.max(...fori.filter(f => f.x === x).map(f => f.y));
        vista.quotaOrizzontale(xs[0], yDi(xs[0]), xs.at(-1), yDi(xs.at(-1)), testoInterasse(xs), true);
    }
    if (ys.length >= 2) {
        const xDi = y => Math.min(...fori.filter(f => f.y === y).map(f => f.x));
        vista.quotaVerticale(xDi(ys[0]), ys[0], xDi(ys.at(-1)), ys.at(-1), testoInterasse(ys), true);
    }
}

// --- Forme ---

function disegnaRettangolo(fronte, alto, p, fori, df) {
    const { larghezza: L, profondita: P, altezza: H, raggio_angoli: R } = p;

    fronte.rettangolo(-L / 2, 0, L / 2, H, 0, "contorno");
    foriInVista(fronte, fori, df, 0, H);
    fronte.quotaVerticale(L / 2, H, L / 2, 0, mm(H));

    alto.rettangolo(-L / 2, -P / 2, L / 2, P / 2, R, "contorno");
    foriInPianta(alto, fori, df);
    alto.quotaOrizzontale(-L / 2, -P / 2, L / 2, -P / 2, mm(L));
    alto.quotaVerticale(L / 2, P / 2, L / 2, -P / 2, mm(P));
    if (fori.length) {
        interassi(alto, fori);
        notaFori(alto, fori, df, testoFori(fori, df));
    }
    if (R > 0) alto.nota(-L / 2 + R - R * Math.SQRT1_2, P / 2 - R + R * Math.SQRT1_2, "R" + mm(R), { versoSinistra: true });
}

function disegnaCilindro(fronte, alto, p, fori, df) {
    const { diametro: D, altezza: H } = p;

    fronte.rettangolo(-D / 2, 0, D / 2, H, 0, "contorno");
    fronte.assi(0, H / 2, 0, H / 2, { orizzontale: false });
    foriInVista(fronte, fori, df, 0, H);
    fronte.quotaVerticale(D / 2, H, D / 2, 0, mm(H));

    alto.cerchio(0, 0, D / 2, "contorno");
    const raggioFori = fori.length > 1 ? Math.hypot(fori[0].x, fori[0].y) : 0;
    if (raggioFori > 0) alto.cerchio(0, 0, raggioFori, "asse");
    foriInPianta(alto, fori, df);
    alto.assi(0, 0, D / 2, D / 2);
    alto.quotaOrizzontale(-D / 2, 0, D / 2, 0, "Ø" + mm(D));
    if (fori.length) {
        notaFori(alto, fori, df, testoFori(fori, df) + (raggioFori > 0 ? ` su Ø${mm(2 * raggioFori)}` : ""));
    }
}

function disegnaTubo(fronte, alto, p) {
    const { diametro: D, diametro_interno: Di, altezza: H } = p;

    fronte.rettangolo(-D / 2, 0, D / 2, H, 0, "contorno");
    fronte.linea(-Di / 2, 0, -Di / 2, H, "nascosto");
    fronte.linea(Di / 2, 0, Di / 2, H, "nascosto");
    fronte.assi(0, H / 2, 0, H / 2, { orizzontale: false });
    fronte.quotaVerticale(D / 2, H, D / 2, 0, mm(H));

    alto.cerchio(0, 0, D / 2, "contorno");
    alto.cerchio(0, 0, Di / 2, "foro");
    alto.assi(0, 0, D / 2, D / 2);
    alto.quotaOrizzontale(-D / 2, 0, D / 2, 0, "Ø" + mm(D));
    alto.nota(Di / 2 * Math.SQRT1_2, Di / 2 * Math.SQRT1_2, "Ø" + mm(Di));
}

function disegnaSfera(fronte, alto, p) {
    const D = p.diametro;

    fronte.cerchio(0, D / 2, D / 2, "contorno");
    fronte.assi(0, D / 2, D / 2, D / 2);
    fronte.quotaVerticale(0, D, 0, 0, mm(D));

    alto.cerchio(0, 0, D / 2, "contorno");
    alto.assi(0, 0, D / 2, D / 2);
    alto.quotaOrizzontale(-D / 2, 0, D / 2, 0, "Ø" + mm(D));
}

function disegnaCono(fronte, alto, p) {
    const { diametro: D, diametro_cima: Dc, altezza: H } = p;
    const massimo = Math.max(D, Dc);

    fronte.poligono([[-D / 2, 0], [D / 2, 0], [Dc / 2, H], [-Dc / 2, H]], "contorno");
    fronte.assi(0, H / 2, 0, H / 2, { orizzontale: false });
    fronte.quotaVerticale(Dc / 2, H, D / 2, 0, mm(H));
    if (Dc > 0) fronte.quotaOrizzontale(-Dc / 2, H, Dc / 2, H, "Ø" + mm(Dc), true);

    alto.cerchio(0, 0, D / 2, "contorno");
    if (Dc > 0) alto.cerchio(0, 0, Dc / 2, "spigolo");
    alto.assi(0, 0, massimo / 2, massimo / 2);
    alto.quotaOrizzontale(-D / 2, 0, D / 2, 0, "Ø" + mm(D));
    if (Dc > 0) alto.nota(Dc / 2 * Math.SQRT1_2, Dc / 2 * Math.SQRT1_2, "Ø" + mm(Dc));
}

const DISEGNI = {
    blocco: disegnaRettangolo,
    piastra: disegnaRettangolo,
    cilindro: disegnaCilindro,
    tubo: disegnaTubo,
    sfera: disegnaSfera,
    cono: disegnaCono,
};

function disegna(r) {
    const p = r.parametri;
    const fori = r.fori || [];

    // Metà larghezza e metà profondità del pezzo, altezza nella vista frontale.
    let mezzoX, mezzoY, altezza;
    switch (r.forma) {
        case "blocco":
        case "piastra":
            [mezzoX, mezzoY, altezza] = [p.larghezza / 2, p.profondita / 2, p.altezza];
            break;
        case "sfera":
            [mezzoX, mezzoY, altezza] = [p.diametro / 2, p.diametro / 2, p.diametro];
            break;
        case "cono": {
            const massimo = Math.max(p.diametro, p.diametro_cima);
            [mezzoX, mezzoY, altezza] = [massimo / 2, massimo / 2, p.altezza];
            break;
        }
        default:   // cilindro, tubo
            [mezzoX, mezzoY, altezza] = [p.diametro / 2, p.diametro / 2, p.altezza];
    }

    const scala = LATO_DISEGNO / Math.max(2 * mezzoX, 2 * mezzoY, altezza);
    const fronte = new Vista("Vista frontale", [-mezzoX, mezzoX, 0, altezza], scala);
    const alto = new Vista("Vista dall'alto", [-mezzoX, mezzoX, -mezzoY, mezzoY], scala);
    DISEGNI[r.forma]?.(fronte, alto, p, fori, p.diametro_fori);

    $("disegno").replaceChildren(fronte.svg, alto.svg);
}

// ---------------------------------------------------------------------------
// Avvio
// ---------------------------------------------------------------------------

async function avvio() {
    const modulo = DEFINIZIONE_MODULI[MODULI.ia];
    $("titolo").prepend(iconaCategoria(modulo.icona, modulo.colore));

    $("esempi").replaceChildren(...ESEMPI.map(esempio =>
        pulsante(esempio, "btn btn-sm btn-light border", () => {
            $("descrizione").value = esempio;
            return nuovaDescrizione();
        })));

    alInvio($("formDescrizione"), nuovaDescrizione);

    // Invio = Riconosci (Maiusc + Invio va a capo).
    $("descrizione").addEventListener("keydown", e => {
        if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
            e.preventDefault();
            $("formDescrizione").requestSubmit();
        }
    });

    $("ricomincia").addEventListener("click", () => conAttesa($("ricomincia"), ricomincia));
    $("giusto").addEventListener("click", () => impostaConferma(true));
    $("correggo").addEventListener("click", () => apriCorrezione(pezzo.parametri));
    $("correggiAncora").addEventListener("click", () => apriCorrezione(pezzo.parametri));
    $("cForma").addEventListener("change", () => campiCorrezione(JSON.parse($("formCorrezione").dataset.base)));
    alInvio($("formCorrezione"), salvaCorrezione);
    $("annullaCorrezione").addEventListener("click", () => {
        $("correzione").hidden = true;
        if (!pezzo || $("vista").hidden) $("risultato").hidden = true;
    });
    $("scarica").addEventListener("click", scarica);

    const response = await api("/api/ia/3d/forme");
    if (response.ok) forme = await response.json();
}

avviaPagina({ modulo: MODULI.ia }).then(avvio);
