// Gancio a muro: piastra con due fori per le viti e braccio con punta rialzata.
// Si stampa coricato su un fianco, così è più robusto.
larghezza = 20;
altezza = 60;      // altezza della piastra
sporgenza = 40;    // lunghezza del braccio
spessore = 5;
diametro_foro = 4;

punta = 15;
$fn = 32;

difference() {
    linear_extrude(larghezza) union() {
        square([spessore, altezza]);                                  // piastra
        square([sporgenza, spessore]);                                // braccio
        translate([sporgenza - spessore, 0]) square([spessore, punta]);   // punta
        // raccordo tra piastra e braccio
        polygon([[spessore, spessore], [spessore + 10, spessore], [spessore, spessore + 10]]);
    }
    for (y = [altezza - 10, altezza / 2 + 5])
        translate([-1, y, larghezza / 2]) rotate([0, 90, 0]) cylinder(d = diametro_foro, h = spessore + 2);
}
