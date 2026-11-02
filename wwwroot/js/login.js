// Script della pagina login.html (separato dall'HTML: la Content-Security-Policy
// permette solo script caricati da file del sito).

// Pagina a cui tornare dopo l'accesso: solo pagine di questo sito.
// L'indirizzo viene interpretato dal browser (come farebbe navigando), così
// varianti come "//sito.com" o "/\sito.com" non possono portare fuori.
function paginaRitorno() {
    const r = new URLSearchParams(location.search).get("ritorno") || "/";
    try {
        const url = new URL(r, location.origin);
        if (url.origin === location.origin && (url.protocol === "http:" || url.protocol === "https:")) {
            return url.pathname + url.search + url.hash;
        }
    } catch { }
    return "/";
}

// Se l'accesso è già stato fatto, vai direttamente all'app.
fetch("/api/auth/me", { credentials: "same-origin" })
    .then(r => { if (r.ok) location.href = paginaRitorno(); })
    .catch(() => { });

alInvio(document.getElementById("form"), async () => {

    const messaggio = document.getElementById("messaggio");
    messaggio.textContent = "";

    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;

    if (!email || !password) {
        mostraMessaggio(messaggio, "Inserisci email e password.");
        return;
    }

    // Su Render il server si "addormenta": il primo accesso può richiedere fino a un minuto.
    const avviso = setTimeout(() => {
        messaggio.className = "small mt-2 text-body-secondary";
        messaggio.textContent = "Il server si sta avviando, può volerci fino a un minuto...";
    }, 4000);

    try {

        const response = await fetch("/api/auth/login", {
            credentials: "same-origin",
            ...jsonBody("POST", { email, password })
        });

        clearTimeout(avviso);

        if (!response.ok) {
            mostraMessaggio(messaggio, await messaggioErrore(response, "Accesso non riuscito."));
            document.getElementById("password").value = "";
            return;
        }

        messaggio.textContent = "";
        location.href = paginaRitorno();
        // Il cerchietto continua a girare finché la nuova pagina non si apre.
        return new Promise(() => { });

    } catch {
        clearTimeout(avviso);
        mostraMessaggio(messaggio, "Errore nel collegamento al server.");
    }
});
