// Portachiavi: forma (rettangolo, cerchio, cuore) con testo in rilievo e anello per la chiave.
testo = "M";
forma = "rettangolo";
larghezza = 50;
spessore = 3;
dimensione_testo = 10;
rilievo = 1;

FONT = "DejaVu Sans:style=Bold";
$fn = 64;
foro = 5;
altezza = forma == "rettangolo" ? larghezza * 0.45
        : forma == "cuore" ? larghezza / (2 * 0.3536 + 1) * 1.5607
        : larghezza;

module sagoma() {
    if (forma == "cerchio") {
        translate([larghezza / 2, larghezza / 2]) circle(d = larghezza);
    } else if (forma == "cuore") {
        // Quadrato ruotato di 45° con due cerchi sui lati superiori; larghezza totale = larghezza.
        s = larghezza / (2 * 0.3536 + 1);
        translate([larghezza / 2, 0])
            union() {
                rotate(45) square(s);
                translate([-0.3536 * s, 1.0607 * s]) circle(d = s);
                translate([ 0.3536 * s, 1.0607 * s]) circle(d = s);
            }
    } else {
        r = 4;
        hull() for (x = [r, larghezza - r], y = [r, altezza - r]) translate([x, y]) circle(r = r);
    }
}

// Linguetta con il foro a sinistra.
module linguetta() {
    hull() {
        translate([0, altezza / 2]) circle(d = foro + 6);
        translate([foro + 6, altezza / 2 - (foro + 6) / 2]) square([1, foro + 6]);
    }
}

difference() {
    linear_extrude(spessore) union() {
        translate([foro + 3, 0]) sagoma();
        translate([foro / 2 + 3, 0]) linguetta();
    }
    translate([foro / 2 + 3, altezza / 2, -1]) cylinder(d = foro, h = spessore + 2);
}

if (len(testo) > 0)
    translate([foro + 3 + larghezza / 2, forma == "cuore" ? altezza * 0.6 : altezza / 2, spessore - 0.01])
        linear_extrude(rilievo + 0.01)
            text(testo, size = dimensione_testo, font = FONT, halign = "center", valign = "center");
