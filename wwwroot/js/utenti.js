// Pagina Utenti (utenti.html, solo amministratori). Componenti e funzioni comuni: componenti.js.

const API = "/api/utenti";
let utenti = [];
let idCorrente = null;
let inModifica = null; // utente in modifica, null se è nuovo

const $ = id => document.getElementById(id);

const formatoDataOra = d => d
    ? new Date(d).toLocaleString("it-IT", { dateStyle: "short", timeStyle: "short" })
    : "mai";

const ricerca = pannelloRicerca({
    bottone: $("cerca"),
    pannello: $("pannelloFiltri"),
    pallino: $("pallinoFiltri"),
    azzera: $("azzera"),
    campi: [$("filtroEmail"), $("filtroRuolo"), $("filtroStato")],
    applica: () => mostraUtenti()
});

const modulo = moduloFinestra($("finestra"), { primoCampo: $("email") });

async function caricaUtenti() {
    try {
        const response = await api(API);
        if (!response.ok) throw new Error();
        utenti = await response.json();
        mostraUtenti();
    } catch {
        $("listaUtenti").replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento degli utenti."));
    }
}

// Elenco filtrato per email, ruolo e stato (la ricerca avviene qui, gli utenti sono pochi).
function mostraUtenti() {

    const { filtroEmail, filtroRuolo, filtroStato } = ricerca.valori();
    const testo = filtroEmail.toLowerCase();

    const trovati = utenti.filter(u =>
        u.email.includes(testo) &&
        (filtroRuolo === "" || (filtroRuolo === "admin") === u.isAdmin) &&
        (filtroStato === "" ||
            (filtroStato === "attivi" && u.attivo) ||
            (filtroStato === "disattivati" && !u.attivo) ||
            (filtroStato === "bloccati" && u.bloccato)));

    const lista = $("listaUtenti");

    $("riepilogo").replaceChildren(
        el("span", "text-body-secondary", `${trovati.length} ${trovati.length === 1 ? "utente" : "utenti"}`));

    if (trovati.length === 0) {
        lista.replaceChildren(el("p", "p-3 mb-0 text-body-secondary", "Nessun utente corrisponde alla ricerca."));
        return;
    }

    lista.replaceChildren(...trovati.map(rigaUtente));
}

function rigaUtente(u) {

    const riga = el("div", "list-group-item riga d-flex flex-column flex-sm-row justify-content-between gap-2 py-3");

    const info = el("div", "text-break");
    const nome = el("div", "fw-bold fs-5 d-flex flex-wrap align-items-center gap-1");
    nome.appendChild(el("span", "me-1", u.email));
    if (u.id === idCorrente) nome.appendChild(etichetta("tu"));
    if (u.isAdmin) nome.appendChild(etichetta("Amministratore", "primary"));
    if (!u.attivo) nome.appendChild(etichetta("Disattivato", "danger"));
    if (u.bloccato) nome.appendChild(etichetta("Bloccato per tentativi errati", "danger"));
    info.appendChild(nome);
    info.appendChild(el("div", "small text-body-secondary",
        `Creato il ${formatoDataOra(u.creatoIl)} · Ultimo accesso: ${formatoDataOra(u.ultimoAccesso)}`));

    const azioni = el("div", "d-flex gap-2 flex-shrink-0 align-self-sm-center");
    azioni.appendChild(pulsante("Modifica", "btn btn-sm btn-light border", () => apri(u)));
    if (u.id !== idCorrente) {
        azioni.appendChild(pulsante("Elimina", "btn btn-sm btn-danger", () => elimina(u)));
    }

    riga.append(info, azioni);
    return riga;
}

function apri(u) {
    inModifica = u || null;
    const stessoUtente = !!u && u.id === idCorrente;

    $("email").value = u ? u.email : "";
    $("email").readOnly = !!u;                 // l'email non si cambia
    $("email").classList.toggle("bg-body-tertiary", !!u);
    $("password").value = "";
    $("etichettaPassword").textContent = u ? "Nuova password" : "Password";
    $("aiutoPassword").textContent = u
        ? "Lascia vuoto per non cambiarla. Almeno 8 caratteri."
        : "Almeno 8 caratteri.";
    $("isAdmin").checked = u ? u.isAdmin : false;
    $("isAdmin").disabled = stessoUtente;
    $("attivo").checked = u ? u.attivo : true;
    $("attivo").disabled = stessoUtente;
    $("rigaAttivo").hidden = !u;                // un utente nuovo è sempre attivo
    $("aiutoSeStesso").hidden = !stessoUtente;
    $("erroreFinestra").textContent = "";

    modulo.apri(u ? "Modifica utente" : "Nuovo utente");
    if (u && !schermoTouch) $("password").focus();
}

async function salva() {

    const errore = $("erroreFinestra");
    const password = $("password").value;

    if (!inModifica && !$("email").value.trim()) {
        errore.textContent = "Inserisci l'email.";
        return;
    }

    if ((!inModifica || password) && password.length < 8) {
        errore.textContent = "La password deve avere almeno 8 caratteri.";
        return;
    }

    try {
        const response = inModifica
            ? await api(`${API}/${inModifica.id}`, jsonBody("PUT", {
                isAdmin: $("isAdmin").checked,
                attivo: $("attivo").checked,
                nuovaPassword: password || null
            }))
            : await api(API, jsonBody("POST", {
                email: $("email").value.trim(),
                password,
                isAdmin: $("isAdmin").checked
            }));

        if (!response.ok) {
            errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
            return;
        }

        modulo.chiudi();
        await caricaUtenti();

    } catch {
        errore.textContent = "Errore nel collegamento al server.";
    }
}

async function elimina(u) {

    if (!confirm(`Eliminare l'utente ${u.email}?`)) return;

    try {
        const response = await api(`${API}/${u.id}`, { method: "DELETE" });
        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'eliminazione."));
            return;
        }
        await caricaUtenti();
    } catch {
        alert("Errore nel collegamento al server.");
    }
}

$("nuova").addEventListener("click", () => apri(null));
$("annulla").addEventListener("click", () => modulo.chiudi());
alInvio($("formUtente"), salva);

avviaPagina({ soloAdmin: true }).then(u => {
    idCorrente = u.id;
    return caricaUtenti();
});
