// Pagina per chi non ha accesso a nessun modulo: se nel frattempo ne ha ricevuto
// uno, va direttamente alla sua prima pagina.
avviaPagina().then(utente => {
    const pagina = paginaIniziale(utente);
    if (pagina !== "/nessun-accesso.html") location.href = pagina;
});
