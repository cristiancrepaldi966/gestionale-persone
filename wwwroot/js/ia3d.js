// Pagina IA 3D del modulo IA (ia3d.html): si descrive un pezzo a parole e il server ne
// riconosce forma e misure (POST /api/ia/3d/riconosci). Sotto compaiono cosa è stato capito,
// il disegno tecnico con le quote (SVG costruito qui dalle misure) e l'anteprima 3D del file
// STL (POST /api/ia/3d/stl), da scaricare.
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
    "cono base 40 punta 10 alto 30",
    "sfera 4 cm",
];

let anteprima = null;
let ultimo = null;          // ultimo pezzo riconosciuto
let ultimoStl = null;       // ArrayBuffer del suo STL
let richiestaCorrente = 0;

// ---------------------------------------------------------------------------
// Riconoscimento
// ---------------------------------------------------------------------------

async function riconosci() {
    const messaggio = $("messaggio");
    messaggio.textContent = "";
    const testo = $("descrizione").value.trim();
    if (!testo) {
        mostraMessaggio(messaggio, "Descrivi il pezzo da creare.");
        $("descrizione").focus();
        return;
    }

    const numeroRichiesta = ++richiestaCorrente;
    let response;
    try {
        response = await api("/api/ia/3d/riconosci", jsonBody("POST", { testo }));
    } catch {
        if (numeroRichiesta === richiestaCorrente) mostraMessaggio(messaggio, "Errore nel collegamento al server.");
        return;
    }
    if (numeroRichiesta !== richiestaCorrente) return;

    if (!response.ok) {
        $("risultato").hidden = true;
        mostraMessaggio(messaggio, await messaggioErrore(response, "Non sono riuscito a riconoscere il pezzo."));
        return;
    }

    ultimo = await response.json();
    if (numeroRichiesta !== richiestaCorrente) return;
    mostraRiconoscimento(ultimo);
    generaModello(ultimo, numeroRichiesta);   // senza attendere: il pulsante torna subito libero
}

function mostraRiconoscimento(r) {
    $("risultato").hidden = false;
    $("nomeForma").textContent = r.nomeForma;
    $("misure").replaceChildren(...r.misure.map(rigaMisura));

    $("capito").replaceChildren(...r.capito.map(p => {
        const li = el("li", "d-flex gap-2 mb-1");
        const icona = el("i", "bi bi-check2 text-success");
        icona.setAttribute("aria-hidden", "true");
        const testo = el("span");
        testo.append(el("span", "fw-semibold", `«${p.testo}»`), ` → ${p.significato}`);
        li.append(icona, testo);
        return li;
    }));

    // L'attributo hidden sta sul contenitore: su un elemento con d-flex non avrebbe effetto.
    const nonCapito = $("nonCapito");
    const parole = el("div", "d-flex flex-wrap align-items-center gap-1");
    parole.append(el("span", "small text-body-secondary me-1", "Parole non usate:"),
        ...r.nonCapito.map(p => el("span", "badge rounded-pill text-bg-light border fw-normal", p)));
    nonCapito.replaceChildren(parole);
    nonCapito.hidden = r.nonCapito.length === 0;

    $("avvisi").replaceChildren(...r.avvisi.map(a => {
        const li = el("li", "d-flex gap-2 mb-1 text-body-secondary");
        const icona = el("i", "bi bi-info-circle");
        icona.setAttribute("aria-hidden", "true");
        li.append(icona, el("span", null, a));
        return li;
    }));

    disegna(r);
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

// ---------------------------------------------------------------------------
// Modello 3D e download
// ---------------------------------------------------------------------------

function statoAnteprima(testo, inCorso) {
    const stato = $("statoAnteprima");
    stato.hidden = !testo;
    if (!testo) return;
    const figli = [el("span", null, testo)];
    if (inCorso) figli.unshift(cerchietto());
    stato.replaceChildren(...figli);
}

async function generaModello(r, numeroRichiesta) {
    ultimoStl = null;
    $("scarica").disabled = true;
    $("dimensioni").textContent = "";
    $("messaggioAnteprima").textContent = "";
    statoAnteprima("Generazione del modello...", true);

    try {
        const response = await api("/api/ia/3d/stl", jsonBody("POST", { parametri: r.parametri }));
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
        $("scarica").disabled = false;
    } catch {
        if (numeroRichiesta === richiestaCorrente) {
            statoAnteprima("Anteprima non disponibile", false);
            mostraMessaggio($("messaggioAnteprima"), "Errore nel collegamento al server.");
        }
    }
}

function scarica() {
    if (!ultimoStl || !ultimo) return;
    const url = URL.createObjectURL(new Blob([ultimoStl], { type: "model/stl" }));
    const a = el("a");
    a.href = url;
    a.download = `ia3d-${ultimo.forma}.stl`;
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

function avvio() {
    const modulo = DEFINIZIONE_MODULI[MODULI.ia];
    $("titolo").prepend(iconaCategoria(modulo.icona, modulo.colore));

    $("esempi").replaceChildren(...ESEMPI.map(testo =>
        pulsante(testo, "btn btn-sm btn-light border", () => {
            $("descrizione").value = testo;
            return riconosci();
        })));

    alInvio($("formDescrizione"), riconosci);

    // Invio = Riconosci (Maiusc + Invio va a capo).
    $("descrizione").addEventListener("keydown", e => {
        if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
            e.preventDefault();
            $("formDescrizione").requestSubmit();
        }
    });

    $("scarica").addEventListener("click", scarica);
}

avviaPagina({ modulo: MODULI.ia }).then(avvio);
