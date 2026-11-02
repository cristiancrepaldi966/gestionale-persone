// Distanziale / rondella: anello con diametro interno, esterno e spessore.
diametro_interno = 8.4;
diametro_esterno = 16;
spessore = 2;

$fn = 96;

difference() {
    cylinder(d = diametro_esterno, h = spessore);
    translate([0, 0, -1]) cylinder(d = diametro_interno, h = spessore + 2);
}
