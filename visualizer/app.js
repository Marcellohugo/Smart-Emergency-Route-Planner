/**
 * Smart Emergency Route Planner - Web Visualizer Application Logic (Indonesian Translation & Simplified GIS Theme)
 * Implements Dijkstra, A*, Bidirectional Dijkstra, Robust Risk-Aware,
 * Yen's K-Shortest Paths, Alternative Penalty Routes, and Multi-Hospital Search.
 */

// ============================================================================
// 1. DATA STRUCTURES & UTILITIES
// ============================================================================

/**
 * Seeded Pseudo-Random Number Generator (Mulberry32)
 * Memastikan layout peta konsisten saat di-regenerasi dengan seed yang sama.
 */
class SeededRandom {
    constructor(seed) {
        this.s = seed;
    }
    next() {
        let t = this.s += 0x6D2B79F5;
        t = Math.imul(t ^ (t >>> 15), t | 1);
        t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    }
    nextRange(min, max) {
        return min + this.next() * (max - min);
    }
    nextInt(min, max) {
        return Math.floor(min + this.next() * (max - min));
    }
}

/**
 * Implementasi Binary Min-Heap
 * Digunakan sebagai Priority Queue untuk Dijkstra, A*, dan pencarian rute lainnya.
 */
class BinaryMinHeap {
    constructor() {
        this.heap = [];
    }

    get IsEmpty() {
        return this.heap.length === 0;
    }

    Insert(vertexId, priority) {
        this.heap.push({ vertexId, priority });
        this.swim(this.heap.length - 1);
    }

    ExtractMin() {
        if (this.IsEmpty) return null;
        const min = this.heap[0];
        const last = this.heap.pop();
        if (this.heap.length > 0) {
            this.heap[0] = last;
            this.sink(0);
        }
        return min;
    }

    Peek() {
        return this.heap.length > 0 ? this.heap[0] : null;
    }

    swim(i) {
        while (i > 0) {
            const p = Math.floor((i - 1) / 2);
            if (this.heap[i].priority >= this.heap[p].priority) break;
            this.swap(i, p);
            i = p;
        }
    }

    sink(i) {
        const len = this.heap.length;
        while (2 * i + 1 < len) {
            let left = 2 * i + 1;
            let right = 2 * i + 2;
            let smallest = left;
            if (right < len && this.heap[right].priority < this.heap[left].priority) {
                smallest = right;
            }
            if (this.heap[i].priority <= this.heap[smallest].priority) break;
            this.swap(i, smallest);
            i = smallest;
        }
    }

    swap(i, j) {
        const temp = this.heap[i];
        this.heap[i] = this.heap[j];
        this.heap[j] = temp;
    }
}

class Vertex {
    constructor(id, x, y, name) {
        this.id = id;
        this.x = x; // 0 sampai 100
        this.y = y; // 0 sampai 100
        this.name = name;
    }
}

class Edge {
    constructor(from, to, distanceKm, speedKmh) {
        this.from = from;
        this.to = to;
        this.distanceKm = distanceKm;
        this.speedKmh = speedKmh;
        this.travelTimeMinutes = (distanceKm / speedKmh) * 60.0;
        
        this.isClosed = false;
        this.traffic = 'Normal';
        this.trafficMultiplier = 1.0;
        this.timePeriodMultiplier = 1.0;
        
        // Atribut lanjutan
        this.hasEmergencyLane = false;
        this.emergencyMultiplier = 0.6;
        this.closureRisk = 0.0;
        this.trafficRisk = 0.0;
    }

    getWeight(emergencyMode = false, robustMode = false, lambda = 15.0) {
        let weight = this.travelTimeMinutes * this.trafficMultiplier * this.timePeriodMultiplier;
        if (emergencyMode && this.hasEmergencyLane) {
            weight *= this.emergencyMultiplier;
        }
        if (robustMode) {
            weight += lambda * (this.closureRisk + this.trafficRisk);
        }
        return weight;
    }
}

class Graph {
    constructor(vertexCount) {
        this.vertexCount = vertexCount;
        this.vertices = [];
        this.adjacencyList = Array.from({ length: vertexCount }, () => []);
        this.reverseAdjacencyList = Array.from({ length: vertexCount }, () => []);
        this.allEdges = [];
    }

    addVertex(vertex) {
        this.vertices.push(vertex);
    }

    addEdge(from, to, distanceKm, speedKmh) {
        const edge = new Edge(from, to, distanceKm, speedKmh);
        this.adjacencyList[from].push(edge);
        this.reverseAdjacencyList[to].push(edge);
        this.allEdges.push(edge);
        return edge;
    }

    closeEdge(from, to) {
        this.adjacencyList[from].forEach(e => {
            if (e.to === to) e.isClosed = true;
        });
    }

    openEdge(from, to) {
        this.adjacencyList[from].forEach(e => {
            if (e.to === to) e.isClosed = false;
        });
    }

    resetClosures() {
        this.allEdges.forEach(e => e.isClosed = false);
    }

    closeRandomEdges(closureRate, seed) {
        this.resetClosures();
        const rand = new SeededRandom(seed);
        const targetCount = Math.floor(this.allEdges.length * closureRate);
        const indices = Array.from({ length: this.allEdges.length }, (_, i) => i);
        
        // Acak urutan jalan
        for (let i = indices.length - 1; i > 0; i--) {
            const j = Math.floor(rand.next() * (i + 1));
            const temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }

        for (let i = 0; i < targetCount && i < indices.length; i++) {
            this.allEdges[indices[i]].isClosed = true;
        }
    }

    applyRandomTraffic(seed) {
        const rand = new SeededRandom(seed);
        this.allEdges.forEach(edge => {
            const r = rand.next();
            if (r < 0.15) {
                edge.traffic = 'Low';
                edge.trafficMultiplier = 0.8;
            } else if (r < 0.65) {
                edge.traffic = 'Normal';
                edge.trafficMultiplier = 1.0;
            } else if (r < 0.90) {
                edge.traffic = 'High';
                edge.trafficMultiplier = 1.5;
            } else {
                edge.traffic = 'Severe';
                edge.trafficMultiplier = 2.5;
            }
        });
    }

    applySevereTraffic() {
        this.allEdges.forEach(edge => {
            edge.traffic = 'Severe';
            edge.trafficMultiplier = 2.5;
        });
    }

    resetTraffic() {
        this.allEdges.forEach(edge => {
            edge.traffic = 'Normal';
            edge.trafficMultiplier = 1.0;
        });
    }
}

// ============================================================================
// 2. NETWORK TOPOLOGY GENERATOR
// ============================================================================

class CityGraphGenerator {
    static generate(vertexCount, edgeCount, seed, family) {
        if (vertexCount < 2) {
            throw new Error("Jumlah persimpangan minimal harus 2.");
        }
        const graph = new Graph(vertexCount);
        const random = new SeededRandom(seed);

        // 1. Membuat Koordinat
        if (family === "GridCity") {
            let rows = Math.floor(Math.sqrt(vertexCount));
            if (rows < 2) rows = 2;
            let cols = Math.ceil(vertexCount / rows);

            for (let i = 0; i < vertexCount; i++) {
                let r = Math.floor(i / cols);
                let c = i % cols;

                let baseX = cols > 1 ? c * (100.0 / (cols - 1)) : 50.0;
                let baseY = rows > 1 ? r * (100.0 / (rows - 1)) : 50.0;

                let jitterRangeX = cols > 1 ? (100.0 / (cols - 1)) * 0.18 : 0.0;
                let jitterRangeY = rows > 1 ? (100.0 / (rows - 1)) * 0.18 : 0.0;

                let x = baseX + (random.next() - 0.5) * jitterRangeX;
                let y = baseY + (random.next() - 0.5) * jitterRangeY;

                x = Math.max(0.0, Math.min(100.0, x));
                y = Math.max(0.0, Math.min(100.0, y));

                let name = i === 0 ? "Pos Awal Ambulans (Start)" :
                           i === vertexCount - 1 ? "Rumah Sakit Pusat Kota (Target)" :
                           `Persimpangan Kota (${r},${c})`;

                graph.addVertex(new Vertex(i, x, y, name));
            }
        } else {
            // Peta Acak
            for (let i = 0; i < vertexCount; i++) {
                let x = 5 + random.next() * 90.0;
                let y = 5 + random.next() * 90.0;
                let name = i === 0 ? "Pos Awal Ambulans (Start)" :
                           i === vertexCount - 1 ? "Rumah Sakit Pusat Kota (Target)" :
                           `Persimpangan ${i}`;
                graph.addVertex(new Vertex(i, x, y, name));
            }
        }

        const existingEdges = new Set();
        function getEdgeKey(u, v) { return `${u}->${v}`; }
        let currentEdgeCount = 0;

        function populateAdvancedProperties(edge) {
            edge.hasEmergencyLane = random.next() < 0.25; // 25% memiliki jalur darurat khusus
            edge.closureRisk = random.next() * 0.1;       // Risiko penutupan jalan: [0, 0.1]
            edge.trafficRisk = random.next() * 0.3;       // Kerawanan macet: [0, 0.3]
        }

        // 2. Jalur Penghubung Utama (Menjamin agar graf selalu terhubung)
        for (let i = 0; i < vertexCount - 1; i++) {
            let from = i;
            let to = i + 1;
            let dist = calculateEuclideanDistance(graph.vertices[from], graph.vertices[to]);
            let speed = 20.0 + (random.next() * 80.0);

            const edge = graph.addEdge(from, to, dist, speed);
            populateAdvancedProperties(edge);
            existingEdges.add(getEdgeKey(from, to));
            currentEdgeCount++;
        }

        let maxPossibleEdges = vertexCount * (vertexCount - 1);
        let targetEdges = Math.min(edgeCount, maxPossibleEdges);

        // 3. Jalur Grid Kota Teratur
        if (family === "GridCity") {
            let rows = Math.floor(Math.sqrt(vertexCount));
            if (rows < 2) rows = 2;
            let cols = Math.ceil(vertexCount / rows);

            for (let i = 0; i < vertexCount; i++) {
                if (currentEdgeCount >= targetEdges) break;
                let r = Math.floor(i / cols);
                let c = i % cols;

                let neighbors = [
                    (c + 1 < cols && i + 1 < vertexCount) ? i + 1 : -1,
                    (r + 1 < rows && i + cols < vertexCount) ? i + cols : -1,
                    (c - 1 >= 0 && i - 1 >= 0) ? i - 1 : -1,
                    (r - 1 >= 0 && i - cols >= 0) ? i - cols : -1
                ];

                for (let nb of neighbors) {
                    if (nb !== -1 && nb !== i && !existingEdges.has(getEdgeKey(i, nb))) {
                        if (currentEdgeCount >= targetEdges) break;
                        let dist = calculateEuclideanDistance(graph.vertices[i], graph.vertices[nb]);
                        let speed = 20.0 + (random.next() * 80.0);
                        const edge = graph.addEdge(i, nb, dist, speed);
                        populateAdvancedProperties(edge);
                        existingEdges.add(getEdgeKey(i, nb));
                        currentEdgeCount++;
                    }
                }
            }

            // Jalur Diagonal Kota (Arah Aveneu serong)
            for (let i = 0; i < vertexCount; i++) {
                if (currentEdgeCount >= targetEdges) break;
                let r = Math.floor(i / cols);
                let c = i % cols;

                let diagonals = [
                    (r + 1 < rows && c + 1 < cols && i + cols + 1 < vertexCount) ? i + cols + 1 : -1,
                    (r + 1 < rows && c - 1 >= 0 && i + cols - 1 < vertexCount) ? i + cols - 1 : -1,
                    (r - 1 >= 0 && c + 1 < cols && i - cols + 1 >= 0) ? i - cols + 1 : -1,
                    (r - 1 >= 0 && c - 1 >= 0 && i - cols - 1 >= 0) ? i - cols - 1 : -1
                ];

                for (let dg of diagonals) {
                    if (dg !== -1 && dg !== i && !existingEdges.has(getEdgeKey(i, dg))) {
                        if (currentEdgeCount >= targetEdges) break;
                        let dist = calculateEuclideanDistance(graph.vertices[i], graph.vertices[dg]);
                        let speed = 20.0 + (random.next() * 80.0);
                        const edge = graph.addEdge(i, dg, dist, speed);
                        populateAdvancedProperties(edge);
                        existingEdges.add(getEdgeKey(i, dg));
                        currentEdgeCount++;
                    }
                }
            }
        }

        // 4. Hubungkan sisanya secara acak
        while (currentEdgeCount < targetEdges) {
            let from = Math.floor(random.next() * vertexCount);
            let to = Math.floor(random.next() * vertexCount);

            if (from === to || existingEdges.has(getEdgeKey(from, to))) {
                continue;
            }

            let dist = calculateEuclideanDistance(graph.vertices[from], graph.vertices[to]);
            let speed = 20.0 + (random.next() * 80.0);

            const edge = graph.addEdge(from, to, dist, speed);
            populateAdvancedProperties(edge);
            existingEdges.add(getEdgeKey(from, to));
            currentEdgeCount++;
        }

        return graph;
    }
}

