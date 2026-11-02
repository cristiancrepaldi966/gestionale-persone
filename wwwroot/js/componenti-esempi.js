// Script della pagina componenti.html (separato dall'HTML: la Content-Security-Policy
// permette solo script caricati da file del sito).

// Esempi interattivi (nessun dato reale).
const utenteEsempio = { id: 0, nome: "Mario", cognome: "Rossi", email: "admin@example.com", superAdmin: true, moduli: { finanze: "Amministratore", admin: "Amministratore" } };
document.getElementById("barraEsempio").append(menuPagine(utenteEsempio), menuProfilo(utenteEsempio));

const attesa = ms => new Promise(r => setTimeout(r, ms));
document.getElementById("esempioAttesa").append(
    pulsante("Salva (prova)", "btn btn-primary", () => attesa(1500)),
    pulsante("Elimina (prova)", "btn btn-sm btn-danger align-self-center", () => attesa(1500)));

document.getElementById("esempioEtichette").append(
    etichetta("tu"), etichetta("Amministratore", "primary"),
    etichetta("Disattivato", "danger"), etichetta("Trasporti", "light"));

document.getElementById("esempioIcone").append(
    iconaCategoria("house-door", "#2a78d6"), iconaCategoria("cart3", "#eb6834"),
    iconaCategoria("lightning-charge", "#eda100"), iconaCategoria("car-front", "#1baf7a"),
    iconaCategoria("heart-pulse", "#e34948", true));
