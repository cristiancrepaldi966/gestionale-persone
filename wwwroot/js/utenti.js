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
    campi: [$("filtroRuolo"), $("filtroTesto"), $("filtroStato")],
    applica: () => mostraUtenti()
});

const modulo = moduloFinestra($("finestra"), { primoCampo: $("nome") });

const nomeCompleto = u => `${u.nome} ${u.cognome}`.trim();

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

// Elenco filtrato per ruolo, nome/cognome/email e stato (la ricerca avviene qui, gli utenti sono pochi).
function mostraUtenti() {

    const { filtroRuolo, filtroTesto, filtroStato } = ricerca.valori();
    const testo = filtroTesto.toLocaleLowerCase("it-IT");

    const trovati = utenti.filter(u =>
        `${u.nome} ${u.cognome} ${u.cognome} ${u.nome} ${u.email}`.toLocaleLowerCase("it-IT").includes(testo) &&
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

    // 1. Ruolo
    const ruolo = el("div", "d-flex flex-wrap align-items-center gap-1 mb-1");
    ruolo.appendChild(u.isAdmin ? etichetta("Amministratore", "primary") : etichetta("Utente", "secondary"));
    if (u.id === idCorrente) ruolo.appendChild(etichetta("tu", "light"));
    info.appendChild(ruolo);

    // 2. Nome e cognome
    info.appendChild(nomeCompleto(u)
        ? el("div", "fw-bold fs-5", nomeCompleto(u))
        : el("div", "fs-5 fst-italic text-body-secondary", "Nome non inserito"));

    // 3. Email
    info.appendChild(el("div", null, u.email));

    // 4. Il resto: stato e date
    const stato = el("div", "d-flex flex-wrap align-items-center gap-1 small text-body-secondary mt-1");
    if (!u.attivo) stato.appendChild(etichetta("Disattivato", "danger"));
    if (u.bloccato) stato.appendChild(etichetta("Bloccato per tentativi errati", "danger"));
    stato.appendChild(el("span", null,
        `Creato il ${formatoDataOra(u.creatoIl)} · Ultimo accesso: ${formatoDataOra(u.ultimoAccesso)}`));
    info.appendChild(stato);

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

    $("ruolo").value = u && u.isAdmin ? "admin" : "utente";
    $("ruolo").disabled = stessoUtente;
    $("nome").value = u ? u.nome : "";
    $("cognome").value = u ? u.cognome : "";
    $("email").value = u ? u.email : "";
    $("email").readOnly = !!u;                 // l'email non si cambia
    $("email").classList.toggle("bg-body-tertiary", !!u);
    $("password").value = "";
    $("etichettaPassword").textContent = u ? "Nuova password" : "Password";
    $("aiutoPassword").textContent = u
        ? "Lascia vuoto per non cambiarla. Almeno 8 caratteri."
        : "Almeno 8 caratteri.";
    $("attivo").checked = u ? u.attivo : true;
    $("attivo").disabled = stessoUtente;
    $("rigaAttivo").hidden = !u;                // un utente nuovo è sempre attivo
    $("aiutoSeStesso").hidden = !stessoUtente;
    $("erroreFinestra").textContent = "";

    modulo.apri(u ? "Modifica utente" : "Nuovo utente");
}

async function salva() {

    const errore = $("erroreFinestra");
    const password = $("password").value;
    const nome = $("nome").value.trim();
    const cognome = $("cognome").value.trim();
    const isAdmin = $("ruolo").value === "admin";

    if (!nome || !cognome) {
        errore.textContent = "Inserisci nome e cognome.";
        return;
    }

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
                nome,
                cognome,
                isAdmin,
                attivo: $("attivo").checked,
                nuovaPassword: password || null
            }))
            : await api(API, jsonBody("POST", {
                nome,
                cognome,
                email: $("email").value.trim(),
                password,
                isAdmin
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

    if (!confirm(`Eliminare l'utente ${nomeCompleto(u) || u.email} (${u.email})?`)) return;

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
