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
In emergency medical services, route planning is critical. Ambulances must navigate urban street networks efficiently. This project models the city road network as a weighted directed graph, optimizing routes based on *shortest travel time* in minutes rather than static physical distance. 

### 1.2 Formal Model
We formulate the routing network as a directed weighted graph $G = (V, E)$.
*   **Vertices ($V$):** Street intersections. Each $v \in V$ is associated with Cartesian coordinates $(X_v, Y_v)$ in a $100 \times 100$ km region.
*   **Edges ($E$):** Road segments.
*   **Weight Function ($w': E \to \mathbb{R}^+$):** The weight of an edge represents effective travel time:
    $$w'(e) = \text{EffectiveTravelTimeMinutes}(e) = \frac{\text{DistanceKm}(e)}{\text{SpeedKmh}(e)} \times 60 \times \text{TrafficMultiplier}(e)$$
*   **Source ($s$):** Dispatch hub ($v_0$).
*   **Target ($t$):** Destination hospital ($v_{n-1}$).

### 1.3 Dynamic Road Condition Model (Traffic Modifier)
Urban traffic is dynamic. We define an enum `TrafficLevel` (Low, Normal, High, Severe) and map it to multipliers:
*   $\text{Low} = 0.8$
*   $\text{Normal} = 1.0$
*   $\text{High} = 1.5$
*   $\text{Severe} = 2.5$
The weight function is modified dynamically: $w'(e) = w(e) \times m(e)$ where $m(e)$ is the traffic multiplier.

### 1.4 Road Closure Constraint
Emergency routes must dynamically route around blocked segments. We model this as $E_{\text{active}} \subseteq E$, where any edge $e$ with property $\text{IsClosed} = \text{true}$ is excluded from the active set ($e \notin E_{\text{active}}$) and bypassed by all solvers.

### 1.5 Multi-Hospital Target Variant
In real emergency logistics, the goal is often to find the nearest hospital among a set of candidates $H \subset V$:
$$\text{Objective:} \quad \min_{h \in H} \text{ShortestPathTime}(s, h)$$

### 1.6 Algorithm Selection and Justification
1.  **Dijkstra's Algorithm:** Baseline exact solver. Positive edge weights guarantee exactness. Running single-source Dijkstra once solves the multi-hospital problem in $O((V+E)\log V)$ time.
2.  **A* Search Algorithm:** Direct exact search utilizing spatial coordinate heuristics. The heuristic is adjusted to remain admissible under Low traffic ($0.8\times$ factor):
    $$h(v, t) = 0.8 \times \left( \frac{\text{EuclideanDistance}(v, t)}{\text{MaxSpeedKmh}} \times 60 \right)$$
3.  **Bellman-Ford Algorithm:** Dynamic programming validator. Computes paths by relaxing all edges $V-1$ times and detects negative cycles ($O(VE)$ complexity).

---

## §2 Implementation

### 2.1 Module Overview
*   `Models/`: `Vertex`, `Edge`, `Graph`, `PathResult`, `TrafficLevel` enums.
*   `DataStructures/`: Custom array-backed priority queue `BinaryMinHeap`.
*   `Algorithms/`: Modular solver classes (`DijkstraSolver`, `AStarSolver`, `BellmanFordSolver`, `DijkstraMultiTargetSolver`, `AStarMultiTargetSolver`).
*   `Generators/`: Synthesizes graph networks (`GraphFamily`: `RandomSparse`, `RandomMedium`, `GridCity`).
*   `Utilities/`: `Geometry`, `PathFormatter`, and `CsvWriter`.

### 2.2 Graph Representation
Adjacency lists are stored in `List<Edge>[]` to enable $O(1)$ neighbor lookup. All edges are collected in a master `List<Edge>` to optimize Bellman-Ford relaxation scans.

### 2.3 Custom Binary Min-Heap
An array-backed binary min-heap implemented in `BinaryMinHeap.cs`. It relies on duplicate insertion with lazy deletion: stale node extractions are filtered out by checking if `extractedPriority > dist[u]`.

### 2.4 Dijkstra Implementation
Uses `BinaryMinHeap` and ignores closed edges.

#### Dijkstra Pseudocode
```
Algorithm Dijkstra(Graph G, int source, int target):
    dist = array of size V filled with Infinity
    prev = array of size V filled with -1
    visited = array of size V filled with False
    
    dist[source] = 0
    Heap H
    H.Insert(source, 0)
    expandedNodes = 0
    relaxationCount = 0
    
    while H is not Empty:
        node = H.ExtractMin()
        u = node.VertexId
        priority = node.Priority
        
        if priority > dist[u]:
            continue
            
        expandedNodes = expandedNodes + 1
        if u == target:
            break
            
        visited[u] = True
        
        foreach edge in G.GetNeighbors(u):
            if edge.IsClosed:
                continue
            relaxationCount = relaxationCount + 1
            v = edge.To
            if visited[v]:
                continue
            alt = dist[u] + edge.EffectiveTravelTimeMinutes
            if alt < dist[v]:
                dist[v] = alt
                prev[v] = u
                H.Insert(v, alt)
                
    return PathResult(dist[target], prev, expandedNodes, relaxationCount)
```

### 2.5 A* Implementation
Incorporates spatial heuristic scaled by 0.8 for admissibility under traffic.

#### A* Pseudocode
```
Algorithm AStar(Graph G, int source, int target, double maxSpeedKmh):
    gScore = array of size V filled with Infinity
    fScore = array of size V filled with Infinity
    prev = array of size V filled with -1
    visited = array of size V filled with False
    
    gScore[source] = 0
    fScore[source] = Heuristic(source, target, maxSpeedKmh)
    
    Heap H
    H.Insert(source, fScore[source])
    expandedNodes = 0
    relaxationCount = 0
    
    while H is not Empty:
        node = H.ExtractMin()
        u = node.VertexId
        priority = node.Priority
        
        if priority > fScore[u]:
            continue
            
        expandedNodes = expandedNodes + 1
        if u == target:
            break
            
        visited[u] = True
        
        foreach edge in G.GetNeighbors(u):
            if edge.IsClosed:
                continue
            relaxationCount = relaxationCount + 1
            v = edge.To
            if visited[v]:
                continue
            altG = gScore[u] + edge.EffectiveTravelTimeMinutes
            if altG < gScore[v]:
                gScore[v] = altG
                f = altG + Heuristic(v, target, maxSpeedKmh)
                fScore[v] = f
                prev[v] = u
                H.Insert(v, f)
                
    return PathResult(gScore[target], prev, expandedNodes, relaxationCount)
```

### 2.6 Bellman-Ford Validator
Operates on active edges ($E_{\text{active}}$) to detect negative cycles.

#### Bellman-Ford Pseudocode
```
Algorithm BellmanFord(Graph G, int source, int target):
    dist = array of size V filled with Infinity
    prev = array of size V filled with -1
    dist[source] = 0
    relaxationCount = 0
    
    for i = 1 to V - 1:
        relaxedAny = False
        foreach edge in G.AllEdges:
            if edge.IsClosed:
                continue
            relaxationCount = relaxationCount + 1
            u = edge.From
            v = edge.To
            w = edge.EffectiveTravelTimeMinutes
            if dist[u] != Infinity and dist[u] + w < dist[v]:
                dist[v] = dist[u] + w
                prev[v] = u
                relaxedAny = True
        if not relaxedAny:
            break // Early stopping
            
    hasNegativeCycle = False
    foreach edge in G.AllEdges:
        if edge.IsClosed:
            continue
        relaxationCount = relaxationCount + 1
        u = edge.From
        v = edge.To
        w = edge.EffectiveTravelTimeMinutes
        if dist[u] != Infinity and dist[u] + w < dist[v]:
            hasNegativeCycle = True
            break
            
    return PathResult(dist[target], prev, hasNegativeCycle, relaxationCount)
```

### 2.7 Road Closure Module
Closed edges are skipped during Dijkstra/A*/Bellman-Ford relaxation. Random closures are generated using Fisher-Yates shuffle index sampling.

### 2.8 Traffic Modifier Module
Dynamic traffic assigns multipliers (0.8x to 2.5x) to edge travel times. All solver weights transition to `EffectiveTravelTimeMinutes`.

### 2.9 Multi-Target Hospital Mode
*   **Dijkstra SSSP Approach:** Implemented in `DijkstraMultiTargetSolver.cs`. Solves the shortest path from a single source to all vertices once, then scans the targets in $O(k)$ time.
*   **A* Multi-Run Approach:** Implemented in `AStarMultiTargetSolver.cs`. Iteratively executes the single-target A* solver to each hospital. Runtimes and node expansions are aggregated.

### 2.10 CLI Demo
Fully interactive console application validating user choices and custom sizes.

### 2.11 Benchmark Runner & CSV Writer
Runs all test cases 5 times, reporting Min, Max, and Average ms, and exports to CSV.

### 2.12 Screenshots Placeholder
`[Insert screenshot captures of the final menu and execution logs here]`

---

## §3 Analysis & Evaluation

### 3.1 Correctness Proofs
*   **Dijkstra:** Inductive proof shows that since edge weights $w'(e) > 0$, distance values finalized upon heap extraction represent the absolute shortest travel time.
*   **A\* Search:** Our traffic-scaled heuristic $h(v, t) = 0.8 \times \left( \frac{\text{EuclideanDistance}(v, t)}{\text{MaxSpeedKmh}} \times 60 \right)$ never overestimates (admissible) and satisfies triangle inequality (consistent), guaranteeing optimality.
*   **Bellman-Ford:** Relaxation bounds paths up to $V-1$ edges. Cycles are detected in the $V$-th loop.

### 3.2 Complexity Analysis

| Solver | Worst-Case Time | Best-Case Time | Space Complexity |
| :--- | :--- | :--- | :--- |
| **Dijkstra** | $O((V + E) \log V)$ | $O(V \log V)$ | $O(V + E)$ |
| **A\*** | $O((V + E) \log V)$ | $O(1)$ | $O(V + E)$ |
| **Bellman-Ford** | $O(V \cdot E)$ | $O(E)$ | $O(V)$ |
| **Dijkstra Multi-Target** | $O((V + E) \log V)$ | $O(V \log V)$ | $O(V + E)$ |
| **A* Multi-Target** | $O(k \cdot (V + E) \log V)$ | $O(k)$ | $O(V + E)$ |

### 3.3 Dijkstra vs A* Runtime & Expanded Nodes
*   A* expands significantly fewer nodes than Dijkstra because of spatial heuristic guidance.
*   This translates directly to lower runtimes, especially in spatial street grids.

### 3.4 Graph Family Comparison
*   **GridCity** structures show maximum speedups for A* since grid layout coordinates map closer to physical straight lines.
*   In **RandomSparse** layouts, the Euclidean distance heuristic is less informative due to arbitrary node placements.

### 3.5 Road Closure Scenario Analysis
*   With 5% closures, travel time increases as the solvers route around blockages.
*   In some configurations, target reachability may drop to False, which is correctly handled by the solvers.

### 3.6 Traffic Scenario Analysis
*   Applying congestion levels (0.8x to 2.5x) shifts the shortest path to bypass highly congested avenues, preferring longer distances with lower congestion.

### 3.7 Multi-Hospital Mode Analysis (Trade-Off)
*   **Dijkstra Multi-Target** is extremely efficient when target count $k$ is large, as it solves SSSP in a single run.
*   **A* Multi-Target** requires $k$ independent runs. Even if a single A* run is faster than Dijkstra, the accumulated runtimes of A* quickly exceed Dijkstra as $k$ increases, showing the theoretical trade-off.

### 3.8 Benchmark Results Table
Raw output captured from [benchmark_results.csv](file:///C:/Users/marco/Documents/Sourcecode/Smart-Emergency-Route-Planner/bench/benchmark_results.csv):

`[Insert CSV table data here]`

---

## §4 Conclusion

### 4.1 Findings Summary
*   A* is the most suitable algorithm for point-to-point single routing in city grids.
*   Dijkstra is optimal for multi-target nearest hospital selection due to SSSP single-run characteristics.
*   Bellman-Ford is valuable as a validation solver but does not scale.

### 4.2 Limitations & Future Work
*   Model dynamic traffic using real-world API data.
*   Incorporate elevation coordinates and weather variables.

### 4.3 Contribution Table
*   **Member 1:** Custom Min-heap, DijkstraSolver, DijkstraMultiTarget, benchmark statistics (33.3%).
*   **Member 2:** AStarSolver, AStarMultiTarget, GridCity generation topology (33.3%).
*   **Member 3:** BellmanFordSolver, Program CLI scenarios, README/Report (33.3%).

---

## References & Appendix
1.  Introduction to Algorithms (CLRS), 4th Edition.
2.  Synthetic urban grid maps generated deterministically with seed `42`.
3.  Language Environment: C# 12, .NET 10.0 Runtime.
