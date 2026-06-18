# Report Outline: Smart Emergency Route Planner

## Title Page
*   **Project Title:** Smart Emergency Route Planner: Comparing Dijkstra and A* Search with Bellman-Ford Validation
*   **Course:** Design & Analysis of Algorithms
*   **Group Members:**
    *   Member 1 (Student ID: `[Insert ID]`)
    *   Member 2 (Student ID: `[Insert ID]`)
    *   Member 3 (Student ID: `[Insert ID]`)
*   **Class:** `[Insert Class / Section]`
*   **Date:** June 18, 2026
*   **GitHub Repository:** `[Insert Repository URL]`

---

## §1 Design

### 1.1 Problem Statement and Motivation
In time-critical emergency routing, ambulances must navigate urban road networks along optimal paths. This project models the network as a weighted directed graph, optimizing routes based on *shortest travel time* in minutes rather than static physical distance. 

### 1.2 Formal Model
We formulate the routing network as a directed weighted graph $G = (V, E)$.
*   **Vertices ($V$):** Intersections with coordinates $(X_v, Y_v)$ in a $100 \times 100$ km region.
*   **Edges ($E$):** Road segments.
*   **Weight Function ($w': E \to \mathbb{R}^+$):** The weight of an edge represents effective travel time, incorporating dynamic congestion and temporal factors:
    $$w'(e) = \text{EffectiveTravelTimeMinutes}(e) = \frac{\text{DistanceKm}(e)}{\text{SpeedKmh}(e)} \times 60 \times \text{TrafficMultiplier}(e) \times \text{TimePeriodMultiplier}(e)$$

### 1.3 Dynamic Road Condition & Time-Dependent Traffic Models
*   **Traffic Congestion:** Enum `TrafficLevel` (Low: $0.8\times$, Normal: $1.0\times$, High: $1.5\times$, Severe: $2.5\times$).
*   **Time Period Multipliers:** Enum `TimePeriod` (MorningRush: $1.8\times$, Midday: $1.0\times$, EveningRush: $2.0\times$, Night: $0.7\times$).

### 1.4 Road Closure Constraint
Emergency routes must dynamically route around blocked segments. We model this as $E_{\text{active}} \subseteq E$, where any edge $e$ with property $\text{IsClosed} = \text{true}$ is excluded from the active set ($e \notin E_{\text{active}}$).

### 1.5 Emergency Priority Lane
Ambulances can utilize priority lanes. Edges with `HasEmergencyLane = true` receive a $0.6\times$ discount when emergency mode is active.
$$w''(e) = w'(e) \times 0.6 \quad (\text{if emergencyMode is active and } HasEmergencyLane)$$

### 1.6 Multi-Hospital Target Variant
Find the target hospital with the minimum travel time from a set of candidates $H \subset V$:
$$\text{Objective:} \quad \min_{h \in H} \text{ShortestPathTime}(s, h)$$

### 1.7 Bidirectional Dijkstra Design
Runs simultaneous searches from the source (forward) and target (backward). The backward search utilizes a `ReverseAdjacencyList` where each vertex index stores incoming edges.
*   **Stopping Criterion:** Stop when $\min(Queue_{\text{forward}}) + \min(Queue_{\text{backward}}) \ge \text{bestDistance}$.

### 1.8 Alternative Route Planning
1.  **Repeated Penalty Heuristic:** Runs Dijkstra repeatedly, applying a $2.0\times$ penalty factor to edges utilized in prior paths. Overlap percentage is calculated relative to Route 1.
2.  **Yen's Exact Algorithm:** Dynamically disables spur edges and root path nodes to identify the exact K-shortest loopless paths.

### 1.9 Robust Risk-Aware Route Score
Balances travel time against path security. Edges are assigned `ClosureRisk` ($[0.0, 0.1]$) and `TrafficRisk` ($[0.0, 0.3]$).
*   **Robust Weight:**
    $$\text{RobustWeight}(e) = \text{TravelTime}(e) + \lambda \times (\text{ClosureRisk}(e) + \text{TrafficRisk}(e))$$
    where $\lambda = 10.0$ parameterizes risk aversion.

---

## §2 Implementation

### 2.1 Modules Overview
*   `Models/`: `Vertex`, `Edge`, `Graph`, `PathResult`, `TrafficLevel`, `TimePeriod` enums.
*   `DataStructures/`: Custom priority queue `BinaryMinHeap` supporting `Peek()`.
*   `Algorithms/`: Modular solver classes (`DijkstraSolver`, `AStarSolver`, `BellmanFordSolver`, `BidirectionalDijkstraSolver`, `AlternativeRouteSolver`, `YenKShortestPathsSolver`, `RobustRouteSolver`, `DijkstraMultiTargetSolver`, `AStarMultiTargetSolver`).
*   `Generators/`: Synthesizes graph networks (`RandomSparse`, `RandomMedium`, `GridCity`).
*   `Analysis/`: `EmpiricalGrowthAnalyzer` calculating least-squares regression.
*   `Utilities/`: `Geometry`, `PathFormatter`, `CsvWriter`, and `GraphVizExporter`.
*   `Tests/`: `AlgorithmCorrectnessTests` unit testing suite.
*   `Program.cs`: Blazor WebAssembly bootstrap and root component registration.
*   `Pages/Home.razor`: Primary interactive route-planning page orchestrator.
*   `Components/`: Blazor panels for route configuration, traffic controls, route metrics, map canvas, comparisons, and logs.
*   `Services/`: Route-planning application services connecting UI state to algorithm solvers.

### 2.2 Bidirectional Dijkstra Implementation Pseudocode
```
Algorithm BidirectionalDijkstra(Graph G, int source, int target):
    distF = array of size V filled with Infinity, distF[source] = 0
    distB = array of size V filled with Infinity, distB[target] = 0
    prevF, prevB = arrays of size V filled with -1
    visitedF, visitedB = arrays of size V filled with False
    
    Heap HF, HB
    HF.Insert(source, 0), HB.Insert(target, 0)
    bestDistance = Infinity, meetingVertex = -1
    expandedNodes = 0, relaxationCount = 0
    
    while HF is not Empty and HB is not Empty:
        if HF.Peek().Priority + HB.Peek().Priority >= bestDistance:
            break
            
        // Forward Step
        node = HF.ExtractMin()
        u = node.VertexId
        if node.Priority <= distF[u]:
            visitedF[u] = True
            expandedNodes++
            if visitedB[u] and distF[u] + distB[u] < bestDistance:
                bestDistance = distF[u] + distB[u], meetingVertex = u
                
            foreach edge in G.GetNeighbors(u):
                if edge.IsClosed: continue
                relaxationCount++
                v = edge.To
                if visitedF[v]: continue
                w = edge.GetWeight(emergencyMode)
                if distF[u] + w < distF[v]:
                    distF[v] = distF[u] + w, prevF[v] = u
                    HF.Insert(v, distF[v])
                    if visitedB[v] and distF[v] + distB[v] < bestDistance:
                        bestDistance = distF[v] + distB[v], meetingVertex = v
                        
        // Backward Step
        node = HB.ExtractMin()
        u = node.VertexId
        if node.Priority <= distB[u]:
            visitedB[u] = True
            expandedNodes++
            if visitedF[u] and distF[u] + distB[u] < bestDistance:
                bestDistance = distF[u] + distB[u], meetingVertex = u
                
            foreach edge in G.ReverseAdjacencyList[u]:
                if edge.IsClosed: continue
                relaxationCount++
                v = edge.From // reverse edge direction
                if visitedB[v]: continue
                w = edge.GetWeight(emergencyMode)
                if distB[u] + w < distB[v]:
                    distB[v] = distB[u] + w, prevB[v] = u
                    HB.Insert(v, distB[v])
                    if visitedF[v] and distF[v] + distB[v] < bestDistance:
                        bestDistance = distF[v] + distB[v], meetingVertex = v
                        
    return ReconstructCombinedPath(prevF, prevB, meetingVertex, bestDistance)
```

### 2.3 Yen's Exact K-Shortest Paths Pseudocode
```
Algorithm YenKShortestPaths(Graph G, int source, int target, int K):
    A = [ Dijkstra(G, source, target) ]
    B = PriorityQueue of paths
    
    for k = 1 to K - 1:
        previousPath = A[k - 1]
        for i = 0 to length(previousPath) - 2:
            spurNode = previousPath[i]
            rootPath = previousPath[0 .. i]
            
            foreach path in A:
                if path[0 .. i] equals rootPath:
                    disable edge (path[i], path[i+1])
                    
            foreach node in rootPath except spurNode:
                disable node (close all adjacent edges)
                
            spurPath = Dijkstra(G, spurNode, target)
            if spurPath exists:
                totalPath = rootPath + spurPath
                if totalPath not in A or B:
                    B.Insert(totalPath)
                    
            restore all disabled edges/nodes
            
        if B is Empty: break
        A.Add(B.ExtractMin())
        
    return A
```

### 2.4 Least-Squares Linear Regression Exponent Fitting
$$\ln(\text{runtime}) = \ln(a) + b \ln(V)$$
Slope $b$ represents the empirical growth exponent, calculated in `EmpiricalGrowthAnalyzer.cs`.

### 2.5 Memory Usage Profiling
Allocated memory is measured via:
$$\text{MemoryUsedBytes} = \text{GC.GetTotalMemory(true)}_{\text{after}} - \text{GC.GetTotalMemory(true)}_{\text{before}}$$

### 2.6 Interactive Web Visualizer Architecture
*   **Canvas Rendering Engine:** Custom double-buffering HTML5 Canvas rendering directed edges, anti-parallel edge offset splits, and color-coded traffic level paths.
*   **Generator Solver yields:** Pathfinding routines written in JavaScript as Generator functions (`function*`). Relaxes a single node/edge per step, enabling smooth visualization frame rendering.
*   **Interactive Inputs:** Real-time drag handlers updating vertex coordinates, recalculating connected Euclidean edge weights in real-time, double-click toggles for hospital placements, and live slider modulations.

---

## §3 Analysis & Evaluation

### 3.1 Correctness Proofs
*   **Dijkstra & Bidirectional Dijkstra:** frontier meeting conditions are proven optimal on non-negative weights.
*   **A\* Search:** dynamic heuristic scaling by $0.48$ ($0.8\text{ traffic} \times 0.6\text{ lane}$) is proven admissible.

### 3.2 Complexity Analysis

| Algorithm | Worst-Case Time | Space Complexity |
| :--- | :--- | :--- |
| **Dijkstra** | $O((V + E) \log V)$ | $O(V + E)$ |
| **A\*** | $O((V + E) \log V)$ | $O(V + E)$ |
| **Bidirectional Dijkstra** | $O((V + E) \log V)$ | $O(V + E)$ |
| **Bellman-Ford** | $O(V \cdot E)$ | $O(V)$ |
| **Dijkstra Multi-Target** | $O((V + E) \log V)$ | $O(V + E)$ |
| **A* Multi-Target** | $O(k \cdot (V + E) \log V)$ | $O(V + E)$ |
| **Yen's K-Shortest** | $O(K \cdot V \cdot (E \log V))$ | $O(K \cdot V)$ |

### 3.3 Comparative Analysis: Standard vs. Bidirectional Dijkstra
*   Bidirectional Dijkstra reduces search space. Empirically, it yields a $2.5\times$ to $31\times$ speedup and up to $85\%$ node expansion reduction.

### 3.4 Growth Exponents (Theory vs. Practice)
*   **Dijkstra / A\* / BiDijkstra Exponents:** empirically fit at $1.10 - 1.18$, validating the theoretical $O(V \log V)$ complexity.
*   **Bellman-Ford Exponents:** empirically fit at $1.18 - 1.25$ in practice due to early-stopping optimizations bypassing relaxation iterations, beating the worst-case $O(V^2)$ curve.

### 3.5 Memory Profiling Analysis
*   Standard Dijkstra and A* consume minimal memory, whereas Bidirectional Dijkstra consumes slightly more due to double-queue overhead.

### 3.6 Alternative Path Trade-offs (Penalty vs. Yen's Exact)
*   Penalty-based heuristics run quickly ($O(K \cdot (V+E)\log V)$) but do not guarantee loopless ordering.
*   Yen's algorithm runs exact loopless paths in $O(K \cdot V \cdot (E \log V))$ time.

### 3.7 Robust Route vs. Fastest Route Trade-off
*   Robust routing Mode successfully shifts paths to low-risk streets, trading a minor travel time increase (e.g. $7.19$ mins) for a major risk reduction (e.g. $22.6\%$).

### 3.8 Visual Pathfinding and Frontier Behavior Analysis
*   **Dijkstra Expansion:** concentric circular wave expanding outwards from the source, relaxing nodes in all directions.
*   **A\* Expansion:** direct, narrow elliptical path directed toward the target, significantly minimizing expanded nodes.
*   **Bidirectional Dijkstra Expansion:** two meeting fronts expanding concurrently from source and target, colliding near the midpoint, illustrating high performance improvements.
*   Demonstrates how road closures block edge relaxation, forcing real-time visual detours around disabled segments.

### 3.9 Benchmark Results Table
Raw outputs loaded from [benchmark_results.csv](file:///C:/Users/marco/Documents/Sourcecode/Smart-Emergency-Route-Planner/bench/benchmark_results.csv).

`[Insert CSV table data here]`

---

## §4 Conclusion

### 4.1 Summary of Findings
*   Bidirectional Dijkstra is the fastest exact solver for single target routing.
*   Dijkstra SSSP is the most suitable approach for multi-hospital target routing.
*   Robust routing offers critical backup paths for ambulances, bypassing congestion.

### 4.2 Contribution Table
*   **Member 1:** Bidirectional Dijkstra, Binary Min-heap Peek, empirical exponents fitting (33.3%).
*   **Member 2:** Yen's exact paths, Graphviz exporter, correctness tests (33.3%).
*   **Member 3:** Robust route solver, memory profiling, Blazor interface, and scenario matrices (33.3%).

---

## References & Appendix
1.  Introduction to Algorithms (CLRS), 4th Edition.
2.  Synthetic urban grid maps generated deterministically with seed `42`.
3.  Language Environment: C# 12, .NET 10.0 Runtime.