function calculateEuclideanDistance(a, b) {
    let dx = a.x - b.x;
    let dy = a.y - b.y;
    return Math.sqrt(dx * dx + dy * dy);
}

// ============================================================================
// 3. STEP-BY-STEP GENERATOR ROUTING ALGORITHMS
// ============================================================================

/**
 * Generator Dijkstra Standar
 */
function* DijkstraGenerator(graph, source, target, emergencyMode = false, edgePenalties = null) {
    const n = graph.vertexCount;
    const dist = Array(n).fill(Infinity);
    const prev = Array(n).fill(-1);
    const visited = Array(n).fill(false);

    dist[source] = 0;
    const heap = new BinaryMinHeap();
    heap.Insert(source, 0);

    let expandedNodes = 0;
    let relaxationCount = 0;
    let reached = false;

    yield { type: 'init', dist: [...dist], prev: [...prev] };

    while (!heap.IsEmpty) {
        const minNode = heap.ExtractMin();
        const u = minNode.vertexId;
        const currentDist = minNode.priority;

        if (currentDist > dist[u]) {
            continue;
        }

        expandedNodes++;
        yield { type: 'visit', u, dist: currentDist, heap: heap.heap.map(x => x.vertexId) };

        if (u === target) {
            reached = true;
            break;
        }

        visited[u] = true;

        for (let edge of graph.adjacencyList[u]) {
            if (edge.isClosed) continue;

            relaxationCount++;
            const v = edge.to;
            if (visited[v]) continue;

            let weight = edge.getWeight(emergencyMode);
            if (edgePenalties && edgePenalties.has(`${edge.from}->${edge.to}`)) {
                weight *= edgePenalties.get(`${edge.from}->${edge.to}`);
            }

            const newDist = dist[u] + weight;
            if (newDist < dist[v]) {
                dist[v] = newDist;
                prev[v] = u;
                heap.Insert(v, newDist);
                yield { type: 'relax', u, v, weight, dist: newDist };
            }
        }
    }

    const isReachable = reached || (dist[target] < Infinity);
    if (isReachable) {
        const path = [];
        let curr = target;
        while (curr !== -1) {
            path.push(curr);
            curr = prev[curr];
        }
        path.reverse();

        // Hitung waktu tempuh murni tanpa penalti
        let totalTime = 0;
        let totalDist = 0;
        for (let i = 0; i < path.length - 1; i++) {
            const uFrom = path[i];
            const uTo = path[i + 1];
            const edge = graph.adjacencyList[uFrom].find(e => e.to === uTo);
            if (edge) {
                totalTime += edge.getWeight(emergencyMode);
                totalDist += edge.distanceKm;
            }
        }

        yield {
            type: 'complete',
            path,
            totalTravelTimeMinutes: totalTime,
            totalDistanceKm: totalDist,
            expandedNodes,
            relaxationCount,
            notes: "Rute terbaik paling cepat telah ditemukan."
        };
    } else {
        yield {
            type: 'unreachable',
            totalTravelTimeMinutes: -1,
            totalDistanceKm: 0,
            path: [],
            expandedNodes,
            relaxationCount,
            notes: "Tujuan tidak dapat dicapai karena terisolasi."
        };
    }
}

/**
 * Generator A* Search dengan heuristik garis lurus (pintar)
 */
function* AStarGenerator(graph, source, target, maxSpeedKmh = 100.0, emergencyMode = false) {
    const n = graph.vertexCount;
    const gScore = Array(n).fill(Infinity);
    const fScore = Array(n).fill(Infinity);
    const prev = Array(n).fill(-1);
    const visited = Array(n).fill(false);

    const targetVertex = graph.vertices[target];
    const heuristicFactor = emergencyMode ? (0.8 * 0.6) : 0.8;

    function Heuristic(u) {
        const uVertex = graph.vertices[u];
        const distKm = calculateEuclideanDistance(uVertex, targetVertex);
        return (distKm / maxSpeedKmh) * 60.0 * heuristicFactor;
    }

    gScore[source] = 0;
    fScore[source] = Heuristic(source);

    const heap = new BinaryMinHeap();
    heap.Insert(source, fScore[source]);

    let expandedNodes = 0;
    let relaxationCount = 0;
    let reached = false;

    yield { type: 'init', dist: [...gScore], prev: [...prev] };

    while (!heap.IsEmpty) {
        const minNode = heap.ExtractMin();
        const u = minNode.vertexId;
        const priority = minNode.priority;

        if (priority > fScore[u]) {
            continue;
        }

        expandedNodes++;
        yield { type: 'visit', u, dist: gScore[u], heap: heap.heap.map(x => x.vertexId) };

        if (u === target) {
            reached = true;
            break;
        }

        visited[u] = true;

        for (let edge of graph.adjacencyList[u]) {
            if (edge.isClosed) continue;

            relaxationCount++;
            const v = edge.to;
            if (visited[v]) continue;

            const weight = edge.getWeight(emergencyMode);
            const tentativeG = gScore[u] + weight;
            if (tentativeG < gScore[v]) {
                gScore[v] = tentativeG;
                const f = tentativeG + Heuristic(v);
                fScore[v] = f;
                prev[v] = u;
                heap.Insert(v, f);
                yield { type: 'relax', u, v, weight, dist: tentativeG };
            }
        }
    }

    const isReachable = reached || (gScore[target] < Infinity);
    if (isReachable) {
        const path = [];
        let curr = target;
        while (curr !== -1) {
            path.push(curr);
            curr = prev[curr];
        }
        path.reverse();

        let totalDist = 0;
        for (let i = 0; i < path.length - 1; i++) {
            const edge = graph.adjacencyList[path[i]].find(e => e.to === path[i+1]);
            if (edge) totalDist += edge.distanceKm;
        }

        yield {
            type: 'complete',
            path,
            totalTravelTimeMinutes: gScore[target],
            totalDistanceKm: totalDist,
            expandedNodes,
            relaxationCount,
            notes: "Rute ditemukan memanfaatkan metode perkiraan arah (heuristik)."
        };
    } else {
        yield {
            type: 'unreachable',
            totalTravelTimeMinutes: -1,
            totalDistanceKm: 0,
            path: [],
            expandedNodes,
            relaxationCount,
            notes: "Tujuan tidak dapat dicapai karena jalan tertutup."
        };
    }
}

/**
 * Generator Pencarian Dua Arah (Bidirectional Dijkstra)
 */
