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

    const pagine = [["Uscite", "/"], ["Categorie", "/categorie.html"]];
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
