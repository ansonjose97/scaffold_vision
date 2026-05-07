import * as THREE from 'three';
import type { BuildingDimensions, ScaffoldingPreferences } from '../domain/types';

const COLORS = {
  building: 0x2a2f38,
  buildingEdge: 0x3a4250,
  ground: 0x14171c,
  gridMajor: 0x2a3038,
  gridMinor: 0x1d2128,
  standard: 0xff7a18,
  ledger: 0x38bdf8,
  platform: 0xa3a3a3,
  brace: 0xc084fc
};

/**
 * Manages the Three.js scene, the building stand-in, and the generated scaffold mesh.
 *
 * The viewport is intentionally separated from any framework: it exposes a small
 * imperative API (`updateBuilding`, `renderScaffold`, `clearScaffold`) so it can
 * be driven from any UI layer without introducing a dependency.
 */
export class Viewport {
  private readonly scene: THREE.Scene;
  private readonly camera: THREE.PerspectiveCamera;
  private readonly renderer: THREE.WebGLRenderer;
  private readonly buildingGroup: THREE.Group;
  private readonly scaffoldGroup: THREE.Group;
  private readonly resizeObserver: ResizeObserver;

  private orbitTarget = new THREE.Vector3(0, 3, 0);
  private orbitDistance = 25;
  private orbitTheta = Math.PI / 5;
  private orbitPhi = Math.PI / 3;
  private isDragging = false;
  private lastPointer = { x: 0, y: 0 };

  constructor(private readonly container: HTMLElement) {
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x0e1116);
    this.scene.fog = new THREE.Fog(0x0e1116, 35, 80);

    this.camera = new THREE.PerspectiveCamera(45, 1, 0.1, 200);
    this.updateCameraFromOrbit();

    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    this.renderer.setPixelRatio(window.devicePixelRatio);
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    container.appendChild(this.renderer.domElement);

    this.setupLighting();
    this.setupGround();

    this.buildingGroup = new THREE.Group();
    this.scaffoldGroup = new THREE.Group();
    this.scene.add(this.buildingGroup);
    this.scene.add(this.scaffoldGroup);

    this.attachInputHandlers();

    this.resizeObserver = new ResizeObserver(() => this.handleResize());
    this.resizeObserver.observe(container);
    this.handleResize();