function* BidirectionalDijkstraGenerator(graph, source, target, emergencyMode = false) {
    const n = graph.vertexCount;
    const distF = Array(n).fill(Infinity);
    const distB = Array(n).fill(Infinity);
    const prevF = Array(n).fill(-1);
    const prevB = Array(n).fill(-1);
    const visitedF = Array(n).fill(false);
    const visitedB = Array(n).fill(false);

    distF[source] = 0;
    distB[target] = 0;

    const heapF = new BinaryMinHeap();
    const heapB = new BinaryMinHeap();

    heapF.Insert(source, 0);
    heapB.Insert(target, 0);

    let bestDistance = Infinity;
    let meetingVertex = -1;
    let expandedNodes = 0;
    let relaxationCount = 0;

    yield { type: 'init', distF: [...distF], distB: [...distB] };

    while (!heapF.IsEmpty && !heapB.IsEmpty) {
        const minF = heapF.Peek().priority;
        const minB = heapB.Peek().priority;

        if (minF + minB >= bestDistance) {
            break;
        }

        // Langkah maju (dari Start)
        if (!heapF.IsEmpty) {
            const node = heapF.ExtractMin();
            const u = node.vertexId;
            const currentD = node.priority;

            if (currentD <= distF[u]) {
                visitedF[u] = true;
                expandedNodes++;
                yield { type: 'visit_forward', u, dist: currentD };

                if (visitedB[u]) {
                    const totalD = distF[u] + distB[u];
                    if (totalD < bestDistance) {
                        bestDistance = totalD;
                        meetingVertex = u;
                        yield { type: 'meet', meetingVertex, bestDistance };
                    }
                }

                for (let edge of graph.adjacencyList[u]) {
                    if (edge.isClosed) continue;

                    relaxationCount++;
                    const v = edge.to;
                    const w = edge.getWeight(emergencyMode);

                    if (distF[u] + w < distF[v]) {
                        distF[v] = distF[u] + w;
                        prevF[v] = u;
                        heapF.Insert(v, distF[v]);
                        yield { type: 'relax_forward', u, v, weight: w, dist: distF[v] };

                        if (visitedB[v]) {
                            const totalD = distF[v] + distB[v];
                            if (totalD < bestDistance) {
                                bestDistance = totalD;
                                meetingVertex = v;
                                yield { type: 'meet', meetingVertex, bestDistance };
                            }
                        }
                    }
                }
            }
        }

        // Langkah mundur (dari Target)
        if (!heapB.IsEmpty) {
            const node = heapB.ExtractMin();
            const u = node.vertexId;
            const currentD = node.priority;

            if (currentD <= distB[u]) {
                visitedB[u] = true;
                expandedNodes++;
                yield { type: 'visit_backward', u, dist: currentD };

                if (visitedF[u]) {
                    const totalD = distF[u] + distB[u];
                    if (totalD < bestDistance) {
                        bestDistance = totalD;
                        meetingVertex = u;
                        yield { type: 'meet', meetingVertex, bestDistance };
                    }
                }

                // Periksa jalur berlawanan
                for (let edge of graph.reverseAdjacencyList[u]) {
                    if (edge.isClosed) continue;

                    relaxationCount++;
                    const v = edge.from;
                    const w = edge.getWeight(emergencyMode);

                    if (distB[u] + w < distB[v]) {
                        distB[v] = distB[u] + w;
                        prevB[v] = u;
                        heapB.Insert(v, distB[v]);
                        yield { type: 'relax_backward', u, v, weight: w, dist: distB[v] };

                        if (visitedF[v]) {
                            const totalD = distF[v] + distB[v];
                            if (totalD < bestDistance) {
                                bestDistance = totalD;
                                meetingVertex = v;
                                yield { type: 'meet', meetingVertex, bestDistance };
                            }
                        }
                    }
                }
            }
        }
    }

    const isReachable = meetingVertex !== -1 && bestDistance < Infinity;
    if (isReachable) {
        const pathF = [];
        let curr = meetingVertex;
        while (curr !== -1) {
            pathF.push(curr);
            curr = prevF[curr];
        }
        pathF.reverse();

        const pathB = [];
        curr = meetingVertex;
        while (curr !== -1) {
            pathB.push(curr);
            curr = prevB[curr];
        }

        const finalPath = [...pathF];
        for (let i = 1; i < pathB.length; i++) {
            finalPath.push(pathB[i]);
        }

        // Kalkulasi jarak dan waktu tempuh rute final
        let totalTime = 0;
        let totalDist = 0;
        for (let i = 0; i < finalPath.length - 1; i++) {
            const edge = graph.adjacencyList[finalPath[i]].find(e => e.to === finalPath[i+1]);
            if (edge) {
                totalTime += edge.getWeight(emergencyMode);
                totalDist += edge.distanceKm;
            }
        }

        yield {
            type: 'complete',
            path: finalPath,
            totalTravelTimeMinutes: totalTime,
            totalDistanceKm: totalDist,
            expandedNodes,
            relaxationCount,
            notes: `Rute bertemu di titik persimpangan ${meetingVertex}`
        };
    } else {
        yield {
            type: 'unreachable',
            totalTravelTimeMinutes: -1,
            totalDistanceKm: 0,
            path: [],
            expandedNodes,
            relaxationCount,
            notes: "Tujuan tidak dapat dijangkau."
        };
    }
}

/**
 * Generator Rute Paling Aman (Robust Risk-Aware)
 */
function* RobustRouteGenerator(graph, source, target, emergencyMode = false, lambda = 15.0) {
    const n = graph.vertexCount;
    const dist = Array(n).fill(Infinity);
    const prev = Array(n).fill(-1);
    const visited = Array(n).fill(false);

    dist[source] = 0;
    const heap = new BinaryMinHeap();
    heap.Insert(source, 0);

    let expandedNodes = 0;
    let relaxationCount = 0;
    let reached = false;

    yield { type: 'init', dist: [...dist], prev: [...prev] };

    while (!heap.IsEmpty) {
        const minNode = heap.ExtractMin();
        const u = minNode.vertexId;
        const currentDist = minNode.priority;

        if (currentDist > dist[u]) {
            continue;
        }

        expandedNodes++;
        yield { type: 'visit', u, dist: currentDist, heap: heap.heap.map(x => x.vertexId) };

        if (u === target) {
            reached = true;
            break;
        }

        visited[u] = true;

        for (let edge of graph.adjacencyList[u]) {
            if (edge.isClosed) continue;

            relaxationCount++;
            const v = edge.to;
            if (visited[v]) continue;

            // Perhitungan bobot rute menyertakan risiko penutupan dan macet
            const robustWeight = edge.getWeight(emergencyMode, true, lambda);
            const newDist = dist[u] + robustWeight;
            if (newDist < dist[v]) {
                dist[v] = newDist;
                prev[v] = u;
                heap.Insert(v, newDist);
                yield { type: 'relax', u, v, weight: robustWeight, dist: newDist };
            }
        }
    }

    const isReachable = reached || (dist[target] < Infinity);
    if (isReachable) {
        const path = [];
        let curr = target;
        while (curr !== -1) {
            path.push(curr);
            curr = prev[curr];
        }
        path.reverse();

        let totalTime = 0;
        let totalDist = 0;
        let totalRisk = 0;
        for (let i = 0; i < path.length - 1; i++) {
            const edge = graph.adjacencyList[path[i]].find(e => e.to === path[i+1]);
            if (edge) {
                totalTime += edge.getWeight(emergencyMode);
                totalDist += edge.distanceKm;
                totalRisk += edge.closureRisk + edge.trafficRisk;
            }
        }

        yield {
            type: 'complete',
            path,
            totalTravelTimeMinutes: totalTime,
            totalDistanceKm: totalDist,
            expandedNodes,
            relaxationCount,
            notes: `Total Skor Risiko Rute: ${totalRisk.toFixed(4)}`
        };
    } else {
        yield {
            type: 'unreachable',
            totalTravelTimeMinutes: -1,
            totalDistanceKm: 0,
            path: [],
            expandedNodes,
            relaxationCount,
            notes: "Tujuan tidak dapat dijangkau."
        };
    }
}

/**
 * Menjalankan generator rute secara langsung sampai selesai (Tanpa jeda animasi)
 */
function solveInstant(generator) {
    let result = null;
    let step = generator.next();
    while (!step.done) {
        if (step.value && (step.value.type === 'complete' || step.value.type === 'unreachable')) {
            result = step.value;
        }
        step = generator.next();
    }
    return result;
}

// ============================================================================
// 4. INSTANT STATIC MULTI-PATH & OTHER SOLVERS
// ============================================================================

/**
 * Mencari rute cadangan dengan menghukum jalan yang sudah terpakai
 */
function solveAlternativeRoutes(graph, source, target, k = 3, emergencyMode = false) {
    const results = [];
    
    // 1. Rute Utama
    const primGenerator = DijkstraGenerator(graph, source, target, emergencyMode);
    const primaryResult = solveInstant(primGenerator);
    if (!primaryResult || !primaryResult.path || primaryResult.path.length === 0) {
        return results;
    }
    primaryResult.algorithmName = "Rute Utama (Tercepat)";
    results.push(primaryResult);

    const penalizedEdges = new Map();
    const penaltyMultiplier = 2.0;

    // 2. Iterasi mencari rute cadangan
    for (let step = 2; step <= k; step++) {
        results.forEach(res => {
            if (res.path && res.path.length > 1) {
                for (let i = 0; i < res.path.length - 1; i++) {
                    penalizedEdges.set(`${res.path[i]}->${res.path[i+1]}`, penaltyMultiplier);
                }
            }
        });

        const altGenerator = DijkstraGenerator(graph, source, target, emergencyMode, penalizedEdges);
        const altResult = solveInstant(altGenerator);
        if (altResult && altResult.path && altResult.path.length > 0) {
            altResult.algorithmName = step === 2 ? "Rute Cadangan (Detour Utama)" : `Rute Alternatif ke-${step}`;
            results.push(altResult);
        } else {
            break;
        }
    }
    return results;
}

/**
 * Yen's K-Shortest Paths (Rute alternatif terpendek tanpa memutar tak tentu)
 */
