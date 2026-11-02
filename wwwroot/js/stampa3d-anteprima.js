// Anteprima 3D di un file STL con three.js (lib/three): l'oggetto si ruota con il mouse
// o con un dito, si ingrandisce con la rotella o con due dita.
// Uso: const a = new Anteprima(contenitore); a.mostra(arrayBufferStl);

import * as THREE from "../lib/three/three.module.min.js";
import { STLLoader } from "../lib/three/STLLoader.js";
import { OrbitControls } from "../lib/three/OrbitControls.js";

export class Anteprima {
    constructor(contenitore) {
        this.contenitore = contenitore;

        this.scena = new THREE.Scene();
        this.camera = new THREE.PerspectiveCamera(40, 4 / 3, 0.1, 5000);
        this.camera.up.set(0, 0, 1);   // in OpenSCAD l'asse verticale è Z

        this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        contenitore.prepend(this.renderer.domElement);

        this.controlli = new OrbitControls(this.camera, this.renderer.domElement);
        this.controlli.enableDamping = true;

        this.scena.add(new THREE.HemisphereLight(0xffffff, 0x8a8a8a, 2.2));
        const luce = new THREE.DirectionalLight(0xffffff, 1.6);
        luce.position.set(1, -1.5, 2);
        this.scena.add(luce);

        this.materiale = new THREE.MeshStandardMaterial({ color: 0xeb6834, roughness: 0.55, metalness: 0.05 });
        this.oggetto = null;
        this.griglia = null;

        new ResizeObserver(() => this.ridimensiona()).observe(contenitore);
        this.ridimensiona();

        const ciclo = () => {
            this.controlli.update();
            this.renderer.render(this.scena, this.camera);
            requestAnimationFrame(ciclo);
        };
        ciclo();
    }

    ridimensiona() {
        const { clientWidth: l, clientHeight: a } = this.contenitore;
        if (!l || !a) return;
        this.renderer.setSize(l, a, false);
        this.camera.aspect = l / a;
        this.camera.updateProjectionMatrix();
    }

    // Mostra l'STL; se cambiano solo le misure, l'inquadratura resta quella scelta dall'utente.
    mostra(buffer, { reinquadra = false } = {}) {
        const geometria = new STLLoader().parse(buffer);
        geometria.computeVertexNormals();
        geometria.computeBoundingBox();
        const box = geometria.boundingBox;
        const dimensioni = box.getSize(new THREE.Vector3());
        // Al centro del piano, appoggiato su z = 0.
        geometria.translate(-(box.min.x + box.max.x) / 2, -(box.min.y + box.max.y) / 2, -box.min.z);

        const primaVolta = !this.oggetto;
        if (this.oggetto) {
            this.scena.remove(this.oggetto);
            this.oggetto.geometry.dispose();
        }
        this.oggetto = new THREE.Mesh(geometria, this.materiale);
        this.scena.add(this.oggetto);

        // Piano di stampa: griglia a quadretti da 10 mm.
        const lato = Math.max(50, Math.ceil(Math.max(dimensioni.x, dimensioni.y) * 1.4 / 10) * 10);
        if (this.griglia) this.scena.remove(this.griglia);
        this.griglia = new THREE.GridHelper(lato, lato / 10, 0xadb5bd, 0xdee2e6);
        this.griglia.rotation.x = Math.PI / 2;
        this.scena.add(this.griglia);

        if (primaVolta || reinquadra) {
            const raggio = dimensioni.length() / 2 || 10;
            this.controlli.target.set(0, 0, dimensioni.z / 2);
            this.camera.position.set(raggio * 1.6, -raggio * 2.2, raggio * 1.6 + dimensioni.z / 2);
            this.camera.near = raggio / 100;
            this.camera.far = raggio * 100;
            this.camera.updateProjectionMatrix();
            this.controlli.update();
        }

        return { x: dimensioni.x, y: dimensioni.y, z: dimensioni.z };
    }
}
