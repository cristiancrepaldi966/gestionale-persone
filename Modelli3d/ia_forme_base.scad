// Forme base del modulo IA (pagina IA 3D): blocco, piastra, cilindro, tubo, sfera, cono.
// I parametri arrivano dal server (Services/FormeBaseIa3d.cs) con -D nome=valore, già
// controllati; anche i centri dei fori li calcola il server (posizioni_fori).
// Il pezzo è centrato sull'origine e appoggiato su z = 0.
forma = "blocco";
larghezza = 40;
profondita = 20;
altezza = 10;
diametro = 20;
diametro_interno = 0;
diametro_cima = 0;
raggio_angoli = 0;
fori = 0;
diametro_fori = 4;
posizione_fori = "centro";
posizioni_fori = [];   // [[x, y], ...] in mm, rispetto al centro del pezzo

$fn = 96;

// Rettangolo l × p alto h, centrato, con gli angoli arrotondati di raggio r.
module rettangolo(l, p, r, h) {
    if (r <= 0)
        translate([-l / 2, -p / 2, 0]) cube([l, p, h]);
    else
        hull()
            for (x = [-l / 2 + r, l / 2 - r], y = [-p / 2 + r, p / 2 - r])
                translate([x, y, 0]) cylinder(r = r, h = h);
}

// Fori passanti, da sottrarre a un pezzo alto h.
module fori_passanti(h) {
    for (f = posizioni_fori)
        translate([f[0], f[1], -1]) cylinder(d = diametro_fori, h = h + 2, $fn = 48);
}

if (forma == "blocco" || forma == "piastra") {
    difference() {
        rettangolo(larghezza, profondita, raggio_angoli, altezza);
        fori_passanti(altezza);
    }
} else if (forma == "cilindro") {
    difference() {
        cylinder(d = diametro, h = altezza);
        fori_passanti(altezza);
    }
} else if (forma == "tubo") {
    difference() {
        cylinder(d = diametro, h = altezza);
        translate([0, 0, -1]) cylinder(d = diametro_interno, h = altezza + 2);
    }
} else if (forma == "sfera") {
    translate([0, 0, diametro / 2]) sphere(d = diametro);
} else if (forma == "cono") {
    cylinder(d1 = diametro, d2 = diametro_cima, h = altezza);
}
