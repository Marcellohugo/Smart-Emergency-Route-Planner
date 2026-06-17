# Smart Emergency Route Planner: Comparing Dijkstra and A* Search with Bellman-Ford Validation

A high-performance C# Console Application designed to model, solve, and analyze the single-source shortest path problem (SSSP) on synthetic urban road networks. The system computes the fastest route for emergency vehicles (e.g., ambulances) from a dispatch hub to a target hospital. 

This project was built from scratch without external graph or pathfinding libraries to satisfy the academic requirements of the **Design & Analysis of Algorithms** course.

---

## 1. Problem Background & Real-World Motivation

In emergency services, every second counts. Finding the fastest route for an ambulance through an urban network can mean the difference between life and death. Traditional navigation services often rely on static pathfinding or proprietary black-box engines. 

This project models a city road network as a weighted directed graph, where edges represent road segments with varying speed limits and lengths. Instead of routing by shortest distance (which may lead through slow, congested residential streets), the planner routes by **shortest travel time** (in minutes) to simulate emergency vehicle behavior.

---

## 2. Advanced Project Features

To elevate this project beyond standard shortest-path implementations, the following advanced features are integrated:

### 2.1 Road Closure Simulation
Emergency situations often involve road closures due to accidents, flooding, or construction. The graph supports toggling edges active or inactive ($E_{\text{active}} \subseteq E$). Dijkstra, A*, and Bellman-Ford solvers dynamically bypass closed edges.
*   **Key Graph Methods:** `CloseEdge(from, to)`, `OpenEdge(from, to)`, `CloseRandomEdges(rate, seed)`, and `ResetClosures()`.

### 2.2 Traffic Condition Modifier
Urban traffic is highly dynamic. The project incorporates traffic levels (`Low`, `Normal`, `High`, `Severe`) which scale the edge traversal weights:
*   **Traffic Multipliers:** Low ($0.8\times$), Normal ($1.0\times$), High ($1.5\times$), Severe ($2.5\times$).
*   **Dynamic Weight Formula:** 
    $$\text{EffectiveTravelTimeMinutes}(e) = \text{TravelTimeMinutes}(e) \times \text{TrafficMultiplier}(e)$$
*   **Key Graph Methods:** `ApplyRandomTraffic(seed)` and `ResetTraffic()`.

### 2.3 Multi-Hospital Nearest Route Mode
In many emergencies, the ambulance does not have a fixed target hospital. Instead, it must route to the **nearest available hospital** from a list of candidates.
*   **Dijkstra Multi-Target Solver:** Executes Dijkstra once from the source to solve the SSSP to all nodes, then identifies the hospital with the shortest travel time. Highly efficient for multiple targets ($O((V+E)\log V)$ total).
*   **A\* Multi-Target Solver:** Executes A* iteratively to each target hospital. Extremely fast for a single target, but total work scales linearly with the number of hospitals ($O(k \cdot (V+E)\log V)$).

### 2.4 Multiple Graph Families
Benchmarks evaluate algorithms across two distinct structural network models:
*   **RandomSparse:** A highly randomized network layout with $E \approx 5V$.
*   **GridCity:** A structured 2D grid resembling real city planning. Nodes are connected to horizontal/vertical grid neighbors and organic diagonal avenues, providing a highly informative spatial layout for A*'s heuristic.

---

## 3. Formal Mathematical Model

The urban street network is modeled as a weighted directed graph $G = (V, E)$, defined as follows:

*   **Vertices ($V$):** A set of vertices representing street intersections, landmark locations, or routing hubs. Each vertex $v \in V$ is positioned on a 2D Cartesian plane with coordinates $(X_v, Y_v)$ in a $100 \times 100$ km city grid.
*   **Edges ($E$):** A set of directed edges representing road segments. Each edge $e = (u, v) \in E$ goes from intersection $u$ to intersection $v$.
*   **Edge Weight ($w'(e)$):** The weight of an edge represents the effective travel time in minutes, incorporating traffic conditions:
    $$w'(e) = \text{EffectiveTravelTimeMinutes}(e) = \left( \frac{\text{DistanceKm}(e)}{\text{SpeedKmh}(e)} \right) \times 60 \times \text{TrafficMultiplier}(e)$$
    Where $\text{DistanceKm}(e)$ is the Euclidean distance between $u$ and $v$ in kilometers, and $\text{SpeedKmh}(e) \in [20, 100]$ represents the road segment's speed limit in km/h.
*   **Source ($s \in V$):** The start vertex (ambulance starting point), designated as Vertex `0`.
*   **Target ($t \in V$):** The destination vertex (central hospital), designated as Vertex `V - 1`.
*   **Objective:** Find a directed path $P = (v_0, v_1, \dots, v_k)$ from $v_0 = s$ to $v_k = t$ along active edges ($E_{\text{active}}$) that minimizes the sum of effective travel times:
    $$\min \sum_{i=0}^{k-1} w'(v_i, v_{i+1})$$

---

## 4. Algorithm Overview

### Dijkstra's Algorithm
Dijkstra's algorithm finds the exact shortest path from a single source to all other vertices on a graph with non-negative weights. It runs in $O((V + E) \log V)$ time using our custom Binary Min-Heap.
*   **Invariant:** Once a vertex is extracted from the priority queue, its shortest distance from the source is finalized.
*   **Optimization:** Stops early as soon as the target vertex is extracted.

### A* Search Algorithm
A* improves upon Dijkstra by incorporating a heuristic to guide the search towards the target. It runs in $O((V + E) \log V)$ worst-case time, but expands fewer vertices in practice.
*   **Priority Metric:** $f(v) = g(v) + h(v)$
    *   $g(v)$ is the exact travel time from the source to vertex $v$.
    *   $h(v)$ is the estimated travel time from $v$ to the target.
*   **Heuristic Definition:** Straight-line Euclidean distance over maximum possible speed, scaled by the minimum traffic multiplier ($0.8\times$) to ensure admissibility:
    $$h(v, t) = 0.8 \times \left( \frac{\text{EuclideanDistance}(v, t)}{\text{MaxSpeedKmh}} \times 60 \right)$$
*   **Admissibility & Consistency:** Since speed limits are bounded by $\text{MaxSpeedKmh} = 100.0$ km/h and traffic multipliers are bounded by $\text{MinMultiplier} = 0.8$, the straight-line travel time at max speed with minimum traffic represents the absolute physical lower bound of travel time. Therefore, $h(v, t)$ never overestimates the actual remaining cost (admissible) and satisfies the triangle inequality (consistent), guaranteeing that A* finds the optimal shortest path.

### Bellman-Ford Validator
A slower dynamic programming shortest-path algorithm running in $O(V \cdot E)$ time. It relaxes all edges $V-1$ times and detects negative weight cycles.
*   **Role:** Used as an independent correctness validator for small-to-medium graphs ($V \le 1000$). For larger graphs, it is skipped to prevent severe benchmark slowdowns.

---

## 5. Key Data Structures

*   **Adjacency List:** The graph $G$ is represented using an array of edge lists `List<Edge>[]` to enable efficient $O(1)$ neighbor traversal.
*   **Custom Binary Min-Heap:** An array-based binary min-heap implemented from scratch in `BinaryMinHeap.cs`. To avoid the overhead of index-tracking for a standard `DecreaseKey` operation, we use the **duplicate insertion with lazy deletion** approach:
    *   When a shorter distance to vertex $v$ is discovered, a new node `(v, new_distance)` is inserted.
    *   Upon extraction, if the node's priority is greater than the current recorded distance of $v$, the node is discarded as a stale entry.
*   **Lookup Tables:** Auxiliary arrays for tracking distances, parent paths (for reconstruction), and visited statuses.

---

## 6. Repository Structure

```
SmartEmergencyRoutePlanner/
├── src/
│   ├── Models/
│   │   ├── Edge.cs                  # Edge representation with closures and traffic
│   │   ├── Vertex.cs                # Coordinates and naming details
│   │   ├── Graph.cs                 # Graph structure with closures/traffic methods
│   │   ├── PathResult.cs            # Solver output container with relaxation counter
│   │   └── TrafficLevel.cs          # Enum for low, normal, high, severe congestion
│   ├── DataStructures/
│   │   └── BinaryMinHeap.cs         # Custom array-backed min priority queue
│   ├── Algorithms/
│   │   ├── DijkstraSolver.cs        # Dijkstra algorithm (from scratch)
│   │   ├── AStarSolver.cs           # A* algorithm (from scratch)
│   │   ├── BellmanFordSolver.cs     # Bellman-Ford validator (from scratch)
│   │   ├── DijkstraMultiTargetSolver.cs # Single-source multi-hospital solver
│   │   └── AStarMultiTargetSolver.cs    # Multi-run A* multi-hospital solver
│   ├── Generators/
│   │   ├── GraphFamily.cs           # Enum for RandomSparse, RandomMedium, GridCity
│   │   └── CityGraphGenerator.cs    # Multi-family urban network builder
│   ├── Benchmark/
│   │   ├── BenchmarkCase.cs         # Size configuration (V, E, seed)
│   │   ├── BenchmarkResult.cs       # Metrics storage with Min/Max/Avg and checks
│   │   └── BenchmarkRunner.cs       # Coordinates 5-run evaluations
│   ├── Utilities/
│   │   ├── CsvWriter.cs             # CSV file generator
│   │   ├── PathFormatter.cs         # Path print detailed route explainer
│   │   └── Geometry.cs              # Euclidean distance calculations
│   └── Program.cs                   # Interactive 7-option CLI menu
├── bench/
│   └── benchmark_results.csv        # Output CSV containing raw benchmark results
├── docs/
│   ├── screenshots/                 # Folder for application execution captures
│   ├── architecture_diagram.md      # Mermaid specification of the system architecture
│   └── sample_output.txt            # Console output logs
├── README.md                        # Project instruction manual (This file)
├── Report_Outline.md                # Comprehensive outline for academic report
└── SmartEmergencyRoutePlanner.csproj # .NET 10 Project file
```

---

## 7. Getting Started

### Prerequisites
*   .NET 10.0 SDK or .NET 8.0 SDK installed on your system.

### How to Build
Open a terminal (Command Prompt, PowerShell, or bash) in the repository root folder and run:
```bash
dotnet build
```

### How to Run
To run the interactive CLI application:
```bash
dotnet run --project SmartEmergencyRoutePlanner.csproj
```

---

## 8. How to Run Benchmarks & Reproduce Results

1.  Start the program using `dotnet run --project SmartEmergencyRoutePlanner.csproj`.
2.  Choose option **`2. Run Full Benchmark Suite`** from the interactive menu.
3.  The program will run the solvers on 10 configurations spanning two families: **RandomSparse ($E \approx 5V$)** and **GridCity ($E \approx 10V$)**.
4.  Each solver is run **5 times** per case (after a warm-up execution) to calculate:
    *   **Average Runtime (`AvgMs`)**
    *   **Minimum Runtime (`MinMs`)**
    *   **Maximum Runtime (`MaxMs`)**
5.  A consolidated copy-pasteable table will print to the console, and the detailed breakdown will be written to `bench/benchmark_results.csv`.

### Reproducibility Parameters
*   **Seed:** Standardized to seed `42` to ensure the exact same city coordinates and speeds are generated across all runs.
*   **Source / Target:** Source is always Vertex `0`, and Target is always Vertex `V - 1`.
*   **Backbone Path:** A deterministic path is constructed initially to guarantee that the destination is reachable.

---

## 9. Analyzing Results and Plotting

The output CSV file `bench/benchmark_results.csv` contains the raw measurements. To create evaluation plots:
1.  Open `bench/benchmark_results.csv` in Excel, Google Sheets, or python (Pandas/Matplotlib).
2.  Create the following plots:
    *   **Runtime vs. Vertices:** Line chart plotting `DijkstraAvgMs` and `AStarAvgMs` (Y-axis) against `VertexCount` (X-axis) for both Sparse and GridCity families.
    *   **Expanded Nodes vs. Vertices:** Line chart plotting `DijkstraExpandedNodes` and `AStarExpandedNodes` (Y-axis) against `VertexCount` (X-axis).
    *   **Relaxation count vs. Vertices:** Chart comparing the relaxation operations to illustrate the practical work against theoretical complexities.
    *   **Speedup Ratio:** Plot the speedup ratio `AStarSpeedup` across graph sizes to visualize A*'s heuristic efficiency.

---

## 10. Academic Integrity and Project Team

We pledge that the code and documentation in this repository have been written entirely by our team members. No external library code has been integrated into the core pathfinding algorithms.

### Team Members
*   **Member 1** (Student ID: [Placeholder])
*   **Member 2** (Student ID: [Placeholder])
*   **Member 3** (Student ID: [Placeholder])
