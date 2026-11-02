// Componenti e funzioni comuni a tutte le pagine.
// Le classi sono quelle di Bootstrap 5 più le aggiunte di componenti.css.
// Esempi di tutti i componenti: componenti.html

// ---------------------------------------------------------------------------
// Elementi di base
// ---------------------------------------------------------------------------

// Crea un elemento con classi e testo (il testo NON viene interpretato come HTML).
function el(tag, classi, testo) {
    const e = document.createElement(tag);
    if (classi) e.className = classi;
    if (testo !== undefined) e.textContent = testo;
    return e;
}

// Pulsante Bootstrap. Se onClick è asincrono (salva, elimina, esci...),
// mostra il cerchietto finché l'operazione non è finita.
// Esempio: pulsante("Elimina", "btn btn-sm btn-danger", () => elimina(x))
function pulsante(testo, classi, onClick) {
    const b = el("button", classi, testo);
    b.type = "button";
    b.addEventListener("click", e => {
        const risultato = onClick(e);
        if (risultato && typeof risultato.then === "function") {
            conAttesa(b, () => risultato);
        }
    });
    return b;
}

// Cerchietto di caricamento di Bootstrap.
function cerchietto(piccolo = true) {
    const s = el("span", "spinner-border" + (piccolo ? " spinner-border-sm" : ""));
    s.setAttribute("aria-hidden", "true");
    return s;
}

// Riga "cerchietto + Caricamento..." da mettere in un elenco.
function caricamentoElenco(testo = "Caricamento...") {
    const div = el("div", "caricamento-elenco d-flex align-items-center gap-2 text-body-secondary p-3");
    const s = cerchietto();
    s.classList.add("text-primary");
    div.append(s, el("span", null, testo));
    return div;
}

// Messaggio sotto un modulo: tipo "errore" (rosso) o "successo" (verde).
function mostraMessaggio(elemento, testo, tipo = "errore") {
    elemento.textContent = testo;
    elemento.className = "small mt-2 " + (tipo === "successo" ? "text-success" : "text-danger");
}

// Etichetta colorata (badge di Bootstrap). colore: primary, danger, secondary...
function etichetta(testo, colore = "secondary") {
    return el("span", `badge rounded-pill text-bg-${colore} fw-normal`, testo);
}

// ---------------------------------------------------------------------------
// Attesa sui pulsanti
// ---------------------------------------------------------------------------

// Mostra il cerchietto nel pulsante e lo blocca mentre "azione" è in corso
// (così un doppio clic non invia la stessa richiesta due volte).
async function conAttesa(bottone, azione) {
    if (!bottone) return azione();
    if (bottone.getAttribute("aria-busy") === "true") return;

    // Fissa la larghezza, così il pulsante non cambia misura.
    const larghezza = bottone.style.width;
    bottone.style.width = bottone.offsetWidth + "px";
    const contenuto = [...bottone.childNodes];
    const testo = el("span", "visually-hidden", "Attendere...");
    bottone.replaceChildren(cerchietto(), testo);
    bottone.disabled = true;
    bottone.setAttribute("aria-busy", "true");

    try {
        return await azione();
    } finally {
        bottone.replaceChildren(...contenuto);
        bottone.style.width = larghezza;
        bottone.disabled = false;
        bottone.removeAttribute("aria-busy");
    }
}

// Collega l'invio di un modulo: il pulsante di invio mostra il cerchietto.
function alInvio(form, gestore) {
    form.addEventListener("submit", e => {
        e.preventDefault();
        const bottone = form.querySelector('button[type="submit"]');
        conAttesa(bottone, () => gestore(e));
    });
}

// ---------------------------------------------------------------------------
// Chiamate al server
// ---------------------------------------------------------------------------

// Estrae un messaggio leggibile dalla risposta di errore dell'API.
async function messaggioErrore(response, predefinito) {
    if (response.status === 429) {
        return "Troppe richieste. Attendi un minuto e riprova.";
    }
    try {
        const dati = await response.json();
        if (dati && dati.messaggio) return dati.messaggio;
        if (dati && dati.errors) return Object.values(dati.errors).flat().join(" ");
    } catch { }
    return predefinito;
}

