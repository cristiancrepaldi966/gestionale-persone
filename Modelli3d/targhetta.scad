// Targhetta con nome: piastra con angoli arrotondati, testo in rilievo e fori facoltativi.
// I parametri arrivano dal server (Services/CatalogoStampa3d.cs) con -D nome=valore.
testo = "CASA";
larghezza = 100;
altezza = 30;
spessore = 3;
dimensione_testo = 12;
rilievo = 1.2;
fori = true;
raggio = 4;

FONT = "DejaVu Sans:style=Bold";
$fn = 48;
r = min(raggio, altezza / 2 - 0.1, larghezza / 2 - 0.1);

module piastra() {
    hull()
        for (x = [r, larghezza - r], y = [r, altezza - r])
            translate([x, y, 0]) cylinder(r = r, h = spessore);
}

difference() {
    piastra();
    if (fori)
        for (x = [6, larghezza - 6])
            translate([x, altezza / 2, -1]) cylinder(d = 4, h = spessore + 2);
}

if (len(testo) > 0)
    translate([larghezza / 2, altezza / 2, spessore - 0.01])
        linear_extrude(rilievo + 0.01)
            text(testo, size = dimensione_testo, font = FONT, halign = "center", valign = "center");
