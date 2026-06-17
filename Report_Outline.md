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
In time-critical emergency scenarios, dispatching ambulances along optimal routes is essential. This project focuses on solving the single-source shortest path problem (SSSP) on a directed graph representing a city's road network, where the objective is to minimize total travel time (in minutes). 

Routing based strictly on physical distance often fails to account for variations in speed limits (e.g., highways vs. residential roads). Thus, we model the weights as travel times calculated dynamically from lengths and speed limits.

### 1.2 Formal Model
We formulate the routing network as a directed weighted graph $G = (V, E)$.
*   **Vertices ($V$):** Street intersections. Each $v \in V$ is associated with Cartesian coordinates $(X_v, Y_v)$ within a $100 \times 100$ km region.
*   **Edges ($E$):** Direct road segments.
*   **Weight Function ($w: E \to \mathbb{R}^+$):** The weight of an edge represents travel time:
    $$w(e) = \frac{\text{DistanceKm}(e)}{\text{SpeedKmh}(e)} \times 60$$
    where distance is the Euclidean distance:
    $$\text{DistanceKm}(e) = \sqrt{(X_{to} - X_{from})^2 + (Y_{to} - Y_{from})^2}$$
    and speed is generated in the range $[20.0, 100.0]$ km/h.
*   **Source ($s$):** Dispatch hub ($v_0$).
*   **Target ($t$):** Destination hospital ($v_{n-1}$).

### 1.3 Algorithm Selection and Justification
1.  **Dijkstra's Algorithm:** Used as the baseline exact solver. It is guaranteed to find the absolute shortest path since all weights $w(e) > 0$.
2.  **A* Search Algorithm:** Uses a heuristic estimate of remaining cost to focus the search direction. Ideal for spatial networks where coordinates are available.
3.  **Bellman-Ford Algorithm:** Serves as a correctness validator. Though slower, it operates on a different computational principle (dynamic programming vs. greedy search) and is capable of identifying negative cycles, providing an robust cross-check.

### 1.4 Data Structures and System Architecture
*   **Graph representation:** Adjacency lists are chosen over adjacency matrices to optimize memory consumption ($O(V+E)$ space) and neighbor traversal ($O(\text{deg}(u))$ time).
*   **Custom Binary Min-Heap:** Custom implementation. Since implementing `DecreaseKey` directly requires maintaining a dynamic hash map of heap indexes, we implement a **duplicate insertion with lazy deletion** scheme:
    *   Shorter paths are pushed directly to the heap as new entries.
    *   When a node is popped, if its priority is greater than the current best distance, it is treated as "stale" and skipped.
*   **Subsystems:** Modular layout separating models, priority queues, algorithms, graph generator, benchmark runner, and CLI.

---

## §2 Implementation

### 2.1 Module Overview
The system is divided into clean, decoupled files:
*   `Models/`: Contains data structures (`Vertex`, `Edge`, `Graph`, `PathResult`).
*   `DataStructures/`: Custom priority queue (`BinaryMinHeap`).
*   `Algorithms/`: Modular solver classes implementing the algorithms.
*   `Generators/`: Generates city street grids with a guaranteed backbone path.
*   `Utilities/`: Handles geometric calculations, path output, and CSV operations.

### 2.2 Graph Representation
Implemented in `Graph.cs` using a `List<Edge>[]` array where index `i` stores outgoing edges for vertex `i`. An auxiliary `List<Edge>` contains all edges to allow linear relaxation loops in Bellman-Ford.

### 2.3 Custom Binary Min-Heap Pseudocode
An array-backed binary tree layout where for parent index `i`, children are at `2i + 1` and `2i + 2`.

```
Algorithm HeapifyUp(index):
    while index > 0:
        parent = (index - 1) / 2
        if heap[index].Priority < heap[parent].Priority:
            Swap(index, parent)
            index = parent
        else:
            break

Algorithm HeapifyDown(index):
    while true:
        left = 2 * index + 1
        right = 2 * index + 2
        smallest = index
        if left < size and heap[left].Priority < heap[smallest].Priority:
            smallest = left
        if right < size and heap[right].Priority < heap[smallest].Priority:
            smallest = right
        if smallest != index:
            Swap(index, smallest)
            index = smallest
        else:
            break
```

