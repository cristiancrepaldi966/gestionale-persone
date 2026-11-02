// Portapenne: cilindro o esagono con fondo pieno e divisori interni facoltativi.
forma = "cilindro";
diametro = 70;
altezza = 100;
pareti = 2;
scomparti = 1;    // 1 = nessun divisorio

fondo = 2;
lati = forma == "esagono" ? 6 : 96;

difference() {
    cylinder(d = diametro, h = altezza, $fn = lati);
    translate([0, 0, fondo]) cylinder(d = diametro - 2 * pareti / cos(180 / lati), h = altezza, $fn = lati);
}

if (scomparti > 1)
    intersection() {
        cylinder(d = diametro - pareti, h = altezza * 0.8, $fn = lati);
        for (i = [0 : scomparti - 1])
            rotate(i * 360 / scomparti) translate([0, -pareti / 2, 0]) cube([diametro / 2, pareti, altezza * 0.8]);
    }
