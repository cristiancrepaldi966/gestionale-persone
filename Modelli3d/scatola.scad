// Scatola con coperchio a incastro: la scatola e, accanto, il coperchio da stampare capovolto.
larghezza = 80;      // misure interne
profondita = 60;
altezza = 40;
pareti = 2;
scomparti = 1;       // divisori lungo la larghezza
tolleranza = 0.3;    // gioco tra coperchio e scatola

fondo = 2;
bordo = 6;           // altezza del bordo del coperchio
$fn = 32;

L = larghezza + 2 * pareti;
P = profondita + 2 * pareti;

module scatola() {
    difference() {
        cube([L, P, altezza + fondo]);
        translate([pareti, pareti, fondo]) cube([larghezza, profondita, altezza + 1]);
    }
    if (scomparti > 1)
        for (i = [1 : scomparti - 1])
            translate([pareti + i * larghezza / scomparti - pareti / 2, pareti, 0])
                cube([pareti, profondita, altezza * 0.85 + fondo]);
}

module coperchio() {
    Lc = L + 2 * tolleranza + 2 * pareti;
    Pc = P + 2 * tolleranza + 2 * pareti;
    difference() {
        cube([Lc, Pc, bordo + fondo]);
        translate([pareti, pareti, fondo]) cube([L + 2 * tolleranza, P + 2 * tolleranza, bordo + 1]);
    }
}

scatola();
translate([L + 10, 0, 0]) coperchio();