function vaiAlLogin() {
    const ritorno = encodeURIComponent(location.pathname + location.search);
    location.href = `/login.html?ritorno=${ritorno}`;
}

// fetch che, se la sessione è scaduta, riporta alla pagina di accesso.
async function api(url, opzioni = {}) {
    const response = await fetch(url, { credentials: "same-origin", ...opzioni });
    if (response.status === 401) {
        vaiAlLogin();
        throw new Error("Accesso richiesto");
    }
    return response;
}

function jsonBody(metodo, dati) {
    return {
        method: metodo,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dati)
    };
}

async function esci() {
    await fetch("/api/auth/logout", { method: "POST", credentials: "same-origin" });
    location.href = "/login.html";
    return new Promise(() => { }); // il cerchietto gira fino al cambio pagina
}

// ---------------------------------------------------------------------------
// Pagina e barra in alto
// ---------------------------------------------------------------------------

// Moduli dell'app (codici come in Models/Moduli.cs) con nome, icona e pagine.
// Menu in alto, pagina di ingresso e controllo delle pagine leggono tutti da qui.
// amministratore: true = pagina riservata al livello Amministratore del modulo.
const MODULI = { finanze: "finanze", fantapolitica: "fantapolitica", stampa3d: "stampa3d", admin: "admin" };

const DEFINIZIONE_MODULI = {
    finanze: {
        nome: "Gestione finanze", icona: "wallet2", descrizione: "Dashboard, uscite e categorie",
        pagine: [
            { testo: "Dashboard", href: "/dashboard.html", icona: "bar-chart" },
            { testo: "Uscite", href: "/uscite.html", icona: "list-ul" },
            { testo: "Categorie", href: "/categorie.html", icona: "tags", amministratore: true },
        ]
    },
    fantapolitica: {
        nome: "Fantapolitica", icona: "bank", descrizione: "Squadre di politici, eventi e classifica",
        pagine: [
            { testo: "Panoramica", href: "/fantapolitica.html", icona: "trophy" },
            { testo: "Politici ed eventi", href: "/fantapolitica-gestione.html", icona: "person-badge", amministratore: true },
        ]
    },
    stampa3d: {
        nome: "Stampa 3D", icona: "printer", descrizione: "Catalogo di oggetti personalizzabili, anteprima e file da stampare",
        pagine: [
            { testo: "Catalogo", href: "/stampa3d.html", icona: "grid-3x3-gap", anche: ["/stampa3d-configuratore.html"] },
            { testo: "Progetti", href: "/stampa3d-progetti.html", icona: "folder2-open" },
        ]
    },
    admin: {
        nome: "Admin", icona: "shield-lock", descrizione: "Utenti e autorizzazioni ai moduli",
        pagine: [
            { testo: "Utenti", href: "/utenti.html", icona: "people" },
            { testo: "Moduli", href: "/moduli.html", icona: "grid", anche: ["/modulo.html"] },
        ]
    },
};

const ORDINE_MODULI = [MODULI.admin, MODULI.finanze, MODULI.fantapolitica, MODULI.stampa3d];   // ordine nella barra e nella pagina di ingresso

// Livello dell'utente in un modulo ("Utente", "Amministratore") o undefined se non ha accesso.
// Il server dà al Super admin tutti i moduli come Amministratore.
const livello = (utente, modulo) => (utente.moduli || {})[modulo];

// Moduli abilitati per l'utente, nell'ordine del menu.
const moduliAbilitati = utente => ORDINE_MODULI.filter(m => livello(utente, m));

// Pagine del modulo che l'utente può aprire.
const pagineAbilitate = (utente, modulo) =>
    DEFINIZIONE_MODULI[modulo].pagine.filter(p => !p.amministratore || livello(utente, modulo) === "Amministratore");

// La pagina di ingresso è per tutti.
const paginaIniziale = () => "/";

