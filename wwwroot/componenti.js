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

// Controlla l'accesso e disegna la barra in alto. Restituisce l'utente corrente.
// Se serve cambiare pagina, la promessa non si risolve mai: il resto non viene eseguito.
async function avviaPagina({ soloAdmin = false } = {}) {
    const response = await fetch("/api/auth/me", { credentials: "same-origin" });

    if (!response.ok) {
        vaiAlLogin();
        return new Promise(() => { });
    }

    const utente = await response.json();

    if (soloAdmin && !utente.isAdmin) {
        location.href = "/";
        return new Promise(() => { });
    }

    const barra = document.getElementById("barraUtente");
    if (barra) {
        barra.replaceChildren(menuPagine(utente), menuProfilo(utente));
    }

    document.body.classList.remove("in-attesa");
    return utente;
}

// Link alle pagine (nav-pills di Bootstrap); quella aperta è evidenziata.
function menuPagine(utente) {
    const menu = el("nav", "menu-pagine nav nav-pills gap-1");

    const pagine = [["Dashboard", "/dashboard.html"], ["Uscite", "/"], ["Categorie", "/categorie.html"]];
    if (utente.isAdmin) pagine.push(["Utenti", "/utenti.html"]);

    const corrente = location.pathname === "/index.html" ? "/" : location.pathname;

    for (const [testo, href] of pagine) {
        const a = el("a", "nav-link py-1 px-3", testo);
        a.href = href;
        if (href === corrente) {
            a.classList.add("active");
            a.setAttribute("aria-current", "page");
        } else {
            a.classList.add("link-light", "link-opacity-75", "link-opacity-100-hover");
        }
        menu.appendChild(a);
    }

    return menu;
}

// Cerchio con l'iniziale: al clic apre una tendina (dropdown-menu di Bootstrap)
// con email, Cambia password ed Esci. Aperta e chiusa dal nostro codice.
function menuProfilo(utente) {
    const profilo = el("div", "dropdown ms-2");

    const cerchio = el("button", "btn btn-primary btn-tondo btn-tondo-piccolo avatar fw-bold",
        utente.email.charAt(0).toUpperCase());
    cerchio.type = "button";
    cerchio.title = utente.email;
    cerchio.setAttribute("aria-label", "Menu utente");
    cerchio.setAttribute("aria-haspopup", "true");
    cerchio.setAttribute("aria-expanded", "false");

    const tendina = el("div", "dropdown-menu menu-utente shadow py-0 overflow-hidden");
    tendina.setAttribute("data-bs-theme", "light");

    const intestazione = el("div", "px-3 py-3 border-bottom");
    intestazione.appendChild(el("div", "small text-body-secondary", "Accesso effettuato come"));
    intestazione.appendChild(el("div", "fw-bold text-break my-1", utente.email));
    if (utente.isAdmin) intestazione.appendChild(etichetta("Amministratore", "primary"));

    const cambio = el("a", "dropdown-item py-2", "Cambia password");
    cambio.href = "/password.html";

    const uscita = pulsante("Esci", "dropdown-item py-2 text-danger border-top", esci);

    tendina.append(intestazione, cambio, uscita);
    profilo.append(cerchio, tendina);

    const apri = aperta => {
        tendina.classList.toggle("show", aperta);
        cerchio.setAttribute("aria-expanded", String(aperta));
    };

    cerchio.addEventListener("click", e => {
        e.stopPropagation();
        apri(!tendina.classList.contains("show"));
    });

    // Chiudi cliccando fuori o con Esc.
    document.addEventListener("click", e => {
        if (!profilo.contains(e.target)) apri(false);
    });
    document.addEventListener("keydown", e => {
        if (e.key === "Escape" && tendina.classList.contains("show")) {
            apri(false);
            cerchio.focus();
        }
    });

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

// Su computer è una finestra sospesa; su telefono una schermata normale al posto
// del contenuto (una finestra sospesa, su iPhone con la tastiera, sposta i tocchi).
// Il <dialog> deve contenere un <h2> (titolo) con tabindex="-1" autofocus.
function moduloFinestra(dialog, { primoCampo } = {}) {

    let scrollSalvato = 0;
    const aPagina = () => matchMedia("(max-width: 600px), (pointer: coarse)").matches;

    // "close" scatta anche chiudendo con Esc: torna dov'eri.
    dialog.addEventListener("close", () => {
        if (document.body.classList.contains("modulo-a-pagina")) {
            document.body.classList.remove("modulo-a-pagina");
            window.scrollTo(0, scrollSalvato);
        }
    });

    return {
        apri(titolo) {
            if (titolo) dialog.querySelector("h2").textContent = titolo;
            scrollSalvato = window.scrollY;

            if (aPagina()) {
                document.body.classList.add("modulo-a-pagina");
                dialog.show();
                window.scrollTo(0, 0);
            } else {
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
