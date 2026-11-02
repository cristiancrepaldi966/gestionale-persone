// Script della pagina password.html (separato dall'HTML: la Content-Security-Policy
// permette solo script caricati da file del sito).

avviaPagina();

alInvio(document.getElementById("form"), async () => {

    const messaggio = document.getElementById("messaggio");
    const attuale = document.getElementById("attuale").value;
    const nuova = document.getElementById("nuova").value;

    if (!attuale) {
        mostraMessaggio(messaggio, "Inserisci la password attuale.");
        return;
    }

    if (nuova.length < 8) {
        mostraMessaggio(messaggio, "La nuova password deve avere almeno 8 caratteri.");
        return;
    }

    if (nuova !== document.getElementById("conferma").value) {
        mostraMessaggio(messaggio, "Le due password nuove non coincidono.");
        return;
    }

    try {

        const response = await api("/api/auth/password", jsonBody("POST", {
            passwordAttuale: attuale,
            nuovaPassword: nuova
        }));

        if (!response.ok) {
            mostraMessaggio(messaggio, await messaggioErrore(response, "Errore durante il cambio password."));
            return;
        }

        alert("Password cambiata. Accedi di nuovo con la nuova password.");
        location.href = "/login.html";

    } catch {
        if (!messaggio.textContent) mostraMessaggio(messaggio, "Errore nel collegamento al server.");
    }
});