// Controlla l'accesso e disegna la barra in alto. Restituisce l'utente corrente.
// modulo: la pagina richiede l'accesso a quel modulo; amministratore: richiede il
// livello Amministratore. Senza accesso si torna alla pagina di ingresso.
// Se serve cambiare pagina, la promessa non si risolve mai: il resto non viene eseguito.
async function avviaPagina({ modulo = null, amministratore = false } = {}) {
    const response = await fetch("/api/auth/me", { credentials: "same-origin" });

    if (!response.ok) {
        vaiAlLogin();
        return new Promise(() => { });
    }

    const utente = await response.json();

    const consentito = !modulo ||
        (amministratore ? livello(utente, modulo) === "Amministratore" : !!livello(utente, modulo));

    if (!consentito) {
        location.href = paginaIniziale(utente);
        return new Promise(() => { });
    }

    const barra = document.getElementById("barraUtente");
    if (barra) {
        barra.replaceChildren(menuPagine(utente), menuProfilo(utente));
    }

    document.body.classList.remove("in-attesa");
    return utente;
}

// Barra in alto: un menu a tendina per ogni modulo abilitato (es. "Gestione finanze",
// "Admin"), con le sue pagine; il modulo della pagina aperta è evidenziato.
function menuPagine(utente) {
    const menu = el("nav", "menu-pagine nav nav-pills gap-1");
    const corrente = location.pathname;
    const eCorrente = p => p.href === corrente || (p.anche || []).includes(corrente);

    for (const codice of moduliAbilitati(utente)) {
        const def = DEFINIZIONE_MODULI[codice];
        const pagine = pagineAbilitate(utente, codice);
        const attivo = pagine.some(eCorrente);

        const contenitore = el("div", "dropdown");
        const bottone = el("button", "nav-link py-1 px-3 dropdown-toggle " +
            (attivo ? "active" : "link-light link-opacity-75 link-opacity-100-hover"), def.nome);
        bottone.type = "button";

        const tendina = el("div", "dropdown-menu shadow");
        tendina.setAttribute("data-bs-theme", "light");
        for (const p of pagine) {
            const a = el("a", "dropdown-item py-2 d-flex align-items-center gap-2" + (eCorrente(p) ? " active" : ""));
            a.href = p.href;
            if (eCorrente(p)) a.setAttribute("aria-current", "page");
            const icona = el("i", "bi bi-" + p.icona);
            icona.setAttribute("aria-hidden", "true");
            a.append(icona, el("span", null, p.testo));
            tendina.appendChild(a);
        }

        contenitore.append(bottone, tendina);
        collegaTendina(contenitore, bottone, tendina);
        menu.appendChild(contenitore);
    }

    return menu;
}

// Apre e chiude una tendina (dropdown-menu di Bootstrap) senza il JavaScript di Bootstrap:
// clic sul pulsante, chiusura cliccando fuori o con Esc.
function collegaTendina(contenitore, bottone, tendina) {
    bottone.setAttribute("aria-haspopup", "true");
    bottone.setAttribute("aria-expanded", "false");

    const apri = aperta => {
        tendina.classList.toggle("show", aperta);
        bottone.setAttribute("aria-expanded", String(aperta));
    };

    bottone.addEventListener("click", e => {
        e.stopPropagation();
        apri(!tendina.classList.contains("show"));
    });
    document.addEventListener("click", e => {
        if (!contenitore.contains(e.target)) apri(false);
    });
    document.addEventListener("keydown", e => {
        if (e.key === "Escape" && tendina.classList.contains("show")) {
            apri(false);
            bottone.focus();
        }
    });
}

// Cerchio con l'iniziale: al clic apre una tendina (dropdown-menu di Bootstrap)
// con email, Cambia password ed Esci. Aperta e chiusa dal nostro codice.
function menuProfilo(utente) {
    const profilo = el("div", "dropdown ms-2");

    const nome = `${utente.nome || ""} ${utente.cognome || ""}`.trim();

    const cerchio = el("button", "btn btn-primary btn-tondo btn-tondo-piccolo avatar fw-bold",
        (nome || utente.email).charAt(0).toUpperCase());
    cerchio.type = "button";
    cerchio.title = nome || utente.email;
    cerchio.setAttribute("aria-label", "Menu utente");

    const tendina = el("div", "dropdown-menu menu-utente shadow py-0 overflow-hidden");
    tendina.setAttribute("data-bs-theme", "light");

    const intestazione = el("div", "px-3 py-3 border-bottom");
    intestazione.appendChild(el("div", "small text-body-secondary", "Accesso effettuato come"));
    if (nome) intestazione.appendChild(el("div", "fw-bold text-break mt-1", nome));
    intestazione.appendChild(el("div", (nome ? "small text-body-secondary" : "fw-bold mt-1") + " text-break mb-1", utente.email));
    intestazione.appendChild(etichetteModuli(utente.moduli, utente.superAdmin));

    const cambio = el("a", "dropdown-item py-2", "Cambia password");
    cambio.href = "/password.html";

    const uscita = pulsante("Esci", "dropdown-item py-2 text-danger border-top", esci);

    tendina.append(intestazione, cambio, uscita);
    profilo.append(cerchio, tendina);

    collegaTendina(profilo, cerchio, tendina);

    return profilo;
}

