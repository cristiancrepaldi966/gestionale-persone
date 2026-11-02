// Funzioni comuni a tutte le pagine.

// Crea un elemento con classe e testo (il testo NON viene interpretato come HTML).
function el(tag, className, text) {
    const e = document.createElement(tag);
    if (className) e.className = className;
    if (text !== undefined) e.textContent = text;
    return e;
}

function pulsante(testo, className, onClick) {
    const b = el("button", className, testo);
    b.type = "button";
    b.addEventListener("click", onClick);
    return b;
}

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
}

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

// Link alle pagine; quella aperta è evidenziata.
function menuPagine(utente) {
    const menu = el("div", "menu-pagine");

    const pagine = [["Uscite", "/"], ["Categorie", "/categorie.html"]];
    if (utente.isAdmin) pagine.push(["Utenti", "/utenti.html"]);

    const corrente = location.pathname === "/index.html" ? "/" : location.pathname;

    for (const [testo, href] of pagine) {
        const a = el("a", null, testo);
        a.href = href;
        if (href === corrente) {
            a.classList.add("attivo");
            a.setAttribute("aria-current", "page");
        }
        menu.appendChild(a);
    }

    return menu;
}

// Cerchio con l'iniziale: al clic apre una tendina con email, cambio password ed esci.
function menuProfilo(utente) {
    const profilo = el("div", "profilo");

    const cerchio = el("button", "avatar", utente.email.charAt(0).toUpperCase());
    cerchio.type = "button";
    cerchio.title = utente.email;
    cerchio.setAttribute("aria-label", "Menu utente");
    cerchio.setAttribute("aria-haspopup", "true");
    cerchio.setAttribute("aria-expanded", "false");

    const tendina = el("div", "tendina");
    tendina.hidden = true;

    const intestazione = el("div", "tendina-utente");
    intestazione.appendChild(el("div", "tendina-etichetta", "Accesso effettuato come"));
    intestazione.appendChild(el("div", "tendina-email", utente.email));
    if (utente.isAdmin) intestazione.appendChild(el("span", "etichetta admin", "Amministratore"));

    const cambio = el("a", "tendina-voce", "Cambia password");
    cambio.href = "/password.html";

    const uscita = pulsante("Esci", "tendina-voce tendina-esci", esci);

    tendina.append(intestazione, cambio, uscita);
    profilo.append(cerchio, tendina);

    const apri = aperta => {
        tendina.hidden = !aperta;
        cerchio.setAttribute("aria-expanded", String(aperta));
    };

    cerchio.addEventListener("click", e => {
        e.stopPropagation();
        apri(tendina.hidden);
    });

    // Chiudi cliccando fuori o con Esc.
    document.addEventListener("click", e => {
        if (!profilo.contains(e.target)) apri(false);
    });
    document.addEventListener("keydown", e => {
        if (e.key === "Escape" && !tendina.hidden) {
            apri(false);
            cerchio.focus();
        }
    });

    return profilo;
}
