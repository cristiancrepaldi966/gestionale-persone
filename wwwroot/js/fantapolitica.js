// Pagina Politici ed eventi del modulo Fantapolitica (fantapolitica-gestione.html, in
// costruzione). La pagina Leghe ha il suo script: js/fantapolitica-leghe.js.
// Il livello richiesto è nell'attributo data-amministratore del <body>. Componenti comuni: componenti.js.

avviaPagina({
    modulo: MODULI.fantapolitica,
    amministratore: document.body.dataset.amministratore === "true"
});