// ---------------------------------------------------------------------------
// Formati
// ---------------------------------------------------------------------------

const euro = new Intl.NumberFormat("it-IT", { style: "currency", currency: "EUR", useGrouping: "always" });

// "2026-11-02" → "02/11/2026"
const formatoData = iso => {
    const [a, m, g] = iso.split("-");
    return `${g}/${m}/${a}`;
};

// Data di oggi come "aaaa-mm-gg" (ora locale).
const oggi = () => {
    const d = new Date();
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 10);
};

// Telefono/tablet (schermo touch).
const schermoTouch = matchMedia("(pointer: coarse)").matches;

// ---------------------------------------------------------------------------
// Pannello di ricerca (lente in alto a destra)
// ---------------------------------------------------------------------------

// Collega lente, pannello, pallino "filtri attivi", pulsante Azzera e campi.
// "applica" viene chiamata quando i filtri cambiano.
// Su telefono il filtro si applica quando il selettore (categoria, data) o la
// tastiera si chiudono: se l'elenco si accorciasse mentre sono aperti, su iPhone
// i tocchi resterebbero spostati rispetto a quello che si vede.
function pannelloRicerca({ bottone, pannello, pallino, azzera, campi, applica }) {

    const attivi = () => campi.some(c => c.value.trim() !== "");

    const aggiornaSegnaposti = () => {
        campi.forEach(c => c.classList.toggle("vuoto", c.value === ""));
    };

    const aggiorna = () => {
        aggiornaSegnaposti();
        pallino.hidden = !attivi();
        applica();
    };

    let attesa;

    campi.forEach(campo => {
        const testuale = campo.tagName === "INPUT" && (campo.type === "text" || campo.type === "search");

        if (testuale) {
            if (schermoTouch) {
                // "change" scatta quando la tastiera si chiude (Fine / Invio).
                campo.addEventListener("change", aggiorna);
            } else {
                campo.addEventListener("input", () => {
                    clearTimeout(attesa);
                    attesa = setTimeout(aggiorna, 250);
                });
            }
            return;
        }

        campo.addEventListener("input", aggiornaSegnaposti);

        campo.addEventListener("change", () => {
            aggiornaSegnaposti();
            if (schermoTouch && document.activeElement === campo) {
                campo.dataset.daApplicare = "1";
            } else {
                aggiorna();
            }
        });

        campo.addEventListener("blur", () => {
            if (campo.dataset.daApplicare) {
                delete campo.dataset.daApplicare;
                aggiorna();
            }
        });
    });

    azzera.addEventListener("click", () => {
        campi.forEach(c => c.value = "");
        aggiorna();
    });

    bottone.addEventListener("click", () => {
        pannello.hidden = !pannello.hidden;
        bottone.setAttribute("aria-expanded", String(!pannello.hidden));
        bottone.classList.toggle("active", !pannello.hidden);
        bottone.classList.toggle("bg-white", pannello.hidden);
        if (!pannello.hidden && !schermoTouch) campi[0].focus();
    });

    aggiornaSegnaposti();

    return {
        // Valori dei campi per id, es. { filtroCategoria: "3", dal: "", al: "" }
        valori: () => Object.fromEntries(campi.map(c => [c.id, c.value.trim()])),
        attivi
    };
}

// ---------------------------------------------------------------------------
// Modulo di inserimento/modifica (pulsante + e "Modifica")
// ---------------------------------------------------------------------------

