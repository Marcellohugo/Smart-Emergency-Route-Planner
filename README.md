# Smart Emergency Route Planner: Comparing Dijkstra and A* Search with Bellman-Ford Validation

A high-performance C# Console Application designed to model, solve, and analyze the single-source shortest path problem (SSSP) on synthetic urban road networks. The system computes the fastest route for emergency vehicles (e.g., ambulances) from a dispatch hub to a target hospital. 

This project was built from scratch without external graph or pathfinding libraries to satisfy the academic requirements of the **Design & Analysis of Algorithms** course.

---

## 1. Problem Background & Real-World Motivation

In emergency services, every second counts. Finding the fastest route for an ambulance through an urban network can mean the difference between life and death. Traditional navigation services often rely on static pathfinding or proprietary black-box engines. 

This project models a city road network as a weighted directed graph, where edges represent road segments with varying speed limits and lengths. Instead of routing by shortest distance (which may lead through slow, congested residential streets), the planner routes by **shortest travel time** (in minutes) to simulate emergency vehicle behavior.

---

## 2. Formal Mathematical Model

The urban street network is modeled as a weighted directed graph $G = (V, E)$, defined as follows:

*   **Vertices ($V$):** A set of vertices representing street intersections, landmark locations, or routing hubs. Each vertex $v \in V$ is positioned on a 2D Cartesian plane with coordinates $(X_v, Y_v)$ in a $100 \times 100$ km city grid.
*   **Edges ($E$):** A set of directed edges representing road segments. Each edge $e = (u, v) \in E$ goes from intersection $u$ to intersection $v$.
*   **Edge Weight ($w(e)$):** The weight of an edge represents the travel time in minutes. It is calculated as:
    $$w(e) = \text{TravelTimeMinutes}(e) = \left( \frac{\text{DistanceKm}(e)}{\text{SpeedKmh}(e)} \right) \times 60$$
    Where $\text{DistanceKm}(e)$ is the Euclidean distance between $u$ and $v$ in kilometers, and $\text{SpeedKmh}(e) \in [20, 100]$ represents the road segment's speed limit in km/h.
*   **Source ($s \in V$):** The start vertex (ambulance starting point), designated as Vertex `0`.
*   **Target ($t \in V$):** The destination vertex (central hospital), designated as Vertex `V - 1`.
*   **Objective:** Find a directed path $P = (v_0, v_1, \dots, v_k)$ from $v_0 = s$ to $v_k = t$ that minimizes the sum of edge weights:
    $$\min \sum_{i=0}^{k-1} w(v_i, v_{i+1})$$

---

## 3. Algorithm Overview

### Dijkstra's Algorithm
Dijkstra's algorithm finds the exact shortest path from a single source to all other vertices on a graph with non-negative weights. It runs in $O((V + E) \log V)$ time using our custom Binary Min-Heap.
*   **Invariant:** Once a vertex is extracted from the priority queue, its shortest distance from the source is finalized.
*   **Optimization:** Stops early as soon as the target vertex is extracted.

### A* Search Algorithm
A* improves upon Dijkstra by incorporating a heuristic to guide the search towards the target. It runs in $O((V + E) \log V)$ worst-case time, but expands fewer vertices in practice.
*   **Priority Metric:** $f(v) = g(v) + h(v)$
    *   $g(v)$ is the exact travel time from the source to vertex $v$.
    *   $h(v)$ is the estimated travel time from $v$ to the target.
*   **Heuristic Definition:** Straight-line Euclidean distance over maximum possible speed:
    $$h(v, t) = \frac{\text{EuclideanDistance}(v, t)}{\text{MaxSpeedKmh}} \times 60$$
*   **Admissibility & Consistency:** Since speed limits are bounded by $\text{MaxSpeedKmh} = 100.0$ km/h, the straight-line travel time at max speed represents the absolute physical lower bound of travel time. Therefore, $h(v, t)$ never overestimates the actual remaining cost (admissible) and satisfies the triangle inequality (consistent), guaranteeing that A* finds the optimal shortest path.

### Bellman-Ford Validator
A slower dynamic programming shortest-path algorithm running in $O(V \cdot E)$ time. It relaxes all edges $V-1$ times and detects negative weight cycles.
*   **Role:** Used as an independent correctness validator for small-to-medium graphs ($V \le 1000$). For larger graphs, it is skipped to prevent severe benchmark slowdowns.

---

## 4. Key Data Structures