function solveYenKShortestPaths(graph, source, target, k = 3, emergencyMode = false) {
    const A = [];
    const B = [];

    // Rute terpendek pertama
    const path0Result = solveInstant(DijkstraGenerator(graph, source, target, emergencyMode));
    if (!path0Result || !path0Result.path || path0Result.path.length === 0) {
        return A;
    }
    path0Result.algorithmName = "Rute Alternatif 1";
    A.push(path0Result);

    const temporarilyClosedEdges = [];

    for (let ki = 1; ki < k; ki++) {
        const previousPath = A[ki - 1].path;

        for (let i = 0; i < previousPath.length - 1; i++) {
            const spurNode = previousPath[i];
            const rootPath = previousPath.slice(0, i + 1);

            A.forEach(path => {
                if (path.path.length > i + 1) {
                    let match = true;
                    for (let j = 0; j <= i; j++) {
                        if (path.path[j] !== rootPath[j]) {
                            match = false;
                            break;
                        }
                    }
                    if (match) {
                        const from = path.path[i];
                        const to = path.path[i+1];
                        graph.adjacencyList[from].forEach(edge => {
                            if (edge.to === to && !edge.isClosed) {
                                edge.isClosed = true;
                                temporarilyClosedEdges.push(edge);
                            }
                        });
                    }
                }
            });

            // Hindari memutar
            for (let j = 0; j < rootPath.length - 1; j++) {
                const rootNode = rootPath[j];
                graph.adjacencyList[rootNode].forEach(edge => {
                    if (!edge.isClosed) {
                        edge.isClosed = true;
                        temporarilyClosedEdges.push(edge);
                    }
                });
                graph.reverseAdjacencyList[rootNode].forEach(edge => {
                    if (!edge.isClosed) {
                        edge.isClosed = true;
                        temporarilyClosedEdges.push(edge);
                    }
                });
            }

            const spurResult = solveInstant(DijkstraGenerator(graph, spurNode, target, emergencyMode));
            if (spurResult && spurResult.path && spurResult.path.length > 0) {
                const combinedPath = [...rootPath, ...spurResult.path.slice(1)];
                
                let totalTime = 0;
                let totalDist = 0;
                for (let j = 0; j < combinedPath.length - 1; j++) {
                    const edge = graph.adjacencyList[combinedPath[j]].find(e => e.to === combinedPath[j+1]);
                    if (edge) {
                        totalTime += edge.getWeight(emergencyMode);
                        totalDist += edge.distanceKm;
                    }
                }

                const pathStr = combinedPath.join(',');
                const isDup = A.some(p => p.path.join(',') === pathStr) || B.some(p => p.path.join(',') === pathStr);

                if (!isDup) {
                    B.push({
                        algorithmName: "Kandidat Rute Yen",
                        path: combinedPath,
                        totalTravelTimeMinutes: totalTime,
                        totalDistanceKm: totalDist,
                        expandedNodes: spurResult.expandedNodes,
                        relaxationCount: spurResult.relaxationCount,
                        notes: "Kandidat rute bebas memutar"
                    });
                }
            }

            temporarilyClosedEdges.forEach(e => e.isClosed = false);
            temporarilyClosedEdges.length = 0;
        }

        if (B.length === 0) break;

        B.sort((x, y) => x.totalTravelTimeMinutes - y.totalTravelTimeMinutes);
        const best = B.shift();
        best.algorithmName = `Rute Alternatif ${ki + 1}`;
        A.push(best);
    }

    return A;
}

/**
 * Pemeriksa Kebenaran Rute (Bellman-Ford Solver)
 * Memastikan rute terpendek yang dihitung 100% benar secara matematika murni.
 */
function solveBellmanFord(graph, source, target, emergencyMode = false) {
    const n = graph.vertexCount;
    const dist = Array(n).fill(Infinity);
    const prev = Array(n).fill(-1);

    dist[source] = 0;

    for (let i = 0; i < n - 1; i++) {
        let anyChange = false;
        for (let u = 0; u < n; u++) {
            if (dist[u] === Infinity) continue;
            for (let edge of graph.adjacencyList[u]) {
                if (edge.isClosed) continue;
                const v = edge.to;
                const w = edge.getWeight(emergencyMode);
                if (dist[u] + w < dist[v]) {
                    dist[v] = dist[u] + w;
                    prev[v] = u;
                    anyChange = true;
                }
            }
        }
        if (!anyChange) break;
    }

    let negativeCycle = false;
    for (let u = 0; u < n; u++) {
        if (dist[u] === Infinity) continue;
        for (let edge of graph.adjacencyList[u]) {
            if (edge.isClosed) continue;
            const v = edge.to;
            const w = edge.getWeight(emergencyMode);
            if (dist[u] + w < dist[v]) {
                negativeCycle = true;
                break;
            }
        }
    }

    return {
        dist: dist[target],
        path: reconstructPath(prev, target),
        hasNegativeCycle: negativeCycle
    };
}

function reconstructPath(prev, target) {
    if (prev[target] === -1 && target !== 0) return [];
    const path = [];
    let curr = target;
    while (curr !== -1) {
        path.push(curr);
        curr = prev[curr];
    }
    path.reverse();
    return path;
}

// ============================================================================
// 5. APPLICATION STATE & GRAPHICS CONTROLLER
// ============================================================================

const State = {
    graph: null,
    sourceId: 0,
    targetId: null,
    selectedNodeId: null,
    hospitals: new Set(),
    
    // Status visualisasi
    activePath: [],
    activePaths: [],
    visitedSet: new Set(),
    visitedForward: new Set(),
    visitedBackward: new Set(),
    relaxedEdges: new Map(),
    
    isPlaying: false,
    animationInterval: null,
    generator: null,
    
    // Pilihan UI
    family: "GridCity",
    nodeCount: 80,
    edgeCount: 180,
    seed: 42,
    algorithm: "Dijkstra",
    emergencyMode: true,
    traffic: "Normal",
    timePeriod: 1.0,
    lambda: 15,
    yenK: 3
};

let canvas, ctx;
let isDraggingNode = false;
let draggedNodeId = null;

const MarginLeft = 60;
const MarginRight = 380;
const MarginTop = 90;
const MarginBottom = 60;
let scaleX = 1, scaleY = 1;

function coordToCanvas(x, y) {
    const cx = MarginLeft + (x / 100) * (canvas.width - MarginLeft - MarginRight);
    const cy = MarginTop + (y / 100) * (canvas.height - MarginTop - MarginBottom);
    return { x: cx, y: cy };
}

function canvasToCoord(cx, cy) {
    const x = ((cx - MarginLeft) / (canvas.width - MarginLeft - MarginRight)) * 100;
    const y = ((cy - MarginTop) / (canvas.height - MarginTop - MarginBottom)) * 100;
    return { x: Math.max(0, Math.min(100, x)), y: Math.max(0, Math.min(100, y)) };
}

window.addEventListener('DOMContentLoaded', () => {
    canvas = document.getElementById('graphCanvas');
    ctx = canvas.getContext('2d');
    
    resizeCanvas();
    window.addEventListener('resize', () => {
        resizeCanvas();
        drawGraph();
    });

    readInputControls();
    generateNewGraph();
    setupEventListeners();
    
    triggerSolve(true);
});

function resizeCanvas() {
    const parent = canvas.parentElement;
    const rect = parent.getBoundingClientRect();
    canvas.width = rect.width;
    canvas.height = rect.height;
}

function readInputControls() {
    State.family = document.getElementById('graphFamily').value;
    State.nodeCount = parseInt(document.getElementById('nodeCount').value) || 80;
    State.edgeCount = parseInt(document.getElementById('edgeCount').value) || 180;
    State.seed = parseInt(document.getElementById('randomSeed').value) || 42;
    State.algorithm = document.getElementById('algorithmSelect').value;
    State.emergencyMode = document.getElementById('emergencyMode').checked;
    State.traffic = document.getElementById('trafficLevel').value;
    State.timePeriod = parseFloat(document.getElementById('timePeriod').value) || 1.0;
    State.lambda = parseInt(document.getElementById('lambdaWeight').value) || 15;
    State.yenK = parseInt(document.getElementById('yenK').value) || 3;
}

function generateNewGraph() {
    State.graph = CityGraphGenerator.generate(State.nodeCount, State.edgeCount, State.seed, State.family);
    
    State.sourceId = 0;
    State.targetId = State.nodeCount - 1;
    State.selectedNodeId = null;
    
    State.hospitals.clear();
    State.hospitals.add(State.targetId);
    
    applyTrafficState();
    clearPathVisuals();
    
    const tipeTekst = State.family === 'GridCity' ? 'Kota Grid teratur' : 'Peta Acak bebas';
    logToConsole(`Peta kota berhasil dibuat dengan ${State.nodeCount} persimpangan jalan dan ${State.graph.allEdges.length} ruas jalan. Kode Acak: ${State.seed}. Model: ${tipeTekst}.`, 'system');
}

function applyTrafficState() {
    if (!State.graph) return;
    State.graph.resetTraffic();
    
    if (State.traffic === "Severe") {
        State.graph.applySevereTraffic();
    } else if (State.traffic === "Random") {
        State.graph.applyRandomTraffic(State.seed);
    }
    
    State.graph.allEdges.forEach(edge => {
        edge.timePeriodMultiplier = State.timePeriod;
    });
}

function clearPathVisuals() {
    State.activePath = [];
    State.activePaths = [];
    State.visitedSet.clear();
    State.visitedForward.clear();
    State.visitedBackward.clear();
    State.relaxedEdges.clear();
    stopAnimation();
}

function getFriendlyAlgorithmName(id) {
    switch(id) {
        case "Dijkstra": return "Dijkstra Standar";
        case "AStar": return "A* Search (Pintar)";
        case "BidirectionalDijkstra": return "Bidirectional Dijkstra";
        case "Robust": return "Robust (Aman)";
        case "Alternative": return "Alternatif (Penalti)";
        case "Yen": return "Alternatif (Yen's)";
        case "MultiHospital": return "Multi-Hospital Nearest";
        default: return id;
    }
}