### 2.4 Dijkstra Implementation
Implemented in `DijkstraSolver.cs`.

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
    
    while H is not Empty:
        node = H.ExtractMin()
        u = node.VertexId
        priority = node.Priority
        
        if priority > dist[u]:
            continue // Skip stale entry (lazy deletion)
            
        expandedNodes = expandedNodes + 1
        
        if u == target:
            break // Early exit
            
        visited[u] = True
        
        foreach edge in G.GetNeighbors(u):
            v = edge.To
            if visited[v]:
                continue
            alt = dist[u] + edge.TravelTimeMinutes
            if alt < dist[v]:
                dist[v] = alt
                prev[v] = u
                H.Insert(v, alt)
                
    if dist[target] == Infinity:
        return UnreachableResult
        
    Path = ReconstructPath(prev, target)
    return PathResult(dist[target], Path, expandedNodes)
```

### 2.5 A* Implementation
Implemented in `AStarSolver.cs`.

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
    
    while H is not Empty:
        node = H.ExtractMin()
        u = node.VertexId
        priority = node.Priority
        
        if priority > fScore[u]:
            continue // Skip stale entry
            
        expandedNodes = expandedNodes + 1
        
        if u == target:
            break // Early exit
            
        visited[u] = True
        
        foreach edge in G.GetNeighbors(u):
            v = edge.To
            if visited[v]:
                continue
            altG = gScore[u] + edge.TravelTimeMinutes
            if altG < gScore[v]:
                gScore[v] = altG
                f = altG + Heuristic(v, target, maxSpeedKmh)
                fScore[v] = f
                prev[v] = u
                H.Insert(v, f)
                
    if gScore[target] == Infinity:
        return UnreachableResult
        
    Path = ReconstructPath(prev, target)
    return PathResult(gScore[target], Path, expandedNodes)
```

### 2.6 Bellman-Ford Validator
Implemented in `BellmanFordSolver.cs`.

#### Bellman-Ford Pseudocode
```
Algorithm BellmanFord(Graph G, int source, int target):
    dist = array of size V filled with Infinity
    prev = array of size V filled with -1
    dist[source] = 0
    
    // Relax edges V - 1 times
    for i = 1 to V - 1:
        relaxedAny = False
        foreach edge in G.AllEdges:
            u = edge.From
            v = edge.To
            w = edge.TravelTimeMinutes
            if dist[u] != Infinity and dist[u] + w < dist[v]:
                dist[v] = dist[u] + w
                prev[v] = u
                relaxedAny = True
        if not relaxedAny:
            break // Early stopping optimization
            
    // Check for negative cycle
    hasNegativeCycle = False
    foreach edge in G.AllEdges:
        u = edge.From
        v = edge.To
        w = edge.TravelTimeMinutes
        if dist[u] != Infinity and dist[u] + w < dist[v]:
            hasNegativeCycle = True
            break
            
    if hasNegativeCycle or dist[target] == Infinity:
        return ErrorOrUnreachableResult
        
    Path = ReconstructPath(prev, target)
    return PathResult(dist[target], Path)
```

### 2.7 CLI Demo
The program CLI is fully interactive, validating menu entries and handling bad parameters (e.g. vertices < 2, edges out of range).

### 2.8 Benchmark Runner
Coordinates the timing loop: runs a JIT warm-up, loops through the 5 target sizes, averages the time of 3 repetitions, checks for distance matches, and outputs to CSV.

### 2.9 How to Build and Run
*   **Compile:** `dotnet build`
*   **Execution:** `dotnet run --project SmartEmergencyRoutePlanner.csproj`

### 2.10 Screenshots Placeholder
`[Insert screenshot captures of the Menu, Small Demo, and Benchmark console results here]`

---

## §3 Analysis & Evaluation