*   **Adjacency List:** The graph $G$ is represented using an array of edge lists `List<Edge>[]` to enable efficient $O(1)$ neighbor traversal.
*   **Custom Binary Min-Heap:** An array-based binary min-heap implemented from scratch in `BinaryMinHeap.cs`. To avoid the overhead of index-tracking for a standard `DecreaseKey` operation, we use the **duplicate insertion with lazy deletion** approach:
    *   When a shorter distance to vertex $v$ is discovered, a new node `(v, new_distance)` is inserted.
    *   Upon extraction, if the node's priority is greater than the current recorded distance of $v$, the node is discarded as a stale entry.
*   **Lookup Tables:** Auxiliary arrays for tracking distances, parent paths (for reconstruction), and visited statuses.

---

## 5. Repository Structure

```
SmartEmergencyRoutePlanner/
├── src/
│   ├── Models/
│   │   ├── Edge.cs                  # Edge representation and travel time formula
│   │   ├── Vertex.cs                # Coordinates and naming details
│   │   ├── Graph.cs                 # Graph structure with adjacency lists
│   │   └── PathResult.cs            # Solver output container
│   ├── DataStructures/
│   │   └── BinaryMinHeap.cs         # Custom array-backed min priority queue
│   ├── Algorithms/
│   │   ├── DijkstraSolver.cs        # Dijkstra algorithm (from scratch)
│   │   ├── AStarSolver.cs           # A* algorithm (from scratch)
│   │   └── BellmanFordSolver.cs     # Bellman-Ford validator (from scratch)
│   ├── Generators/
│   │   └── CityGraphGenerator.cs    # Reproducible synthetic urban network builder
│   ├── Benchmark/
│   │   ├── BenchmarkCase.cs         # Size configuration (V, E, seed)
│   │   ├── BenchmarkResult.cs       # Metrics storage
│   │   └── BenchmarkRunner.cs       # Multi-run evaluation coordinator
│   ├── Utilities/
│   │   ├── CsvWriter.cs             # CSV file generator
│   │   ├── PathFormatter.cs         # Path print helper
│   │   └── Geometry.cs              # Euclidean distance calculations
│   └── Program.cs                   # Interactive CLI menu
├── bench/
│   └── benchmark_results.csv        # Output CSV containing raw benchmark results
├── docs/
│   ├── screenshots/                 # Folder for application execution captures
│   ├── architecture_diagram.md      # Mermaid specification of the system architecture
│   └── sample_output.txt            # Console output logs
├── README.md                        # Project instruction manual (This file)
├── Report_Outline.md                # Comprehensive outline for academic report
└── SmartEmergencyRoutePlanner.csproj # .NET 8 Project file
```

---

## 6. Getting Started

### Prerequisites
*   .NET 8.0 SDK or .NET 7.0 SDK installed on your system.

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

## 7. How to Run Benchmarks & Reproduce Results

1.  Start the program using `dotnet run --project SmartEmergencyRoutePlanner.csproj`.
2.  Choose option **`2. Run Full Benchmark Suite`** from the interactive menu.
3.  The program will run the solvers on 5 primary network configurations spanning two orders of magnitude ($V=100$ to $V=10000$), testing both **Sparse ($E \approx 5V$)** and **Medium ($E \approx 10V$)** density families.
4.  Each solver is run **3 times** per case (after a warm-up execution) to average out JIT compilation overhead and measurement noise.
5.  A summary metrics table will print to the console, and the detailed breakdown will be written to `bench/benchmark_results.csv`.

### Reproducibility Parameters
*   **Seed:** Standardized to seed `42` to ensure the exact same city coordinates and speeds are generated across all runs.
*   **Source / Target:** Source is always Vertex `0`, and Target is always Vertex `V - 1`.
*   **Backbone Path:** A deterministic path is constructed initially to guarantee that the destination is reachable.

---

## 8. Analyzing Results and Plotting

The output CSV file `bench/benchmark_results.csv` contains the raw measurements. To create evaluation plots:
1.  Open `bench/benchmark_results.csv` in Excel, Google Sheets, or python (Pandas/Matplotlib).
2.  Create the following plots:
    *   **Runtime vs. Vertices:** Line chart plotting `DijkstraMs` and `AStarMs` (Y-axis) against `VertexCount` (X-axis) for both Sparse and Medium families.
    *   **Expanded Nodes vs. Vertices:** Line chart plotting `DijkstraExpandedNodes` and `AStarExpandedNodes` (Y-axis) against `VertexCount` (X-axis).
    *   **Speedup Ratio:** Plot the speedup ratio `AStarSpeedup = DijkstraMs / AStarMs` across graph sizes to visualize A*'s heuristic efficiency.

---

## 9. Academic Integrity and Project Team

We pledge that the code and documentation in this repository have been written entirely by our team members. No external library code has been integrated into the core pathfinding algorithms.

### Team Members
*   **Member 1** (Student ID: [Placeholder])
*   **Member 2** (Student ID: [Placeholder])
*   **Member 3** (Student ID: [Placeholder])