function setupEventListeners() {
    document.getElementById('btnGenerate').addEventListener('click', () => {
        readInputControls();
        generateNewGraph();
        triggerSolve(true);
    });

    document.getElementById('btnRandomSeed').addEventListener('click', () => {
        const newSeed = Math.floor(Math.random() * 9999) + 1;
        document.getElementById('randomSeed').value = newSeed;
        State.seed = newSeed;
        generateNewGraph();
        triggerSolve(true);
    });

    document.getElementById('algorithmSelect').addEventListener('change', (e) => {
        State.algorithm = e.target.value;
        toggleSolverParamBoxes();
        clearPathVisuals();
        triggerSolve(true);
    });

    document.getElementById('emergencyMode').addEventListener('change', (e) => {
        State.emergencyMode = e.target.checked;
        applyTrafficState();
        triggerSolve(true);
    });

    document.getElementById('trafficLevel').addEventListener('change', (e) => {
        State.traffic = e.target.value;
        applyTrafficState();
        triggerSolve(true);
    });

    document.getElementById('timePeriod').addEventListener('input', (e) => {
        const val = parseFloat(e.target.value);
        document.getElementById('timePeriodVal').textContent = val.toFixed(1) + 'x';
        State.timePeriod = val;
        applyTrafficState();
        triggerSolve(true);
    });

    document.getElementById('lambdaWeight').addEventListener('input', (e) => {
        const val = parseInt(e.target.value);
        document.getElementById('lambdaWeightVal').textContent = val;
        State.lambda = val;
        triggerSolve(true);
    });

    document.getElementById('yenK').addEventListener('input', (e) => {
        const val = parseInt(e.target.value);
        document.getElementById('yenKVal').textContent = val;
        State.yenK = val;
        triggerSolve(true);
    });

    document.getElementById('btnRandomClosure').addEventListener('click', () => {
        State.graph.closeRandomEdges(0.10, State.seed + 1);
        logToConsole("Menutup 10% ruas jalan secara acak karena kondisi darurat.", "warning");
        triggerSolve(true);
    });

    document.getElementById('btnResetClosures').addEventListener('click', () => {
        State.graph.resetClosures();
        logToConsole("Semua penutupan jalan dibersihkan. Seluruh rute kembali terbuka.", "system");
        triggerSolve(true);
    });

    document.getElementById('btnSolve').addEventListener('click', () => {
        triggerSolve(true);
    });

    document.getElementById('btnAnimate').addEventListener('click', () => {
        triggerAnimate();
    });

    document.getElementById('btnStop').addEventListener('click', () => {
        stopAnimation();
    });

    document.getElementById('btnClearConsole').addEventListener('click', () => {
        document.getElementById('consoleLog').innerHTML = '';
    });

    canvas.addEventListener('mousedown', handleMouseDown);
    canvas.addEventListener('mousemove', handleMouseMove);
    canvas.addEventListener('mouseup', handleMouseUp);
    canvas.addEventListener('dblclick', handleDoubleClick);
    canvas.addEventListener('contextmenu', e => e.preventDefault());

    // Tab switching logic for floating panel
    const tabButtons = document.querySelectorAll('.panel-tabs .tab-btn');
    tabButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            tabButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            
            const targetTabId = btn.getAttribute('data-tab');
            const tabContents = document.querySelectorAll('.floating-panel .tab-content');
            tabContents.forEach(content => {
                if (content.id === targetTabId) {
                    content.classList.add('active');
                } else {
                    content.classList.remove('active');
                }
            });
        });
    });
}

function toggleSolverParamBoxes() {
    document.getElementById('yenParams').classList.add('hidden');
    document.getElementById('robustParams').classList.add('hidden');
    
    if (State.algorithm === "Yen" || State.algorithm === "Alternative") {
        document.getElementById('yenParams').classList.remove('hidden');
    } else if (State.algorithm === "Robust") {
        document.getElementById('robustParams').classList.remove('hidden');
    }
}

// ============================================================================
// 6. CANVAS MOUSE INTERACTION HANDLERS
// ============================================================================

function findVertexNear(cx, cy, threshold = 12) {
    if (!State.graph) return -1;
    for (let v of State.graph.vertices) {
        const canvasCoords = coordToCanvas(v.x, v.y);
        const dx = canvasCoords.x - cx;
        const dy = canvasCoords.y - cy;
        const dist = Math.sqrt(dx * dx + dy * dy);
        if (dist <= threshold) {
            return v.id;
        }
    }
    return -1;
}

function findEdgeNear(cx, cy, threshold = 6) {
    if (!State.graph) return null;
    
    for (let edge of State.graph.allEdges) {
        const uNode = State.graph.vertices[edge.from];
        const vNode = State.graph.vertices[edge.to];
        
        const pt1 = coordToCanvas(uNode.x, uNode.y);
        const pt2 = coordToCanvas(vNode.x, vNode.y);
        
        const dist = pointToSegmentDistance(cx, cy, pt1.x, pt1.y, pt2.x, pt2.y);
        if (dist <= threshold) {
            return edge;
        }
    }
    return null;
}

function pointToSegmentDistance(px, py, x1, y1, x2, y2) {
    const dx = x2 - x1;
    const dy = y2 - y1;
    const l2 = dx * dx + dy * dy;
    if (l2 === 0) return Math.sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
    
    let t = ((px - x1) * dx + (py - y1) * dy) / l2;
    t = Math.max(0, Math.min(1, t));
    
    const projX = x1 + t * dx;
    const projY = y1 + t * dy;
    
    return Math.sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
}

function handleMouseDown(e) {
    const rect = canvas.getBoundingClientRect();
    const cx = e.clientX - rect.left;
    const cy = e.clientY - rect.top;
    
    const clickedVertex = findVertexNear(cx, cy);
    if (clickedVertex !== -1) {
        isDraggingNode = true;
        draggedNodeId = clickedVertex;
        State.selectedNodeId = clickedVertex;
        updateSelectedNodePanel();
        
        if (e.button === 2) {
            e.preventDefault();
            if (clickedVertex === State.sourceId) {
                logToConsole("Titik Mulai Ambulans tidak bisa diubah langsung menjadi Tujuan.", "warning");
            } else {
                State.targetId = clickedVertex;
                logToConsole(`Titik Tujuan Ambulans dipindahkan ke persimpangan ${clickedVertex}.`, 'system');
                triggerSolve(true);
            }
        }
        return;
    }
    
    const clickedEdge = findEdgeNear(cx, cy);
    if (clickedEdge) {
        clickedEdge.isClosed = !clickedEdge.isClosed;
        logToConsole(`Jalur ${clickedEdge.from} → ${clickedEdge.to} sekarang ${clickedEdge.isClosed ? 'DITUTUP' : 'DIBUKA KEMBALI'}.`, clickedEdge.isClosed ? 'danger' : 'system');
        triggerSolve(true);
        return;
    }

    State.selectedNodeId = null;
    updateSelectedNodePanel();
}

function handleMouseMove(e) {
    const rect = canvas.getBoundingClientRect();
    const cx = e.clientX - rect.left;
    const cy = e.clientY - rect.top;
    
    if (isDraggingNode && draggedNodeId !== null) {
        const newCoords = canvasToCoord(cx, cy);
        const v = State.graph.vertices[draggedNodeId];
        v.x = newCoords.x;
        v.y = newCoords.y;
        
        const reCalcEdge = (edge) => {
            const fromV = State.graph.vertices[edge.from];
            const toV = State.graph.vertices[edge.to];
            edge.distanceKm = calculateEuclideanDistance(fromV, toV);
            edge.travelTimeMinutes = (edge.distanceKm / edge.speedKmh) * 60.0;
        };
        
        State.graph.adjacencyList[draggedNodeId].forEach(reCalcEdge);
        State.graph.reverseAdjacencyList[draggedNodeId].forEach(reCalcEdge);
        
        for (let u = 0; u < State.graph.vertexCount; u++) {
            State.graph.adjacencyList[u].forEach(edge => {
                if (edge.to === draggedNodeId) {
                    reCalcEdge(edge);
                }
            });
        }
        
        triggerSolve(false); // Update instan saat drag (tanpa banjir log)
        return;
    }
    
    updateTooltip(cx, cy);
}

function handleMouseUp(e) {
    if (isDraggingNode) {
        isDraggingNode = false;
        draggedNodeId = null;
        triggerSolve(true);
    }
}

function handleDoubleClick(e) {
    const rect = canvas.getBoundingClientRect();
    const cx = e.clientX - rect.left;
    const cy = e.clientY - rect.top;
    
    const clickedVertex = findVertexNear(cx, cy);
    if (clickedVertex !== -1) {
        if (clickedVertex === State.sourceId) {
            logToConsole("Titik Awal Ambulans tidak bisa diubah menjadi Rumah Sakit.", "warning");
            return;
        }
        
        if (State.hospitals.has(clickedVertex)) {
            if (clickedVertex === State.targetId) {
                logToConsole("Rumah sakit tujuan utama tidak boleh dinonaktifkan.", "warning");
                return;
            }
            State.hospitals.delete(clickedVertex);
            logToConsole(`Titik ${clickedVertex} dihapus dari daftar Rumah Sakit Alternatif.`, 'warning');
        } else {
            State.hospitals.add(clickedVertex);
            logToConsole(`Titik ${clickedVertex} berhasil ditandai sebagai Rumah Sakit Alternatif.`, 'success');
        }
        
        triggerSolve(true);
    }
}

function updateSelectedNodePanel() {
    if (State.selectedNodeId !== null) {
        const v = State.graph.vertices[State.selectedNodeId];
        logToConsole(`Memilih Persimpangan ${v.id}: "${v.name}" (Koordinat X: ${v.x.toFixed(1)}%, Y: ${v.y.toFixed(1)}%)`, 'system');
    }
}

function updateTooltip(cx, cy) {
    const tooltip = document.getElementById('hoverTooltip');
    const vId = findVertexNear(cx, cy);
    
    if (vId !== -1) {
        const v = State.graph.vertices[vId];
        let role = "Persimpangan Jalan Biasa";
        if (vId === State.sourceId) role = "Titik Awal Ambulans (Start)";
        else if (vId === State.targetId) role = "Rumah Sakit Tujuan Utama (Target)";
        else if (State.hospitals.has(vId)) role = "Rumah Sakit Alternatif (Hospital)";

        tooltip.innerHTML = `
            <div style="font-weight:700; color:var(--color-primary);">${v.name}</div>
            <div>Nomor Titik: <strong>ID ${v.id}</strong></div>
            <div>Status Titik: <strong>${role}</strong></div>
            <div>Posisi Koordinat: <strong>(${v.x.toFixed(1)}%, ${v.y.toFixed(1)}%)</strong></div>
        `;
        tooltip.style.left = (cx + 15) + 'px';
        tooltip.style.top = (cy + 15) + 'px';
        tooltip.classList.remove('hidden');
        return;
    }
    
    const edge = findEdgeNear(cx, cy);
    if (edge) {
        let trafficText = "Lancar";
        if (edge.traffic === "High") trafficText = "Padat";
        else if (edge.traffic === "Severe") trafficText = "Macet Parah";
        
        const priorityLane = edge.hasEmergencyLane ? "<span style='color:var(--color-success)'>Ada Jalur Khusus</span>" : "Jalan Biasa";
        tooltip.innerHTML = `
            <div style="font-weight:700; color:var(--text-primary);">Ruas Jalan ${edge.from} → ${edge.to}</div>
            <div>Panjang Fisik: <strong>${edge.distanceKm.toFixed(2)} Km</strong></div>
            <div>Batas Kecepatan: <strong>${edge.speedKmh.toFixed(1)} Km/jam</strong></div>
            <div>Tingkat Kemacetan: <strong style="color:${edge.traffic === 'Severe' ? 'var(--color-severe)' : 'var(--text-secondary)'}">${trafficText}</strong></div>
            <div>Fasilitas Jalan: <strong>${priorityLane}</strong></div>
            <div>Status Jalan: <strong style="color:${edge.isClosed ? 'var(--color-danger)' : 'var(--color-success)'}">${edge.isClosed ? 'DITUTUP' : 'BUKA'}</strong></div>
            <div style="border-top:1px solid #cbd5e1; margin-top:4px; padding-top:4px;">
                Waktu Tempuh Ambulans: <strong>${edge.getWeight(State.emergencyMode, State.algorithm === 'Robust', State.lambda).toFixed(2)} menit</strong>
            </div>
        `;
        tooltip.style.left = (cx + 15) + 'px';
        tooltip.style.top = (cy + 15) + 'px';
        tooltip.classList.remove('hidden');
        return;
    }
    
    tooltip.classList.add('hidden');
}

