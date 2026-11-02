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
        barra.replaceChildren();
        barra.appendChild(el("span", null, utente.email));

        const link = (testo, href) => {
            const a = el("a", null, testo);
            a.href = href;
            return a;
        };

        barra.appendChild(link("Uscite", "/"));
        barra.appendChild(link("Categorie", "/categorie.html"));
        if (utente.isAdmin) barra.appendChild(link("Utenti", "/utenti.html"));
        barra.appendChild(link("Password", "/password.html"));
        barra.appendChild(pulsante("Esci", null, esci));
    }

    document.body.classList.remove("in-attesa");
    return utente;
}