// Su computer è la finestra modale del browser (fissata sullo schermo).
// Su telefono è una finestra sopra la pagina con lo sfondo scuro, ma "agganciata"
// alla pagina (position: absolute) invece che fissata sullo schermo: su iPhone gli
// elementi fissati, quando si apre la tastiera, fanno finire i tocchi nel punto
// sbagliato; quelli agganciati alla pagina si spostano con lei e restano allineati.
// Il <dialog> deve contenere un <h2> (titolo) con tabindex="-1" autofocus.
function moduloFinestra(dialog, { primoCampo } = {}) {

    const suTelefono = () => matchMedia("(max-width: 600px), (pointer: coarse)").matches;
    let sfondo = null;

    const chiudiSuTelefono = () => {
        document.body.classList.remove("modale-telefono");
        if (sfondo) { sfondo.remove(); sfondo = null; }
    };

    // "close" scatta anche chiudendo con Esc.
    dialog.addEventListener("close", chiudiSuTelefono);

    // Esc chiude anche la versione per telefono (es. iPad con tastiera).
    document.addEventListener("keydown", e => {
        if (e.key === "Escape" && dialog.open && sfondo) dialog.close();
    });

    return {
        apri(titolo) {
            if (titolo) dialog.querySelector("h2").textContent = titolo;

            if (suTelefono()) {
                sfondo = el("div", "sfondo-modale");
                sfondo.style.height = document.documentElement.scrollHeight + "px";
                document.body.appendChild(sfondo);
                document.body.classList.add("modale-telefono");
                dialog.style.top = (window.scrollY + 12) + "px";
                dialog.show();
            } else {
                dialog.style.top = "";
                dialog.showModal();
                if (primoCampo && !primoCampo.disabled && !primoCampo.readOnly) primoCampo.focus();
            }
        },
        chiudi() {
            dialog.close();
        }
    };
}

// ---------------------------------------------------------------------------
// Elenchi
// ---------------------------------------------------------------------------

// Elenco che si aggiorna (es. dopo un filtro): sbiadito con il cerchietto sopra,
// altezza mantenuta così la pagina non "salta". Al primo caricamento non serve:
// c'è già la riga "Caricamento...".
function elencoInAggiornamento(lista, cerchiettoSopra, attivo) {
    if (attivo && lista.querySelector(".caricamento-elenco")) return;
    lista.classList.toggle("opacity-50", attivo);
    cerchiettoSopra.hidden = !attivo;
    lista.style.minHeight = attivo && lista.offsetHeight ? lista.offsetHeight + "px" : "";
}

// Riempie una tendina con le categorie, mantenendo la scelta corrente.
function riempiCategorie(select, categorie, primaVoce) {
    const scelta = select.value;
    select.replaceChildren(new Option(primaVoce, ""),
        ...categorie.map(c => new Option(c.nome, c.id)));
    select.value = scelta;
}

// Parametri di ricerca delle uscite dai filtri (categoria, dal, al).
function parametriUscite({ filtroCategoria, dal, al }) {
    const p = new URLSearchParams();
    if (filtroCategoria) p.set("categoriaId", filtroCategoria);
    if (dal) p.set("dal", dal);
    if (al) p.set("al", al);
    return p;
}

// ---------------------------------------------------------------------------
// Categorie: icona e colore
// ---------------------------------------------------------------------------

// Cerchio colorato con l'icona della categoria (Bootstrap Icons).
function iconaCategoria(icona, colore, piccola = false) {
    const cerchio = el("span", "icona-categoria" + (piccola ? " piccola" : ""));
    cerchio.style.setProperty("--colore", colore);
    cerchio.setAttribute("aria-hidden", "true");
    cerchio.appendChild(el("i", "bi bi-" + icona));
    return cerchio;
}

// Colora una riga dell'elenco con il colore della categoria (striscia a sinistra).
function coloraRiga(riga, colore) {
    riga.classList.add("riga-colorata");
    riga.style.setProperty("--colore", colore);
    return riga;
}

// ---------------------------------------------------------------------------
// Carosello
// ---------------------------------------------------------------------------