// ============================================================================
// 7. PATHFINDING TRIGGER & ANNOTATION LOOPS
// ============================================================================

function triggerSolve(verbose = true) {
    if (!State.graph) return;
    
    stopAnimation();
    
    State.activePath = [];
    State.activePaths = [];
    State.visitedSet.clear();
    State.relaxedEdges.clear();

    const tStart = performance.now();
    let solution = null;
    let logMsg = "";
    
    if (State.algorithm === "Dijkstra") {
        State.generator = DijkstraGenerator(State.graph, State.sourceId, State.targetId, State.emergencyMode);
        solution = solveInstant(State.generator);
        if (solution && solution.path) {
            State.activePath = solution.path;
        }
    } 
    else if (State.algorithm === "AStar") {
        State.generator = AStarGenerator(State.graph, State.sourceId, State.targetId, 100.0, State.emergencyMode);
        solution = solveInstant(State.generator);
        if (solution && solution.path) {
            State.activePath = solution.path;
        }
    } 
    else if (State.algorithm === "BidirectionalDijkstra") {
        State.generator = BidirectionalDijkstraGenerator(State.graph, State.sourceId, State.targetId, State.emergencyMode);
        solution = solveInstant(State.generator);
        if (solution && solution.path) {
            State.activePath = solution.path;
        }
    }
    else if (State.algorithm === "Robust") {
        State.generator = RobustRouteGenerator(State.graph, State.sourceId, State.targetId, State.emergencyMode, State.lambda);
        solution = solveInstant(State.generator);
        if (solution && solution.path) {
            State.activePath = solution.path;
        }
    }
    else if (State.algorithm === "Alternative") {
        const altPaths = solveAlternativeRoutes(State.graph, State.sourceId, State.targetId, State.yenK, State.emergencyMode);
        State.activePaths = altPaths;
        if (altPaths.length > 0) {
            State.activePath = altPaths[0].path;
            solution = altPaths[0];
        }
    }
    else if (State.algorithm === "Yen") {
        const yenPaths = solveYenKShortestPaths(State.graph, State.sourceId, State.targetId, State.yenK, State.emergencyMode);
        State.activePaths = yenPaths;
        if (yenPaths.length > 0) {
            State.activePath = yenPaths[0].path;
            solution = yenPaths[0];
        }
    }
    else if (State.algorithm === "MultiHospital") {
        let bestHospitalId = -1;
        let bestHospitalTime = Infinity;
        const hospitalResults = [];
        
        State.hospitals.forEach(hId => {
            const solverGen = DijkstraGenerator(State.graph, State.sourceId, hId, State.emergencyMode);
            const res = solveInstant(solverGen);
            if (res && res.path && res.path.length > 0) {
                res.hospitalId = hId;
                res.hospitalName = State.graph.vertices[hId].name;
                hospitalResults.push(res);
                if (res.totalTravelTimeMinutes < bestHospitalTime) {
                    bestHospitalTime = res.totalTravelTimeMinutes;
                    bestHospitalId = hId;
                    solution = res;
                }
            }
        });
        
        State.activePaths = hospitalResults;
        if (solution) {
            State.activePath = solution.path;
        }
        
        populateHospitalTable(hospitalResults);
    }
    
    const tEnd = performance.now();
    const durationMs = tEnd - tStart;
    
    updateMetricsPanel(solution, durationMs);
    
    if (State.algorithm !== "MultiHospital" && State.activePath.length > 0) {
        validateWithBellmanFord(solution);
    } else {
        resetValidationCard();
    }
    
    runComparativeAnalysis();
    drawGraph();
    
    if (verbose && solution) {
        const formulaName = getFriendlyAlgorithmName(State.algorithm);
        if (solution.path && solution.path.length > 0) {
            logMsg = `[RUTE] Berhasil mencari rute tercepat via ${formulaName}. Waktu Tempuh: ${solution.totalTravelTimeMinutes.toFixed(2)} menit. Panjang Rute: ${solution.totalDistanceKm.toFixed(2)} Km. Jalan Diperiksa: ${solution.expandedNodes} titik.`;
            logToConsole(logMsg, 'success');
        } else {
            logToConsole(`[GAGAL] Tidak ada rute yang terhubung. Ambulance tidak bisa mencapai lokasi tujuan karena jalan ditutup!`, 'danger');
        }
    }
}

function triggerAnimate() {
    if (!State.graph) return;
    
    stopAnimation();
    clearPathVisuals();
    readInputControls();
    
    if (State.algorithm === "Dijkstra") {
        State.generator = DijkstraGenerator(State.graph, State.sourceId, State.targetId, State.emergencyMode);
    } 
    else if (State.algorithm === "AStar") {
        State.generator = AStarGenerator(State.graph, State.sourceId, State.targetId, 100.0, State.emergencyMode);
    } 
    else if (State.algorithm === "BidirectionalDijkstra") {
        State.generator = BidirectionalDijkstraGenerator(State.graph, State.sourceId, State.targetId, State.emergencyMode);
    }
    else if (State.algorithm === "Robust") {
        State.generator = RobustRouteGenerator(State.graph, State.sourceId, State.targetId, State.emergencyMode, State.lambda);
    }
    else {
        logToConsole(`Metode '${getFriendlyAlgorithmName(State.algorithm)}' tidak memerlukan simulasi animasi bertahap. Cukup klik 'Cari Rute Langsung'.`, 'warning');
        return;
    }
    
    const formulaName = getFriendlyAlgorithmName(State.algorithm);
    logToConsole(`Memulai simulasi pencarian rute langkah demi langkah dengan ${formulaName}...`, 'system');
    
    State.isPlaying = true;
    document.getElementById('btnStop').classList.remove('hidden');
    document.getElementById('btnAnimate').classList.add('hidden');
    
    const speed = parseInt(document.getElementById('animSpeed').value);
    const intervalMs = Math.max(5, 200 - speed * 1.95);
    
    State.animationInterval = setInterval(() => {
        const step = State.generator.next();
        if (step.done) {
            stopAnimation();
            return;
        }
        
        const val = step.value;
        if (val) {
            if (val.type === 'visit') {
                State.visitedSet.add(val.u);
                drawGraph();
            } 
            else if (val.type === 'visit_forward') {
                State.visitedForward.add(val.u);
                drawGraph();
            } 
            else if (val.type === 'visit_backward') {
                State.visitedBackward.add(val.u);
                drawGraph();
            } 
            else if (val.type === 'relax') {
                State.relaxedEdges.set(val.v, val.u);
                drawGraph();
            }
            else if (val.type === 'relax_forward') {
                State.relaxedEdges.set('F_' + val.v, val.u);
                drawGraph();
            }
            else if (val.type === 'relax_backward') {
                State.relaxedEdges.set('B_' + val.v, val.u);
                drawGraph();
            }
            else if (val.type === 'meet') {
                logToConsole(`Titik temu terdeteksi di persimpangan jalan nomor ${val.meetingVertex}! Estimasi waktu sementara: ${val.bestDistance.toFixed(2)} menit`, 'success');
            }
            else if (val.type === 'complete') {
                State.activePath = val.path;
                drawGraph();
                updateMetricsPanel(val, 0);
                validateWithBellmanFord(val);
                runComparativeAnalysis();
                logToConsole(`Simulasi Selesai. Rute tercepat ditemukan. Waktu: ${val.totalTravelTimeMinutes.toFixed(2)} menit. Total jalan diperiksa: ${val.expandedNodes} persimpangan.`, 'success');
                stopAnimation();
            } 
            else if (val.type === 'unreachable') {
                drawGraph();
                updateMetricsPanel(val, 0);
                logToConsole("Simulasi Selesai. Rumah sakit tujuan tidak dapat dijangkau!", 'danger');
                stopAnimation();
            }
        }
    }, intervalMs);
}

function stopAnimation() {
    State.isPlaying = false;
    if (State.animationInterval) {
        clearInterval(State.animationInterval);
        State.animationInterval = null;
    }
    document.getElementById('btnStop').classList.add('hidden');
    document.getElementById('btnAnimate').classList.remove('hidden');
}

// ============================================================================
// 8. RENDER MANAGER & CANVAS GRAPH DRAWING
// ============================================================================

