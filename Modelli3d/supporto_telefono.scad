// Supporto per telefono o tablet: base, bordo anteriore e schienale inclinato con rinforzo.
// Si stampa coricato su un fianco (la larghezza è l'altezza di stampa).
larghezza = 70;
spessore_dispositivo = 11;   // spessore del telefono con la cover
inclinazione = 65;           // gradi dello schienale rispetto al piano
altezza_schienale = 80;

t = 4;          // spessore del materiale
bordo = 12;     // altezza del bordo anteriore
x0 = 6 + t + spessore_dispositivo + 1;   // dove parte lo schienale
fine = x0 + altezza_schienale * cos(inclinazione);
base = fine + 15;

linear_extrude(larghezza) union() {
    square([base, t]);                                   // base
    translate([6, 0]) square([t, bordo]);                // bordo anteriore
    translate([x0, 0]) rotate(inclinazione) square([altezza_schienale, t]);   // schienale
    // rinforzo sotto lo schienale
    polygon([[x0 + t, t], [fine, altezza_schienale * sin(inclinazione) * 0.55], [fine + 8, t]]);
}
