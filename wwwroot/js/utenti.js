// Script della pagina utenti.html (separato dall'HTML: la Content-Security-Policy
// permette solo script caricati da file del sito).

const API = "/api/utenti";
let idCorrente = null;
const messaggio = () => document.getElementById("messaggio");

const formatoData = d => d
    ? new Date(d).toLocaleString("it-IT", { dateStyle: "short", timeStyle: "short" })
    : "mai";

async function caricaUtenti() {

    const lista = document.getElementById("listaUtenti");

    try {
        const response = await api(API);
        if (!response.ok) throw new Error();

        const utenti = await response.json();
        lista.replaceChildren(...utenti.map(rigaUtente));

    } catch {
        lista.replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento degli utenti."));
    }
}

function rigaUtente(u) {

    const riga = el("div", "list-group-item riga d-flex flex-column flex-sm-row justify-content-between gap-2 py-3");

    const info = el("div", "text-break");
    const nome = el("div", "fw-bold d-flex flex-wrap align-items-center gap-1");
    nome.appendChild(el("span", "me-1", u.email));
    if (u.id === idCorrente) nome.appendChild(etichetta("tu"));
    if (u.isAdmin) nome.appendChild(etichetta("Amministratore", "primary"));
    if (!u.attivo) nome.appendChild(etichetta("Disattivato", "danger"));
    if (u.bloccato) nome.appendChild(etichetta("Bloccato per tentativi errati", "danger"));
    info.appendChild(nome);
    info.appendChild(el("div", "small text-body-secondary",
        `Creato il ${formatoData(u.creatoIl)} · Ultimo accesso: ${formatoData(u.ultimoAccesso)}`));

    const azioni = el("div", "d-flex gap-2 flex-shrink-0 align-self-sm-center");
    azioni.appendChild(pulsante("Modifica", "btn btn-sm btn-light border", () => modificaUtente(riga, u)));
    if (u.id !== idCorrente) {
        azioni.appendChild(pulsante("Elimina", "btn btn-sm btn-danger", () => eliminaUtente(u)));
    }

    riga.append(info, azioni);
    return riga;
}

function modificaUtente(riga, u) {

    const casella = (testo, valore, disabilitata) => {
        const div = el("div", "form-check mb-0");
        const input = el("input", "form-check-input");
        input.type = "checkbox";
        input.id = `c${u.id}-${testo.length}`;
        input.checked = valore;
        input.disabled = disabilitata;
        const label = el("label", "form-check-label", testo);
        label.htmlFor = input.id;
        div.append(input, label);
        return { div, input };
    };

    const stessoUtente = u.id === idCorrente;
    const admin = casella("Amministratore", u.isAdmin, stessoUtente);
    const attivo = casella("Attivo (può accedere)", u.attivo, stessoUtente);

    const password = el("input", "form-control");
    password.type = "password";
    password.placeholder = "Nuova password (lascia vuoto per non cambiarla)";
    password.autocomplete = "new-password";
    password.setAttribute("aria-label", "Nuova password");

    const errore = el("div", "small text-danger");

    const contenitore = el("div", "d-grid gap-2 flex-grow-1");
    contenitore.append(el("div", "fw-bold text-break", u.email), password, admin.div, attivo.div, errore);

    const salva = async () => {

        if (password.value && password.value.length < 8) {
            errore.textContent = "La password deve avere almeno 8 caratteri.";
            return;
        }

        try {
            const response = await api(`${API}/${u.id}`, jsonBody("PUT", {
                isAdmin: admin.input.checked,
                attivo: attivo.input.checked,
                nuovaPassword: password.value || null
            }));

            if (!response.ok) {
                errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
                return;
            }

            mostraMessaggio(messaggio(), `Utente ${u.email} aggiornato.`, "successo");
            await caricaUtenti();

        } catch {
            errore.textContent = "Errore nel collegamento al server.";
        }
    };

    const azioni = el("div", "d-flex gap-2 flex-shrink-0 align-self-sm-start");
    azioni.appendChild(pulsante("Salva", "btn btn-sm btn-primary", salva));
    azioni.appendChild(pulsante("Annulla", "btn btn-sm btn-light border", () => riga.replaceWith(rigaUtente(u))));

    riga.replaceChildren(contenitore, azioni);
    password.focus();
}

async function eliminaUtente(u) {

    if (!confirm(`Eliminare l'utente ${u.email}?`)) return;

    try {
        const response = await api(`${API}/${u.id}`, { method: "DELETE" });

        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'eliminazione."));
            return;
        }

        mostraMessaggio(messaggio(), `Utente ${u.email} eliminato.`, "successo");
        await caricaUtenti();

    } catch {
        alert("Errore nel collegamento al server.");
    }
}

alInvio(document.getElementById("formNuovo"), async e => {

    try {
        const response = await api(API, jsonBody("POST", {
            email: document.getElementById("email").value.trim(),
            password: document.getElementById("password").value,
            isAdmin: document.getElementById("isAdmin").checked
        }));

        if (!response.ok) {
            mostraMessaggio(messaggio(), await messaggioErrore(response, "Errore durante la creazione."));
            return;
        }

        const creato = await response.json();
        e.target.reset();
        mostraMessaggio(messaggio(), `Utente ${creato.email} creato.`, "successo");
        await caricaUtenti();

    } catch {
        mostraMessaggio(messaggio(), "Errore nel collegamento al server.");
    }
});

avviaPagina({ soloAdmin: true }).then(u => {
    idCorrente = u.id;
    return caricaUtenti();
});