function drawGraph() {
    if (!ctx || !State.graph) return;
    
    // Background peta: abu-abu sangat muda bersih (menenangkan mata)
    ctx.fillStyle = '#f8fafc';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    
    drawGridBackground();
    
    // 1. Gambar jalan biasa
    State.graph.allEdges.forEach(edge => {
        if (!isInActivePath(edge.from, edge.to) && !isInMultiPath(edge.from, edge.to)) {
            drawEdge(edge, false, false);
        }
    });

    // 2. Gambar alur relaksasi saat simulasi
    if (State.isPlaying) {
        drawRelaxationEdges();
    }

    // 3. Rute alternatif digambar dalam satu warna abu-abu transparan (mengurangi pusing)
    if (State.activePaths.length > 0) {
        const reversedPaths = [...State.activePaths].reverse();
        reversedPaths.forEach((pathRes, idx) => {
            const originalIndex = State.activePaths.indexOf(pathRes);
            if (originalIndex > 0) {
                // Semua rute cadangan berwarna abu-abu transparan tipis
                const pathColor = 'rgba(148, 163, 184, 0.4)';
                if (pathRes.path && pathRes.path.length > 1) {
                    for (let i = 0; i < pathRes.path.length - 1; i++) {
                        const u = pathRes.path[i];
                        const v = pathRes.path[i + 1];
                        const edge = State.graph.adjacencyList[u].find(e => e.to === v);
                        if (edge) {
                            drawPathEdge(edge, pathColor, 3.5, true); // Garis cadangan putus-putus halus
                        }
                    }
                }
            }
        });
    }

    // 4. Gambar Rute Utama (Fokus utama berwarna Royal Blue solid)
    if (State.activePath && State.activePath.length > 1) {
        for (let i = 0; i < State.activePath.length - 1; i++) {
            const u = State.activePath[i];
            const v = State.activePath[i + 1];
            const edge = State.graph.adjacencyList[u].find(e => e.to === v);
            if (edge) {
                drawPathEdge(edge, '#3b82f6', 4.5, false); // Garis rute utama biru solid tebal
            }
        }
    }

    // 5. Gambar titik persimpangan jalan
    State.graph.vertices.forEach(v => {
        drawVertex(v);
    });
}

function drawGridBackground() {
    ctx.save();
    ctx.strokeStyle = 'rgba(148, 163, 184, 0.04)'; // Subtle slate-400 grid lines
    ctx.lineWidth = 1;
    
    const size = 30; // Grid square size in pixels
    // Vertical lines
    for (let x = 0; x < canvas.width; x += size) {
        ctx.beginPath();
        ctx.moveTo(x, 0);
        ctx.lineTo(x, canvas.height);
        ctx.stroke();
    }
    // Horizontal lines
    for (let y = 0; y < canvas.height; y += size) {
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(canvas.width, y);
        ctx.stroke();
    }
    ctx.restore();
}

function drawEdge(edge, isHighlighted, isRelaxing) {
    const u = State.graph.vertices[edge.from];
    const v = State.graph.vertices[edge.to];
    
    let p1 = coordToCanvas(u.x, u.y);
    let p2 = coordToCanvas(v.x, v.y);
    
    let doubleEdge = false;
    State.graph.adjacencyList[edge.to].forEach(revEdge => {
        if (revEdge.to === edge.from) doubleEdge = true;
    });
    
    if (doubleEdge) {
        const dx = p2.x - p1.x;
        const dy = p2.y - p1.y;
        const len = Math.sqrt(dx * dx + dy * dy);
        if (len > 0) {
            const px = -dy / len;
            const py = dx / len;
            const offset = 3;
            p1.x += px * offset;
            p1.y += py * offset;
            p2.x += px * offset;
            p2.y += py * offset;
        }
    }

    ctx.save();
    
    let strokeColor = '#e2e8f0'; // Jalan normal abu-abu sangat muda (menyatu dengan peta)
    let lineDash = [];
    let lineWidth = 1.2;
    
    if (edge.isClosed) {
        strokeColor = '#cbd5e1'; // Jalan ditutup berwarna abu-abu redup putus-putus
        lineDash = [2, 3];
        lineWidth = 1.0;
    } else {
        // Hanya mewarnai kemacetan parah dan padat agar tidak penuh warna
        if (edge.traffic === 'High') {
            strokeColor = '#f59e0b'; // Amber kalem untuk padat
            lineWidth = 1.5;
        } else if (edge.traffic === 'Severe') {
            strokeColor = '#ef4444'; // Red kalem untuk macet parah
            lineWidth = 1.8;
        }
        
        if (edge.hasEmergencyLane && State.emergencyMode) {
            lineWidth += 0.5; // Sedikit tebal untuk jalur darurat
        }
    }

    ctx.strokeStyle = strokeColor;
    ctx.lineWidth = lineWidth;
    ctx.setLineDash(lineDash);
    
    ctx.beginPath();
    ctx.moveTo(p1.x, p1.y);
    ctx.lineTo(p2.x, p2.y);
    ctx.stroke();

    if (!edge.isClosed) {
        drawArrowhead(p1.x, p1.y, p2.x, p2.y, strokeColor, 5);
    }
    
    ctx.restore();
}

function drawPathEdge(edge, pathColor, width, isDashed = false) {
    const u = State.graph.vertices[edge.from];
    const v = State.graph.vertices[edge.to];
    
    let p1 = coordToCanvas(u.x, u.y);
    let p2 = coordToCanvas(v.x, v.y);
    
    let doubleEdge = false;
    State.graph.adjacencyList[edge.to].forEach(revEdge => {
        if (revEdge.to === edge.from) doubleEdge = true;
    });
    
    if (doubleEdge) {
        const dx = p2.x - p1.x;
        const dy = p2.y - p1.y;
        const len = Math.sqrt(dx * dx + dy * dy);
        if (len > 0) {
            const px = -dy / len;
            const py = dx / len;
            const offset = 3;
            p1.x += px * offset;
            p1.y += py * offset;
            p2.x += px * offset;
            p2.y += py * offset;
        }
    }

    ctx.save();
    
    // Draw soft glow under solid main paths
    if (!isDashed) {
        ctx.save();
        // If pathColor is blue (#2563eb or #3b82f6), use a soft blue glow. Otherwise, match the path color with low opacity.
        ctx.strokeStyle = (pathColor === '#2563eb' || pathColor === '#3b82f6') ? 'rgba(59, 130, 246, 0.22)' : 'rgba(148, 163, 184, 0.2)';
        ctx.lineWidth = width + 4.5;
        ctx.lineCap = 'round';
        ctx.beginPath();
        ctx.moveTo(p1.x, p1.y);
        ctx.lineTo(p2.x, p2.y);
        ctx.stroke();
        ctx.restore();
    }

    ctx.strokeStyle = pathColor;
    ctx.lineWidth = width;
    ctx.lineCap = 'round';
    
    if (isDashed) {
        ctx.setLineDash([3, 3]); // Rute cadangan putus-putus agar berbeda dari rute utama
    } else {
        ctx.setLineDash([]);
    }
    
    ctx.beginPath();
    ctx.moveTo(p1.x, p1.y);
    ctx.lineTo(p2.x, p2.y);
    ctx.stroke();

    drawArrowhead(p1.x, p1.y, p2.x, p2.y, pathColor, width + 2);
    
    ctx.restore();
}

function drawRelaxationEdges() {
    State.relaxedEdges.forEach((parent, childStr) => {
        let child = childStr;
        let color = '#94a3b8'; // Abu-abu netral untuk simulasi
        if (typeof child === 'string' && child.startsWith('F_')) {
            child = parseInt(child.substring(2));
            color = 'rgba(37, 99, 235, 0.3)'; // Biru halus
        } else if (typeof child === 'string' && child.startsWith('B_')) {
            child = parseInt(child.substring(2));
            color = 'rgba(219, 39, 119, 0.3)'; // Pink halus
        } else {
            child = parseInt(child);
            color = 'rgba(37, 99, 235, 0.3)';
        }

        const u = State.graph.vertices[parent];
        const v = State.graph.vertices[child];
        if (u && v) {
            const p1 = coordToCanvas(u.x, u.y);
            const p2 = coordToCanvas(v.x, v.y);
            ctx.save();
            ctx.strokeStyle = color;
            ctx.lineWidth = 1.5;
            ctx.setLineDash([2, 3]);
            ctx.beginPath();
            ctx.moveTo(p1.x, p1.y);
            ctx.lineTo(p2.x, p2.y);
            ctx.stroke();
            ctx.restore();
        }
    });
}

function drawArrowhead(x1, y1, x2, y2, color, arrowSize) {
    const angle = Math.atan2(y2 - y1, x2 - x1);
    const offset = 8;
    const ax = x2 - offset * Math.cos(angle);
    const ay = y2 - offset * Math.sin(angle);
    
    ctx.save();
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.moveTo(ax, ay);
    ctx.lineTo(ax - arrowSize * Math.cos(angle - Math.PI / 8), ay - arrowSize * Math.sin(angle - Math.PI / 8));
    ctx.lineTo(ax - arrowSize * Math.cos(angle + Math.PI / 8), ay - arrowSize * Math.sin(angle + Math.PI / 8));
    ctx.closePath();
    ctx.fill();
    ctx.restore();
}

function drawVertex(v) {
    const coords = coordToCanvas(v.x, v.y);
    const cx = coords.x;
    const cy = coords.y;
    
    let radius = 2.0;       // Sangat kecil agar bersih (mengurangi pusing)
    let fill = '#94a3b8';   // Warna abu-abu pudar menyatu dengan jalan
    let stroke = '#94a3b8';
    let strokeWidth = 0;
    let isSpecial = false;
    let label = '';

    if (v.id === State.sourceId) {
        fill = '#3b82f6'; // Softer Electric Blue
        stroke = '#ffffff';
        radius = 8.5;
        isSpecial = true;
        label = 'S';
    } 
    else if (v.id === State.targetId) {
        fill = '#ef4444'; // Softer Red
        stroke = '#ffffff';
        radius = 8.5;
        isSpecial = true;
        label = 'T';
    } 
    else if (State.hospitals.has(v.id)) {
        fill = '#10b981'; // Emerald Green
        stroke = '#ffffff';
        radius = 8.5;
        isSpecial = true;
        label = 'H';
    } 
    else if (State.visitedForward.has(v.id) || State.visitedBackward.has(v.id) || State.visitedSet.has(v.id)) {
        fill = '#cbd5e1'; // Light grey when visited
        stroke = '#94a3b8';
        radius = 2.5;
        strokeWidth = 0.5;
    }
    
    ctx.save();
    if (isSpecial) {
        ctx.shadowColor = 'rgba(15, 23, 42, 0.18)';
        ctx.shadowBlur = 5;
        ctx.shadowOffsetY = 2;
    }
    ctx.fillStyle = fill;
    ctx.strokeStyle = stroke;
    ctx.lineWidth = strokeWidth || (isSpecial ? 2 : 0);
    
    ctx.beginPath();
    ctx.arc(cx, cy, radius, 0, 2 * Math.PI);
    ctx.fill();
    if (ctx.lineWidth > 0) ctx.stroke();
    ctx.restore();

    if (label) {
        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 9px var(--font-body)';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(label, cx, cy);
    }
    
    if (State.selectedNodeId === v.id) {
        ctx.save();
        ctx.strokeStyle = '#0f172a';
        ctx.lineWidth = 1;
        ctx.setLineDash([2, 2]);
        ctx.beginPath();
        ctx.arc(cx, cy, radius + 3, 0, 2 * Math.PI);
        ctx.stroke();
        ctx.restore();
    }
}