// Carosello basato sullo scorrimento orizzontale "a scatti" del browser:
// swipe su telefono; sopra, frecce ‹ › e pallini (anche frecce della tastiera).
// Struttura: <div class="carosello"><div class="carosello-binario" tabindex="0">
//              <section class="carosello-slide">…</section> …</div></div>
function carosello(contenitore) {

    const binario = contenitore.querySelector(".carosello-binario");
    const slide = [...binario.querySelectorAll(".carosello-slide")];
    if (slide.length < 2) return;

    // Distanza tra una slide e la successiva (larghezza + spazio).
    const passo = () => (slide[1].offsetLeft - slide[0].offsetLeft) || 1;
    const indice = () => Math.max(0, Math.min(slide.length - 1, Math.round(binario.scrollLeft / passo())));
    const vaiA = i => {
        const n = Math.max(0, Math.min(slide.length - 1, i));
        binario.scrollTo({ left: slide[n].offsetLeft - slide[0].offsetLeft, behavior: "smooth" });
    };

    const freccia = (icona, etichetta, passo) => {
        const b = pulsante("", "btn btn-light border rounded-circle btn-tondo btn-tondo-piccolo", () => vaiA(indice() + passo));
        b.setAttribute("aria-label", etichetta);
        b.appendChild(el("i", "bi bi-" + icona));
        return b;
    };

    const precedente = freccia("chevron-left", "Grafico precedente", -1);
    const successivo = freccia("chevron-right", "Grafico successivo", 1);

    const pallini = slide.map((s, i) => {
        const area = el("button", "carosello-pallino-area");
        area.type = "button";
        area.setAttribute("aria-label", s.getAttribute("aria-label") || `Vai a ${i + 1}`);
        area.appendChild(el("span", "carosello-pallino d-inline-block"));
        area.addEventListener("click", () => vaiA(i));
        return area;
    });

    const comandi = el("div", "carosello-comandi");
    comandi.append(precedente, ...pallini, successivo);
    contenitore.insertBefore(comandi, binario);   // sopra: sempre visibili, anche con grafici alti

    const aggiorna = () => {
        const i = indice();
        pallini.forEach((p, n) => p.firstChild.setAttribute("aria-current", String(n === i)));
        precedente.disabled = i === 0;
        successivo.disabled = i === slide.length - 1;
    };

    let attesa;
    binario.addEventListener("scroll", () => { clearTimeout(attesa); attesa = setTimeout(aggiorna, 60); });
    window.addEventListener("resize", () => vaiA(indice()));

    binario.addEventListener("keydown", e => {
        if (e.key === "ArrowLeft") { e.preventDefault(); vaiA(indice() - 1); }
        if (e.key === "ArrowRight") { e.preventDefault(); vaiA(indice() + 1); }
    });

    aggiorna();
}

// ---------------------------------------------------------------------------
// Moduli e autorizzazioni
// ---------------------------------------------------------------------------

// Intestazione della pagina di un modulo in Admin → Moduli: percorso e titolo.
function intestazioneModulo(contenitore, codice) {
    const def = DEFINIZIONE_MODULI[codice] || { nome: codice, icona: "grid" };

    const percorso = el("nav", "small mb-2");
    percorso.setAttribute("aria-label", "Percorso");
    const moduli = el("a", "link-secondary", "Moduli");
    moduli.href = "/moduli.html";
    percorso.append(moduli, el("span", "text-body-secondary mx-1", "›"), el("span", "fw-semibold", def.nome));

    const titolo = el("div", "d-flex align-items-center gap-2 mb-3");
    titolo.append(iconaCategoria(def.icona, "#212529"), el("h2", "h3 fw-bold mb-0", def.nome));

    contenitore.replaceChildren(percorso, titolo);
}

// Etichette "Super admin" oppure "Modulo · Livello" (es. "Gestione finanze · Utente").
// moduli: autorizzazioni registrate; superAdmin: vede tutto.
function etichetteModuli(moduli, superAdmin = false) {
    const div = el("div", "d-flex flex-wrap gap-1");
    if (superAdmin) div.appendChild(etichetta("Super admin", "dark"));
    const voci = Object.entries(moduli || {}).filter(([m]) => !(superAdmin && m === MODULI.admin));
    if (voci.length === 0 && !superAdmin) {
        div.appendChild(etichetta("Nessun accesso", "danger"));
        return div;
    }
    if (!superAdmin) {
        for (const [modulo, liv] of voci) {
            div.appendChild(etichetta(`${(DEFINIZIONE_MODULI[modulo] || { nome: modulo }).nome} · ${liv}`,
                liv === "Amministratore" ? "primary" : "secondary"));
        }
    }
    return div;
}
