// Script della pagina categorie.html (separato dall'HTML: la Content-Security-Policy
// permette solo script caricati da file del sito).

const API = "/api/categorie";
const messaggio = () => document.getElementById("messaggio");

async function caricaCategorie() {

    const lista = document.getElementById("listaCategorie");

    try {
        const response = await api(API);
        if (!response.ok) throw new Error();
        const categorie = await response.json();

        if (categorie.length === 0) {
            lista.replaceChildren(el("p", "p-3 mb-0 text-body-secondary", "Nessuna categoria."));
            return;
        }

        lista.replaceChildren(...categorie.map(rigaCategoria));

    } catch {
        lista.replaceChildren(el("p", "p-3 mb-0 text-danger", "Errore nel caricamento delle categorie."));
    }
}

function rigaCategoria(c) {

    const riga = el("div", "list-group-item riga d-flex justify-content-between align-items-center gap-3 py-3");

    const info = el("div", "text-break");
    info.appendChild(el("div", "fw-bold", c.nome));
    info.appendChild(el("div", "small text-body-secondary",
        c.numeroUscite === 0 ? "Nessuna uscita" :
        c.numeroUscite === 1 ? "1 uscita" : `${c.numeroUscite} uscite`));

    const azioni = el("div", "d-flex gap-2 flex-shrink-0");
    azioni.appendChild(pulsante("Rinomina", "btn btn-sm btn-light border", () => rinomina(riga, c)));
    azioni.appendChild(pulsante("Elimina", "btn btn-sm btn-danger", () => elimina(c)));

    riga.append(info, azioni);
    return riga;
}

function rinomina(riga, c) {

    const input = el("input", "form-control");
    input.value = c.nome;
    input.maxLength = 50;
    input.setAttribute("aria-label", "Nuovo nome");

    const errore = el("div", "small mt-1 text-danger");
    const contenitore = el("div", "flex-grow-1");
    contenitore.append(input, errore);

    const annulla = () => riga.replaceWith(rigaCategoria(c));

    const salva = async () => {
        const nome = input.value.trim();
        if (!nome) {
            errore.textContent = "Inserisci un nome.";
            return;
        }
        try {
            const response = await api(`${API}/${c.id}`, jsonBody("PUT", { nome }));
            if (!response.ok) {
                errore.textContent = await messaggioErrore(response, "Errore durante il salvataggio.");
                return;
            }
            await caricaCategorie();
        } catch {
            errore.textContent = "Errore nel collegamento al server.";
        }
    };

    const bottoneSalva = pulsante("Salva", "btn btn-sm btn-primary", salva);

    input.addEventListener("keydown", e => {
        if (e.key === "Enter") bottoneSalva.click();
        if (e.key === "Escape") annulla();
    });

    const azioni = el("div", "d-flex gap-2 flex-shrink-0 align-self-start");
    azioni.append(bottoneSalva, pulsante("Annulla", "btn btn-sm btn-light border", annulla));

    riga.replaceChildren(contenitore, azioni);
    riga.classList.remove("align-items-center");
    input.focus();
}

async function elimina(c) {

    if (!confirm(`Eliminare la categoria "${c.nome}"?`)) return;

    try {
        const response = await api(`${API}/${c.id}`, { method: "DELETE" });
        if (!response.ok) {
            alert(await messaggioErrore(response, "Errore durante l'eliminazione."));
            return;
        }
        mostraMessaggio(messaggio(), `Categoria "${c.nome}" eliminata.`, "successo");
        await caricaCategorie();
    } catch {
        alert("Errore nel collegamento al server.");
    }
}

alInvio(document.getElementById("formNuova"), async () => {

    const input = document.getElementById("nome");
    const nome = input.value.trim();

    if (!nome) {
        mostraMessaggio(messaggio(), "Inserisci il nome della categoria.");
        return;
    }

    try {
        const response = await api(API, jsonBody("POST", { nome }));
        if (!response.ok) {
            mostraMessaggio(messaggio(), await messaggioErrore(response, "Errore durante l'inserimento."));
            return;
        }
        const nuova = await response.json();
        input.value = "";
        mostraMessaggio(messaggio(), `Categoria "${nuova.nome}" aggiunta.`, "successo");
        await caricaCategorie();
    } catch {
        mostraMessaggio(messaggio(), "Errore nel collegamento al server.");
    }
});

avviaPagina().then(caricaCategorie);
