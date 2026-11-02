// Pagine del modulo Fantapolitica (per ora in costruzione). Il livello richiesto è
// nell'attributo data-amministratore del <body>. Componenti comuni: componenti.js.

avviaPagina({
    modulo: MODULI.fantapolitica,
    amministratore: document.body.dataset.amministratore === "true"
});