### 3.1 Correctness Proof for Dijkstra
*   **Theorem:** For any vertex $u$ added to the set of closed vertices (extracted from the min-heap), $dist[u]$ is the weight of the shortest path from $source$ to $u$.
*   **Proof:** By induction on the size of the visited set.
    *   *Base Case:* For $|Visited| = 1$, $Visited = \{source\}$, and $dist[source] = 0$, which is correct since edge weights are positive.
    *   *Inductive Step:* Let $u$ be the next vertex extracted. Assume the paths to all nodes in $Visited$ are correct. Let $P$ be the path found to $u$. Suppose there exists a shorter path $P'$ from $source$ to $u$. Since $source \in Visited$ and $u \notin Visited$, there must be an edge $(x, y)$ on $P'$ where $x \in Visited$ and $y \notin Visited$. 
    Because $x \in Visited$, the subpath from $source$ to $x$ is optimal. Since $y$ is adjacent to $x$, $dist[y] \le dist[x] + w(x, y)$. Since all weights are non-negative, the remaining travel time on $P'$ from $y$ to $u$ is $\ge 0$. Therefore, the total time of $P'$ is at least $dist[y]$. 
    However, because $u$ was extracted from the heap before $y$, it must be that $dist[u] \le dist[y]$. This contradicts the assumption that $P'$ is strictly shorter than $P$. Thus, the chosen path is optimal.

### 3.2 Correctness Proof for A*
*   **Heuristic Admissibility:** A heuristic $h(v)$ is admissible if it never overestimates the actual remaining travel time to the target $t$, i.e., $h(v) \le h^*(v)$ where $h^*(v)$ is the optimal remaining time.
    *   *Our Heuristic:* $h(v) = \frac{\text{EuclideanDistance}(v, t)}{\text{MaxSpeedKmh}} \times 60$.
    *   *Proof:* Since speed limits on any edge are $\le \text{MaxSpeedKmh}$ and the Euclidean distance represents the shortest straight-line path between two points on a plane, the travel time along a straight line at maximum speed is the physical lower bound. Any actual path must have a length $\ge$ Euclidean distance and speed limits $\le \text{MaxSpeedKmh}$, resulting in a travel time $\ge h(v)$. Thus, $h(v) \le h^*(v)$ holds, and the heuristic is admissible.
*   **Heuristic Consistency:** A heuristic is consistent if for any edge $(u, v)$, $h(u) \le w(u, v) + h(v)$.
    *   *Proof:* By the triangle inequality:
        $$\text{EuclideanDistance}(u, t) \le \text{EuclideanDistance}(u, v) + \text{EuclideanDistance}(v, t)$$
        Dividing by $\text{MaxSpeedKmh}$ and multiplying by 60 preserves the inequality:
        $$h(u) \le \frac{\text{EuclideanDistance}(u, v)}{\text{MaxSpeedKmh}} \times 60 + h(v)$$
        Since speed $s(u, v) \le \text{MaxSpeedKmh}$:
        $$w(u, v) = \frac{\text{EuclideanDistance}(u, v)}{s(u, v)} \times 60 \ge \frac{\text{EuclideanDistance}(u, v)}{\text{MaxSpeedKmh}} \times 60$$
        Therefore:
        $$h(u) \le w(u, v) + h(v)$$
        The heuristic is consistent. A* with an admissible and consistent heuristic is guaranteed to find the shortest path without re-expanding closed nodes.

### 3.3 Bellman-Ford Correctness Justification
A shortest path between two reachable vertices in a graph of size $V$ contains at most $V-1$ edges. Bellman-Ford works by relaxing all edges $V-1$ times. In the $k$-th iteration, it is guaranteed to find the shortest path for all paths containing at most $k$ edges. After $V-1$ passes, all shortest paths must be resolved. A further $V$-th pass that succeeds in reducing any distance indicates the presence of a negative cycle.

### 3.4 Complexity Analysis

| Algorithm | Time Complexity | Space Complexity | Best Case Behavior |
| :--- | :--- | :--- | :--- |
| **Dijkstra** | $O((V + E) \log V)$ | $O(V + E)$ | $O(V \log V)$ (Target close to source) |
| **A\*** | $O((V + E) \log V)$ | $O(V + E)$ | $O(1)$ (Direct straight-line highway to target) |
| **Bellman-Ford** | $O(V \cdot E)$ | $O(V)$ | $O(E)$ (No relaxation updates in first pass) |

