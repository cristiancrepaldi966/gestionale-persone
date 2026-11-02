// Sottobicchiere: cerchio, esagono o quadrato con bordo rialzato e testo inciso facoltativo.
forma = "cerchio";
diametro = 95;
spessore = 4;
bordo = 1.5;      // altezza del bordo rialzato (0 = piatto)
testo = "";

FONT = "DejaVu Sans:style=Bold";
dimensione_testo = diametro / 8;
lati = forma == "esagono" ? 6 : forma == "quadrato" ? 4 : 96;
rot = forma == "quadrato" ? 45 : 0;

difference() {
    rotate(rot) cylinder(d = diametro, h = spessore + bordo, $fn = lati);
    if (bordo > 0)
        translate([0, 0, spessore]) rotate(rot) cylinder(d = diametro - 6, h = bordo + 1, $fn = lati);
    if (len(testo) > 0)
        translate([0, 0, spessore - 0.8])
            linear_extrude(1 + bordo)
                text(testo, size = dimensione_testo, font = FONT, halign = "center", valign = "center");
}
