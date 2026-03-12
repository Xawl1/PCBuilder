class SimplePCVisualizer {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
        if (!this.container) {
            console.error('Container not found!');
            return;
        }

        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.parts = [];
        this.buildId = null;
        this.fanGroups = [];
        this.mouse = new THREE.Vector2();
        this.raycaster = new THREE.Raycaster();
        this.hoveredObject = null;
        this.isDragging = false;
        this.previousMousePosition = { x: 0, y: 0 };
        // theta = horizontal rotation around Y axis (left/right)
        // phi = vertical angle from top (0 = top, PI/2 = side)
        this.theta = Math.PI / 6;   // start slightly to the side
        this.phi = Math.PI / 3;     // start slightly above
        this.cameraRadius = 12;

        this.init();
    }

    init() {
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x0a0a1a);
        this.scene.fog = new THREE.Fog(0x0a0a1a, 20, 40);

        const width = this.container.clientWidth || this.container.offsetWidth || 800;
        const height = this.container.clientHeight || this.container.offsetHeight || 650;

        this.camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000);
        this.updateCameraPosition();

        this.renderer = new THREE.WebGLRenderer({ antialias: true });
        this.renderer.setSize(width, height);
        this.renderer.shadowMap.enabled = true;
        this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        this.renderer.setPixelRatio(window.devicePixelRatio);
        this.container.appendChild(this.renderer.domElement);

        requestAnimationFrame(() => this.resize());

        // Lighting
        const ambientLight = new THREE.AmbientLight(0x223344, 1.5);
        this.scene.add(ambientLight);

        const keyLight = new THREE.DirectionalLight(0xffffff, 1.2);
        keyLight.position.set(5, 10, 8);
        keyLight.castShadow = true;
        keyLight.shadow.mapSize.width = 2048;
        keyLight.shadow.mapSize.height = 2048;
        this.scene.add(keyLight);

        const fillLight = new THREE.DirectionalLight(0x4488ff, 0.4);
        fillLight.position.set(-5, 3, -5);
        this.scene.add(fillLight);

        // Cyan rim light from inside
        const rimLight = new THREE.PointLight(0x00f0ff, 1.5, 8);
        rimLight.position.set(0, 1, 0);
        this.scene.add(rimLight);

        // Floor grid
        const gridHelper = new THREE.GridHelper(30, 30, 0x00f0ff22, 0x11223322);
        gridHelper.position.y = -3.5;
        this.scene.add(gridHelper);

        // Floor plane for shadows
        const floorGeo = new THREE.PlaneGeometry(30, 30);
        const floorMat = new THREE.MeshStandardMaterial({ color: 0x080812, roughness: 1 });
        const floor = new THREE.Mesh(floorGeo, floorMat);
        floor.rotation.x = -Math.PI / 2;
        floor.position.y = -3.5;
        floor.receiveShadow = true;
        this.scene.add(floor);

        // Mouse controls
        this.renderer.domElement.addEventListener('mousedown', (e) => {
            this.isDragging = true;
            this.previousMousePosition = { x: e.clientX, y: e.clientY };
        });
        this.renderer.domElement.addEventListener('mousemove', (e) => this.onMouseMove(e));
        this.renderer.domElement.addEventListener('mouseup', () => this.isDragging = false);
        this.renderer.domElement.addEventListener('wheel', (e) => {
            e.preventDefault();
            this.cameraRadius = Math.max(4, Math.min(22, this.cameraRadius + e.deltaY * 0.05));
            this.updateCameraPosition();
        }, { passive: false });

        // Tooltip
        this.tooltip = document.createElement('div');
        this.tooltip.style.cssText = `
            position: absolute; display: none; background: rgba(0,0,0,0.85);
            color: #00f0ff; padding: 8px 12px; border-radius: 8px;
            border: 1px solid #00f0ff; font-size: 13px; pointer-events: none;
            font-family: monospace; z-index: 100; max-width: 200px;
        `;
        this.container.style.position = 'relative';
        this.container.appendChild(this.tooltip);

        this.animate();
    }

    updateCameraPosition() {
        // Standard spherical: theta around Y, phi from top
        const x = this.cameraRadius * Math.sin(this.phi) * Math.sin(this.theta);
        const y = this.cameraRadius * Math.cos(this.phi);
        const z = this.cameraRadius * Math.sin(this.phi) * Math.cos(this.theta);
        this.camera.position.set(x, y, z);
        this.camera.lookAt(0, 0, 0);
    }

    onMouseMove(e) {
        const rect = this.container.getBoundingClientRect();

        if (this.isDragging) {
            const dx = e.clientX - this.previousMousePosition.x;
            const dy = e.clientY - this.previousMousePosition.y;
            // drag left → theta decreases → camera orbits left
            this.theta -= dx * 0.008;
            // drag up → phi decreases → camera goes higher
            this.phi -= dy * 0.008;
            this.phi = Math.max(0.2, Math.min(Math.PI / 2, this.phi));
            this.updateCameraPosition();
            this.previousMousePosition = { x: e.clientX, y: e.clientY };
        }

        // Raycasting for hover tooltip
        this.mouse.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
        this.mouse.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
        this.raycaster.setFromCamera(this.mouse, this.camera);

        const meshes = this.parts.filter(p => p.isMesh);
        const intersects = this.raycaster.intersectObjects(meshes);

        if (intersects.length > 0) {
            const obj = intersects[0].object;
            if (obj.userData && obj.userData.name) {
                this.tooltip.style.display = 'block';
                this.tooltip.style.left = (e.clientX - rect.left + 12) + 'px';
                this.tooltip.style.top = (e.clientY - rect.top - 10) + 'px';
                this.tooltip.innerHTML = `<strong>${obj.userData.name}</strong><br><span style="color:#00ff88">${obj.userData.category}</span>`;
            }
        } else {
            this.tooltip.style.display = 'none';
        }
    }

    async loadBuild(buildId) {
        this.buildId = buildId;
        this.parts.forEach(p => this.scene.remove(p));
        this.parts = [];

        try {
            const response = await fetch(`/Visualization/GetBuildParts?buildId=${buildId}`);
            const parts = await response.json();
            console.log('Parts loaded:', parts);
            this.buildPCScene(parts);
        } catch (error) {
            console.error('Error loading build:', error);
        }
    }

    buildPCScene(parts) {
        // Find parts by category
        const byCategory = {};
        parts.forEach(p => { byCategory[p.categoryId] = p; });

        this.addCase();
        this.addMotherboard(byCategory[3]);
        this.addCPU(byCategory[1]);
        this.addRAM(byCategory[5]);
        this.addGPU(byCategory[2]);
        this.addStorage(byCategory[4]);
        this.addPSU(byCategory[6]);
    }

    makeMesh(geo, color, emissiveStrength = 0.15, roughness = 0.4, metalness = 0.6) {
        const mat = new THREE.MeshStandardMaterial({
            color,
            emissive: new THREE.Color(color).multiplyScalar(emissiveStrength),
            roughness,
            metalness,
        });
        const mesh = new THREE.Mesh(geo, mat);
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        return mesh;
    }

    addLabel(text, position, color = '#ffffff') {
        const canvas = document.createElement('canvas');
        canvas.width = 256;
        canvas.height = 64;
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, 256, 64);
        ctx.fillStyle = color;
        ctx.font = 'bold 18px Arial';
        ctx.textAlign = 'center';
        ctx.fillText(text, 128, 38);

        const texture = new THREE.CanvasTexture(canvas);
        const mat = new THREE.SpriteMaterial({ map: texture, transparent: true });
        const sprite = new THREE.Sprite(mat);
        sprite.scale.set(1.8, 0.45, 1);
        sprite.position.copy(position);
        this.scene.add(sprite);
        this.parts.push(sprite);
    }

    addCase() {
        // Case outer shell — transparent panels
        const caseW = 4, caseH = 6, caseD = 3;

        // Back/sides/top/bottom — solid dark panels
        const panels = [
            // back
            { size: [caseW, caseH, 0.1], pos: [0, 0, -caseD / 2] },
            // top
            { size: [caseW, 0.1, caseD], pos: [0, caseH / 2, 0] },
            // bottom
            { size: [caseW, 0.1, caseD], pos: [0, -caseH / 2, 0] },
            // left
            { size: [0.1, caseH, caseD], pos: [-caseW / 2, 0, 0] },
            // right (opaque)
            { size: [0.1, caseH, caseD], pos: [caseW / 2, 0, 0] },
        ];

        panels.forEach(({ size, pos }) => {
            const geo = new THREE.BoxGeometry(...size);
            const mat = new THREE.MeshStandardMaterial({
                color: 0x1a1a2e,
                metalness: 0.8,
                roughness: 0.3,
            });
            const mesh = new THREE.Mesh(geo, mat);
            mesh.position.set(...pos);
            mesh.castShadow = true;
            mesh.receiveShadow = true;
            this.scene.add(mesh);
            this.parts.push(mesh);
        });

        // Front glass panel — transparent cyan tint
        const glassMat = new THREE.MeshStandardMaterial({
            color: 0x001a2e,
            transparent: true,
            opacity: 0.25,
            metalness: 0.1,
            roughness: 0.05,
            side: THREE.DoubleSide,
        });
        const glassGeo = new THREE.BoxGeometry(caseW, caseH, 0.05);
        const glass = new THREE.Mesh(glassGeo, glassMat);
        glass.position.set(0, 0, caseD / 2);
        this.scene.add(glass);
        this.parts.push(glass);

        // Glowing cyan edge strips
        const edgePositions = [
            { size: [0.05, caseH, 0.05], pos: [-caseW / 2, 0, caseD / 2] },
            { size: [0.05, caseH, 0.05], pos: [caseW / 2, 0, caseD / 2] },
            { size: [caseW, 0.05, 0.05], pos: [0, caseH / 2, caseD / 2] },
            { size: [caseW, 0.05, 0.05], pos: [0, -caseH / 2, caseD / 2] },
        ];

        edgePositions.forEach(({ size, pos }) => {
            const geo = new THREE.BoxGeometry(...size);
            const mat = new THREE.MeshStandardMaterial({
                color: 0x00f0ff,
                emissive: new THREE.Color(0x00f0ff),
                emissiveIntensity: 2,
            });
            const mesh = new THREE.Mesh(geo, mat);
            mesh.position.set(...pos);
            this.scene.add(mesh);
            this.parts.push(mesh);
        });

        // Top fans — 2x120mm exhausting upward
        this.addFan(-0.8, caseH / 2, 0, 'top', 0x00f0ff);
        this.addFan(0.8, caseH / 2, 0, 'top', 0x00f0ff);

        // Side fans — 3x120mm intake on right side panel
        this.addFan(caseW / 2, 1.5, 0, 'side', 0x00aaff);
        this.addFan(caseW / 2, 0.0, 0, 'side', 0x00aaff);
        this.addFan(caseW / 2, -1.5, 0, 'side', 0x00aaff);
    }

    addFan(x, y, z, axis, glowColor) {
        const group = new THREE.Group();
        group.position.set(x, y, z);

        // Fan frame
        const frameGeo = new THREE.BoxGeometry(0.95, 0.95, 0.12);
        const frameMat = new THREE.MeshStandardMaterial({ color: 0x111122, roughness: 0.5, metalness: 0.7 });
        const frame = new THREE.Mesh(frameGeo, frameMat);
        frame.castShadow = true;
        group.add(frame);

        // Fan hub — cylinder oriented along Z so it faces outward
        const hubGeo = new THREE.CylinderGeometry(0.1, 0.1, 0.14, 12);
        const hubMat = new THREE.MeshStandardMaterial({ color: 0x222233, roughness: 0.4, metalness: 0.8 });
        const hub = new THREE.Mesh(hubGeo, hubMat);
        hub.rotation.x = Math.PI / 2; // rotate hub 90deg so it aligns with spin axis
        group.add(hub);

        // Spinner group — blades spin around Z axis (facing outward through the panel)
        const spinner = new THREE.Group();
        const bladeMat = new THREE.MeshStandardMaterial({ color: 0x1a1a33, roughness: 0.5, metalness: 0.5 });
        for (let i = 0; i < 5; i++) {
            const bladeGeo = new THREE.BoxGeometry(0.28, 0.32, 0.06); // flat in XY plane
            const blade = new THREE.Mesh(bladeGeo, bladeMat);
            const angle = (i / 5) * Math.PI * 2;
            blade.position.set(Math.cos(angle) * 0.22, Math.sin(angle) * 0.22, 0);
            blade.rotation.z = angle + 0.4;
            spinner.add(blade);
        }
        group.add(spinner); // spinner is children[2]

        // RGB ring
        const ringGeo = new THREE.TorusGeometry(0.38, 0.03, 8, 32);
        const ringMat = new THREE.MeshStandardMaterial({
            color: glowColor,
            emissive: new THREE.Color(glowColor),
            emissiveIntensity: 2.5,
        });
        const ring = new THREE.Mesh(ringGeo, ringMat);
        group.add(ring);

        // Orient group to face the right panel
        if (axis === 'top') {
            group.rotation.x = Math.PI / 2;
        } else if (axis === 'side') {
            group.rotation.y = Math.PI / 2;
        }

        this.scene.add(group);
        this.parts.push(group);
        this.fanGroups.push(spinner);
    }

    addMotherboard(part) {
        const geo = new THREE.BoxGeometry(2.8, 3.5, 0.1);
        const mesh = this.makeMesh(geo, 0x1a4a2a, 0.2, 0.8, 0.1);
        mesh.position.set(-0.3, 0.2, -1.3);
        mesh.userData = { name: part ? `${part.brand} ${part.modelName}` : 'Motherboard', category: 'Motherboard' };

        // PCIe slots
        for (let i = 0; i < 3; i++) {
            const slotGeo = new THREE.BoxGeometry(1.2, 0.08, 0.05);
            const slot = this.makeMesh(slotGeo, 0x333333, 0, 0.9, 0.5);
            slot.position.set(-0.3, 0.5 - i * 0.4, -1.24);
            this.scene.add(slot);
            this.parts.push(slot);
        }

        this.scene.add(mesh);
        this.parts.push(mesh);

        if (part) this.addLabel(`${part.brand}`, new THREE.Vector3(-0.3, 2.3, -1.3), '#00ff88');
    }

    addCPU(part) {
        // CPU socket on mobo — positioned left of RAM
        const geo = new THREE.BoxGeometry(0.55, 0.55, 0.12);
        const mesh = this.makeMesh(geo, 0x888888, 0.3, 0.2, 0.9);
        mesh.position.set(-0.9, 1.3, -1.22);
        mesh.userData = { name: part ? `${part.brand} ${part.modelName}` : 'CPU', category: 'Processor' };

        // CPU cooler
        const coolerGeo = new THREE.BoxGeometry(0.65, 0.8, 0.65);
        const cooler = this.makeMesh(coolerGeo, 0x555566, 0.1, 0.3, 0.7);
        cooler.position.set(-0.9, 1.3, -0.85);

        // Fan on cooler
        const fanGeo = new THREE.CylinderGeometry(0.28, 0.28, 0.08, 16);
        const fan = this.makeMesh(fanGeo, 0x222233, 0.05, 0.5, 0.3);
        fan.rotation.x = Math.PI / 2;
        fan.position.set(-0.9, 1.3, -0.48);

        this.scene.add(mesh);
        this.scene.add(cooler);
        this.scene.add(fan);
        this.parts.push(mesh, cooler, fan);

        if (part) this.addLabel(`${part.brand}`, new THREE.Vector3(-0.9, 2.3, -0.6), '#00f0ff');
    }

    addRAM(part) {
        // 2 RAM sticks
        for (let i = 0; i < 2; i++) {
            const geo = new THREE.BoxGeometry(0.12, 1.1, 0.05);
            const mesh = this.makeMesh(geo, 0xaa00ff, 0.4, 0.3, 0.6);
            mesh.position.set(-0.5 + i * 0.22, 1.1, -1.22);
            mesh.userData = { name: part ? `${part.brand} ${part.modelName}` : 'RAM', category: 'RAM' };

            // RGB strip glow on top
            const rgbGeo = new THREE.BoxGeometry(0.12, 0.12, 0.06);
            const rgbMat = new THREE.MeshStandardMaterial({
                color: 0xff00ff,
                emissive: new THREE.Color(0xff00ff),
                emissiveIntensity: 1.5,
            });
            const rgb = new THREE.Mesh(rgbGeo, rgbMat);
            rgb.position.set(-0.5 + i * 0.22, 1.72, -1.22);

            this.scene.add(mesh);
            this.scene.add(rgb);
            this.parts.push(mesh, rgb);
        }

        if (part) this.addLabel(`${part.brand}`, new THREE.Vector3(-0.39, 2.3, -1.22), '#aa00ff');
    }

    addGPU(part) {
        // GPU card — large horizontal slab slotted into mobo
        const geo = new THREE.BoxGeometry(2.2, 0.55, 0.85);
        const mesh = this.makeMesh(geo, 0x222233, 0.1, 0.4, 0.7);
        mesh.position.set(-0.3, -0.3, -0.8);
        mesh.userData = { name: part ? `${part.brand} ${part.modelName}` : 'GPU', category: 'Graphics Card' };

        // GPU fans
        for (let i = 0; i < 2; i++) {
            const fanGeo = new THREE.CylinderGeometry(0.22, 0.22, 0.1, 16);
            const fan = this.makeMesh(fanGeo, 0x111122, 0.05, 0.6, 0.2);
            fan.rotation.x = Math.PI / 2;
            fan.position.set(-0.55 + i * 0.7, -0.3, -0.36);

            const bladeGeo = new THREE.TorusGeometry(0.18, 0.03, 4, 8);
            const blade = this.makeMesh(bladeGeo, 0x333355, 0.1, 0.5, 0.5);
            blade.rotation.x = Math.PI / 2;
            blade.position.set(-0.55 + i * 0.7, -0.3, -0.36);

            this.scene.add(fan);
            this.scene.add(blade);
            this.parts.push(fan, blade);
        }

        // GPU RGB strip
        const rgbGeo = new THREE.BoxGeometry(2.2, 0.06, 0.06);
        const rgbMat = new THREE.MeshStandardMaterial({
            color: 0xff3366,
            emissive: new THREE.Color(0xff3366),
            emissiveIntensity: 2,
        });
        const rgb = new THREE.Mesh(rgbGeo, rgbMat);
        rgb.position.set(-0.3, -0.02, -0.36);

        this.scene.add(mesh);
        this.scene.add(rgb);
        this.parts.push(mesh, rgb);

        if (part) this.addLabel(`${part.brand}`, new THREE.Vector3(-0.3, 0.4, -0.8), '#ff3366');
    }

    addStorage(part) {
        // NVMe SSD on mobo
        const geo = new THREE.BoxGeometry(0.8, 0.12, 0.25);
        const mesh = this.makeMesh(geo, 0x1a1a3a, 0.1, 0.5, 0.5);
        mesh.position.set(0.0, 0.55, -1.22);
        mesh.userData = { name: part ? `${part.brand} ${part.modelName}` : 'Storage', category: 'Storage' };

        // chips on SSD
        const chipGeo = new THREE.BoxGeometry(0.12, 0.07, 0.12);
        for (let i = 0; i < 3; i++) {
            const chip = this.makeMesh(chipGeo, 0x333333, 0, 0.9, 0.3);
            chip.position.set(-0.22 + i * 0.22, 0.62, -1.22);
            this.scene.add(chip);
            this.parts.push(chip);
        }

        this.scene.add(mesh);
        this.parts.push(mesh);

        if (part) this.addLabel(`${part.brand}`, new THREE.Vector3(0.0, 0.9, -1.1), '#ffaa00');
    }

    addPSU(part) {
        // PSU at bottom of case
        const geo = new THREE.BoxGeometry(1.8, 0.9, 1.4);
        const mesh = this.makeMesh(geo, 0x1a1a1a, 0.05, 0.3, 0.8);
        mesh.position.set(0.6, -2.5, -0.7);
        mesh.userData = { name: part ? `${part.brand} ${part.modelName}` : 'PSU', category: 'Power Supply' };

        // PSU fan grille
        const fanGeo = new THREE.CylinderGeometry(0.35, 0.35, 0.05, 16);
        const fan = this.makeMesh(fanGeo, 0x111111, 0, 0.9, 0.1);
        fan.rotation.x = Math.PI / 2;
        fan.position.set(0.6, -2.5, -0.01);

        this.scene.add(mesh);
        this.scene.add(fan);
        this.parts.push(mesh, fan);

        if (part) this.addLabel(`${part.brand}`, new THREE.Vector3(0.6, -1.9, -0.7), '#888888');
    }

    animate() {
        requestAnimationFrame(() => this.animate());

        // Spin fan blade spinners around Z axis
        if (this.fanGroups) {
            this.fanGroups.forEach(spinner => {
                spinner.rotation.z += 0.08;
            });
        }

        this.renderer.render(this.scene, this.camera);
    }

    resize() {
        const width = this.container.clientWidth;
        const height = this.container.clientHeight;
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
    }
}

window.SimplePCVisualizer = SimplePCVisualizer;