function isInActivePath(u, v) {
    if (!State.activePath || State.activePath.length < 2) return false;
    for (let i = 0; i < State.activePath.length - 1; i++) {
        if (State.activePath[i] === u && State.activePath[i+1] === v) {
            return true;
        }
    }
    return false;
}

function isInMultiPath(u, v) {
    if (State.activePaths.length === 0) return false;
    return State.activePaths.some(p => {
        if (!p.path) return false;
        for (let i = 0; i < p.path.length - 1; i++) {
            if (p.path[i] === u && p.path[i+1] === v) return true;
        }
        return false;
    });
}

function getPathColorForIndex(idx) {
    const colors = [
        '#2563eb', // Royal Blue
        '#7c3aed', // Purple
        '#ea580c', // Orange
        '#db2777', // Pink
        '#16a34a', // Hijau
        '#0891b2'  // Cyan
    ];
    return colors[idx % colors.length];
}

// ============================================================================
// 9. METRICS & LIVE DASHBOARD HANDLERS
// ============================================================================

function updateMetricsPanel(solution, durationMs) {
    const metricTime = document.getElementById('metricTime');
    const metricExpanded = document.getElementById('metricExpanded');
    const metricDistance = document.getElementById('metricDistance');
    const metricRuntime = document.getElementById('metricRuntime');
    
    if (solution && solution.totalTravelTimeMinutes !== -1) {
        metricTime.textContent = solution.totalTravelTimeMinutes.toFixed(2) + ' mnt';
        metricExpanded.textContent = (solution.expandedNodes || '--') + ' titik';
        metricDistance.textContent = solution.totalDistanceKm.toFixed(2) + ' Km';
        metricRuntime.textContent = durationMs > 0 ? durationMs.toFixed(2) + ' ms' : '< 1 ms';
        
        document.querySelectorAll('.metric-val').forEach(el => el.classList.remove('empty'));
    } else {
        metricTime.textContent = 'GAGAL';
        metricExpanded.textContent = '--';
        metricDistance.textContent = '0.00';
        metricRuntime.textContent = '--';
    }
}

function populateHospitalTable(hospitalResults) {
    const tableBody = document.getElementById('hospitalTableBody');
    tableBody.innerHTML = '';
    
    if (hospitalResults.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="3" class="empty-table-msg">Semua Rumah Sakit tujuan tidak dapat dihubungi!</td></tr>`;
        return;
    }
    
    hospitalResults.sort((a, b) => a.totalTravelTimeMinutes - b.totalTravelTimeMinutes);
    
    hospitalResults.forEach((res, index) => {
        const row = document.createElement('tr');
        if (index === 0) {
            row.style.background = '#f0fdf4';
        }
        
        const rank = index + 1;
        const nameText = res.hospitalId === State.targetId ? `Rumah Sakit ${res.hospitalId} (Tujuan Utama)` : `Rumah Sakit ${res.hospitalId}`;
        
        row.innerHTML = `
            <td><strong style="color:${index === 0 ? 'var(--color-hospital)' : 'inherit'}">${nameText}</strong></td>
            <td><strong>${res.totalTravelTimeMinutes.toFixed(2)} menit</strong></td>
            <td><span class="status-badge" style="background:${index === 0 ? '#dcfce7' : '#f1f5f9'}; color:${index === 0 ? '#15803d' : 'var(--text-secondary)'}; border:none; padding: 2px 8px;">Peringkat ${rank}</span></td>
        `;
        tableBody.appendChild(row);
    });
}

function validateWithBellmanFord(solution) {
    const validationTitle = document.getElementById('validationTitle');
    const validationDesc = document.getElementById('validationDesc');
    const shield = document.querySelector('.validation-shield');
    const card = document.getElementById('validationCard');
    
    if (!solution || solution.totalTravelTimeMinutes === -1) {
        resetValidationCard();
        return;
    }
    
    const bfSolution = solveBellmanFord(State.graph, State.sourceId, State.targetId, State.emergencyMode);
    
    shield.classList.remove('valid', 'invalid');
    card.style.background = '#f8fafc';
    card.style.borderColor = 'var(--bg-card-border)';
    
    if (bfSolution.hasNegativeCycle) {
        validationTitle.textContent = "Deteksi Jalan Rusak Parah!";
        validationTitle.style.color = 'var(--color-danger)';
        validationDesc.textContent = "Validator mendeteksi adanya siklus waktu negatif di peta kota.";
        shield.classList.add('invalid');
        shield.setAttribute('data-lucide', 'alert-triangle');
        card.style.background = '#fff1f2';
        card.style.borderColor = '#fecdd3';
    } else {
        const epsilon = 0.0001;
        const delta = Math.abs(solution.totalTravelTimeMinutes - bfSolution.dist);
        
        if (delta < epsilon) {
            validationTitle.textContent = "Terbukti Rute Tercepat!";
            validationTitle.style.color = 'var(--color-success)';
            validationDesc.textContent = `Validator Bellman-Ford membuktikan rute ini 100% optimal (${solution.totalTravelTimeMinutes.toFixed(2)} menit).`;
            shield.classList.add('valid');
            shield.setAttribute('data-lucide', 'shield-check');
            card.style.background = '#f0fdf4';
            card.style.borderColor = '#bbf7d0';
        } else {
            validationTitle.textContent = "Rute Kurang Optimal!";
            validationTitle.style.color = 'var(--color-warning)';
            validationDesc.textContent = `Metode pintar memiliki selisih waktu sebesar ${delta.toFixed(3)} menit dibandingkan rute matematika murni.`;
            shield.classList.add('invalid');
            shield.setAttribute('data-lucide', 'alert-octagon');
            card.style.background = '#fffbeb';
            card.style.borderColor = '#fde68a';
        }
    }
    lucide.createIcons({ attrs: { class: ['validation-shield'] } });
}

function resetValidationCard() {
    const validationTitle = document.getElementById('validationTitle');
    const validationDesc = document.getElementById('validationDesc');
    const shield = document.querySelector('.validation-shield');
    const card = document.getElementById('validationCard');
    
    validationTitle.textContent = "Belum Diuji";
    validationTitle.style.color = 'inherit';
    validationDesc.textContent = "Validator otomatis mencocokkan rute yang dicari untuk menjamin akurasi rute.";
    card.style.background = '#f8fafc';
    card.style.borderColor = 'var(--bg-card-border)';
    
    shield.className = "validation-shield";
    shield.setAttribute('data-lucide', 'shield');
    lucide.createIcons();
}

function runComparativeAnalysis() {
    const tableBody = document.getElementById('comparisonTableBody');
    tableBody.innerHTML = '';
    
    const solversList = [
        { name: "Dijkstra Standar", id: "Dijkstra" },
        { name: "A* Search (Pintar)", id: "AStar" },
        { name: "Bidirectional Dijkstra", id: "BidirectionalDijkstra" },
        { name: "Robust (Aman)", id: "Robust" }
    ];
    
    const baseGenerator = DijkstraGenerator(State.graph, State.sourceId, State.targetId, State.emergencyMode);
    const baseResult = solveInstant(baseGenerator);
    
    if (!baseResult || baseResult.totalTravelTimeMinutes === -1) {
        tableBody.innerHTML = `<tr><td colspan="4" class="empty-table-msg">Tujuan tidak terjangkau. Tidak dapat membandingkan metode.</td></tr>`;
        return;
    }
    
    solversList.forEach(item => {
        let result = null;
        let tStart = performance.now();
        
        if (item.id === "Dijkstra") {
            result = baseResult;
        } else if (item.id === "AStar") {
            const gen = AStarGenerator(State.graph, State.sourceId, State.targetId, 100.0, State.emergencyMode);
            result = solveInstant(gen);
        } else if (item.id === "BidirectionalDijkstra") {
            const gen = BidirectionalDijkstraGenerator(State.graph, State.sourceId, State.targetId, State.emergencyMode);
            result = solveInstant(gen);
        } else if (item.id === "Robust") {
            const gen = RobustRouteGenerator(State.graph, State.sourceId, State.targetId, State.emergencyMode, State.lambda);
            result = solveInstant(gen);
        }
        
        const tEnd = performance.now();
        const costMs = tEnd - tStart;
        
        const isOptimal = Math.abs(result.totalTravelTimeMinutes - baseResult.totalTravelTimeMinutes) < 0.001;
        const timeText = result.totalTravelTimeMinutes !== -1 ? result.totalTravelTimeMinutes.toFixed(2) + ' mnt' : 'GAGAL';
        
        const baseExp = baseResult.expandedNodes;
        const itemExp = result.expandedNodes || 1;
        const speedupPercent = ((baseExp - itemExp) / baseExp * 100).toFixed(0);
        
        let speedupText = speedupPercent > 0 ? `Hemat ${speedupPercent}% jalan` : `Mencakup ${Math.abs(speedupPercent)}% jalan`;
        if (item.id === "Dijkstra") speedupText = "Titik Acuan Utama";
        if (item.id === "Robust") speedupText = "Fokus Keselamatan";
        
        const row = document.createElement('tr');
        row.innerHTML = `
            <td><strong>${item.name}</strong></td>
            <td><span style="color:${isOptimal ? 'inherit' : 'var(--color-warning)'}">${timeText}</span></td>
            <td><strong>${result.expandedNodes || '--'} titik</strong></td>
            <td><strong style="color:${speedupPercent > 0 ? 'var(--color-hospital)' : 'inherit'}">${speedupText}</strong></td>
        `;
        tableBody.appendChild(row);
    });
}

// ============================================================================
// 10. LOGGER HELPER
// ============================================================================

function logToConsole(message, type = 'system') {
    const consoleLog = document.getElementById('consoleLog');
    const entry = document.createElement('div');
    entry.className = `log-entry ${type}`;
    
    const timestamp = new Date().toLocaleTimeString([], { hour12: false });
    entry.textContent = `[${timestamp}] ${message}`;
    
    consoleLog.appendChild(entry);
    consoleLog.scrollTop = consoleLog.scrollHeight;
}