    this.animate();
  }

  // === Public API ===

  updateBuilding(dimensions: BuildingDimensions): void {
    this.disposeGroup(this.buildingGroup);

    const { widthMeters: w, heightMeters: h, depthMeters: d } = dimensions;

    const geometry = new THREE.BoxGeometry(w, h, d);
    const material = new THREE.MeshStandardMaterial({
      color: COLORS.building,
      roughness: 0.85,
      metalness: 0.1
    });
    const mesh = new THREE.Mesh(geometry, material);
    mesh.position.set(0, h / 2, 0);
    mesh.castShadow = true;
    mesh.receiveShadow = true;

    const edges = new THREE.LineSegments(
      new THREE.EdgesGeometry(geometry),
      new THREE.LineBasicMaterial({ color: COLORS.buildingEdge })
    );
    edges.position.copy(mesh.position);

    this.buildingGroup.add(mesh, edges);

    this.orbitTarget.set(0, h / 2, 0);
    this.orbitDistance = Math.max(20, Math.max(w, d) * 2.2);
    this.updateCameraFromOrbit();
  }

  /**
   * Render a wireframe scaffold around the building based on the recommendation
   * geometry. This is a stylised representation, not a precise CAD layout.
   */
  renderScaffold(
    building: BuildingDimensions,
    prefs: ScaffoldingPreferences,
    summary: { bays: number; lifts: number }
  ): void {
    this.clearScaffold();

    const { widthMeters: w, heightMeters: h, depthMeters: d } = building;
    const { bays, lifts } = summary;

    // Front-face scaffold along the Z+ side, offset 0.6m from the building.
    const offset = 0.6;
    const frontZ = d / 2 + offset;
    const startX = -w / 2;
    const bayWidth = w / bays;
    const liftHeight = h / lifts;

    // Standards (vertical poles): one at every bay boundary, every lift level.
    for (let i = 0; i <= bays; i++) {
      const x = startX + i * bayWidth;
      this.addStandard(x, frontZ, h);
      // Inner row of standards (against the building face)
      this.addStandard(x, frontZ - 0.5, h);
    }

    // Ledgers (horizontal beams) at every lift, both inner and outer rows.
    for (let j = 0; j <= lifts; j++) {
      const y = j * liftHeight;
      this.addLedger(startX, y, frontZ, w);
      this.addLedger(startX, y, frontZ - 0.5, w);
    }

    // Platforms at every bay-lift cell.
    for (let j = 0; j < lifts; j++) {
      for (let i = 0; i < bays; i++) {
        const x = startX + i * bayWidth + bayWidth / 2;
        const y = (j + 1) * liftHeight - 0.05;
        this.addPlatform(x, y, frontZ - 0.25, bayWidth);
      }
    }

    // Diagonal braces every Nth bay.
    for (let j = 0; j < lifts; j++) {
      for (let i = 0; i < bays; i += prefs.braceEveryNBays) {
        const x1 = startX + i * bayWidth;
        const x2 = startX + (i + 1) * bayWidth;
        const y1 = j * liftHeight;
        const y2 = (j + 1) * liftHeight;
        this.addBrace(x1, y1, x2, y2, frontZ);
      }
    }

    if (prefs.wrapAround) {
      // Rather than rendering all four sides (which would make the demo
      // visually noisy), we add a subtle indicator on the back face.
      const backZ = -d / 2 - offset;
      for (let i = 0; i <= bays; i++) {
        const x = startX + i * bayWidth;
        this.addStandard(x, backZ, h);
      }
    }
  }

  clearScaffold(): void {
    this.disposeGroup(this.scaffoldGroup);
  }

  dispose(): void {
    this.resizeObserver.disconnect();
    this.disposeGroup(this.buildingGroup);
    this.disposeGroup(this.scaffoldGroup);
    this.renderer.dispose();
    if (this.renderer.domElement.parentElement === this.container) {
      this.container.removeChild(this.renderer.domElement);
    }
  }

  // === Scene setup ===

  private setupLighting(): void {
    const ambient = new THREE.AmbientLight(0xffffff, 0.4);
    this.scene.add(ambient);

    const key = new THREE.DirectionalLight(0xffffff, 1.0);
    key.position.set(15, 20, 12);
    key.castShadow = true;
    key.shadow.mapSize.set(1024, 1024);
    key.shadow.camera.left = -25;
    key.shadow.camera.right = 25;
    key.shadow.camera.top = 25;
    key.shadow.camera.bottom = -25;
    this.scene.add(key);

    const fill = new THREE.DirectionalLight(0x88aaff, 0.25);
    fill.position.set(-10, 8, -8);
    this.scene.add(fill);
  }

  private setupGround(): void {
    const groundGeom = new THREE.PlaneGeometry(80, 80);
    const groundMat = new THREE.MeshStandardMaterial({
      color: COLORS.ground,
      roughness: 1.0
    });
    const ground = new THREE.Mesh(groundGeom, groundMat);
    ground.rotation.x = -Math.PI / 2;
    ground.receiveShadow = true;
    this.scene.add(ground);

    const grid = new THREE.GridHelper(80, 80, COLORS.gridMajor, COLORS.gridMinor);
    (grid.material as THREE.Material).opacity = 0.5;
    (grid.material as THREE.Material).transparent = true;
    this.scene.add(grid);
  }

  // === Component primitives ===

  private addStandard(x: number, z: number, height: number): void {
    const geom = new THREE.CylinderGeometry(0.04, 0.04, height, 8);
    const mat = new THREE.MeshStandardMaterial({
      color: COLORS.standard,
      roughness: 0.4,
      metalness: 0.6
    });
    const mesh = new THREE.Mesh(geom, mat);
    mesh.position.set(x, height / 2, z);
    mesh.castShadow = true;
    this.scaffoldGroup.add(mesh);
  }

  private addLedger(startX: number, y: number, z: number, width: number): void {
    const geom = new THREE.CylinderGeometry(0.035, 0.035, width, 8);
    const mat = new THREE.MeshStandardMaterial({
      color: COLORS.ledger,
      roughness: 0.4,
      metalness: 0.6
    });
    const mesh = new THREE.Mesh(geom, mat);
    mesh.rotation.z = Math.PI / 2;
    mesh.position.set(startX + width / 2, y, z);
    mesh.castShadow = true;
    this.scaffoldGroup.add(mesh);
  }

  private addPlatform(x: number, y: number, z: number, width: number): void {
    const geom = new THREE.BoxGeometry(width * 0.95, 0.04, 0.5);
    const mat = new THREE.MeshStandardMaterial({
      color: COLORS.platform,
      roughness: 0.7,
      metalness: 0.3
    });
    const mesh = new THREE.Mesh(geom, mat);
    mesh.position.set(x, y, z);
    mesh.castShadow = true;
    mesh.receiveShadow = true;
    this.scaffoldGroup.add(mesh);
  }

  private addBrace(x1: number, y1: number, x2: number, y2: number, z: number): void {
    const start = new THREE.Vector3(x1, y1, z);
    const end = new THREE.Vector3(x2, y2, z);
    const length = start.distanceTo(end);
    const geom = new THREE.CylinderGeometry(0.025, 0.025, length, 6);
    const mat = new THREE.MeshStandardMaterial({
      color: COLORS.brace,
      roughness: 0.4,
      metalness: 0.6
    });
    const mesh = new THREE.Mesh(geom, mat);
    mesh.position.copy(start).lerp(end, 0.5);

    const direction = new THREE.Vector3().subVectors(end, start).normalize();
    const up = new THREE.Vector3(0, 1, 0);
    const quaternion = new THREE.Quaternion().setFromUnitVectors(up, direction);
    mesh.setRotationFromQuaternion(quaternion);

    mesh.castShadow = true;
    this.scaffoldGroup.add(mesh);
  }

  // === Camera + input ===

  private updateCameraFromOrbit(): void {
    const x = this.orbitTarget.x + this.orbitDistance * Math.sin(this.orbitTheta) * Math.cos(this.orbitPhi);
    const y = this.orbitTarget.y + this.orbitDistance * Math.cos(this.orbitTheta);
    const z = this.orbitTarget.z + this.orbitDistance * Math.sin(this.orbitTheta) * Math.sin(this.orbitPhi);
    this.camera.position.set(x, y, z);
    this.camera.lookAt(this.orbitTarget);
  }

  private attachInputHandlers(): void {
    const dom = this.renderer.domElement;
    dom.style.touchAction = 'none';

    dom.addEventListener('pointerdown', (e) => {
      this.isDragging = true;
      this.lastPointer = { x: e.clientX, y: e.clientY };
      dom.setPointerCapture(e.pointerId);
    });

    dom.addEventListener('pointermove', (e) => {
      if (!this.isDragging) return;
      const dx = e.clientX - this.lastPointer.x;
      const dy = e.clientY - this.lastPointer.y;
      this.lastPointer = { x: e.clientX, y: e.clientY };

      this.orbitPhi -= dx * 0.005;
      this.orbitTheta = Math.max(0.1, Math.min(Math.PI / 2 - 0.05, this.orbitTheta - dy * 0.005));
      this.updateCameraFromOrbit();
    });

    dom.addEventListener('pointerup', (e) => {
      this.isDragging = false;
      dom.releasePointerCapture(e.pointerId);
    });

    dom.addEventListener('wheel', (e) => {
      e.preventDefault();
      this.orbitDistance = Math.max(8, Math.min(60, this.orbitDistance + e.deltaY * 0.02));
      this.updateCameraFromOrbit();
    }, { passive: false });
  }

  private handleResize(): void {
    const { clientWidth, clientHeight } = this.container;
    this.renderer.setSize(clientWidth, clientHeight, false);
    this.camera.aspect = clientWidth / clientHeight;
    this.camera.updateProjectionMatrix();
  }

  private animate = (): void => {
    requestAnimationFrame(this.animate);
    this.renderer.render(this.scene, this.camera);
  };

  private disposeGroup(group: THREE.Group): void {
    while (group.children.length > 0) {
      const child = group.children[0];
      group.remove(child);
      if (child instanceof THREE.Mesh) {
        child.geometry.dispose();
        if (Array.isArray(child.material)) {
          child.material.forEach((m) => m.dispose());
        } else {
          child.material.dispose();
        }
      } else if (child instanceof THREE.LineSegments) {
        child.geometry.dispose();
        (child.material as THREE.Material).dispose();
      }
    }
  }
}