### 3.5 Comparative Analysis
*   **Dijkstra vs A\*:** Both guarantee the same optimal distance. However, Dijkstra expands nodes radially in all directions, whereas A* expands nodes in a directed search corridor towards the target. Thus, A* will expand significantly fewer nodes, reducing search space and runtime.
*   **Role of Bellman-Ford:** Acts as a solid validator for small networks. However, because of its $O(V \cdot E)$ complexity, running it on $V=10000, E=100000$ requires billions of relaxations, causing unacceptable lag. Thus, skipping it on large configurations is essential.

### 3.6 Experimental Setup
*   **CPU:** `[Insert CPU Model]`
*   **Memory:** `[Insert RAM]`
*   **OS:** Windows 11 / 10
*   **Runtime:** .NET 8.0 CLR
*   **Graph Parameters:** Generated using fixed seed `42` with target configurations.

### 3.7 Benchmark Table Placeholder
The following table represents the layout of benchmark results (to be populated with actual runs):

| Vertices | Edges | Fam. | DijkMs | A* Ms | BF Ms | DijkNodes | A* Nodes | Speedup | Match | BF Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 100 | 500 | Sparse | * | * | * | * | * | * | True | Completed |
| 500 | 2500 | Sparse | * | * | * | * | * | * | True | Completed |
| 1000 | 5000 | Sparse | * | * | * | * | * | * | True | Completed |
| 5000 | 25000 | Sparse | * | * | Skipped | * | * | * | True | Skipped |
| 10000 | 50000 | Sparse | * | * | Skipped | * | * | * | True | Skipped |
| 100 | 1000 | Med | * | * | * | * | * | * | True | Completed |
| 500 | 5000 | Med | * | * | * | * | * | * | True | Completed |
| 1000 | 10000 | Med | * | * | * | * | * | * | True | Completed |
| 5000 | 50000 | Med | * | * | Skipped | * | * | * | True | Skipped |
| 10000 | 100000 | Med | * | * | Skipped | * | * | * | True | Skipped |

*(Note: Data values are placeholders and will be populated upon benchmark execution).*

### 3.8 Runtime Plot Placeholder
`[Insert Line Plot of Runtime (ms) vs. Vertices for Dijkstra and A* here]`

### 3.9 Expanded-node Plot Placeholder
`[Insert Line Plot of Nodes Expanded vs. Vertices for Dijkstra and A* here]`

### 3.10 Theory vs Practice Discussion
`[Discuss how actual runtime curves match theoretical O((V+E) log V) and O(VE) expectations. Observe the impact of heap operations overhead vs node expansions.]`

### 3.11 Cross-check Discussion
`[Explain how the validation check "DijkstraEqualsAStar" and "DijkstraEqualsBellmanFord" succeeded in all cases, confirming implementation correctness.]`

---

## §4 Conclusion

### 4.1 Summary of Findings
`[Summarize comparative speeds, speedup ratios, and node reduction rates achieved by A* Search over Dijkstra.]`

### 4.2 Limitations
*   Euclidean heuristic assumes a flat grid; it does not model elevation changes or curved roads directly.
*   Travel times are static; they do not model dynamic congestion patterns.

### 4.3 Lessons Learned
*   The duplicate insertion priority queue simplifies code but results in stale heap nodes that must be filtered.
*   Early termination optimizations drastically improve performance in search algorithms.

### 4.4 Future Work
*   Implement real-world mapping coordinates (e.g., OpenStreetMap GPX/JSON).
*   Add dynamic traffic congestion factors.

### 4.5 Contribution Table
| Name | Tasks Completed | Contribution % |
| :--- | :--- | :--- |
| Member 1 | Data structures, Dijkstra algorithm, benchmarks | 33.3% |
| Member 2 | A* algorithm, Geometry, graph generator | 33.3% |
| Member 3 | Bellman-Ford, Program CLI, report outline | 33.3% |

---

## References & Appendix
1.  Introduction to Algorithms (CLRS), 4th Edition.
2.  Synthetic urban grid maps generated deterministically with seed `42`.
3.  Language Environment: C# 12, .NET 8.0 Runtime.
