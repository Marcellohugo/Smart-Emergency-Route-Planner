# Smart Emergency Route Planner: Comparing Dijkstra and A* Search with Bellman-Ford Validation

A high-performance C# Console Application designed to model, solve, and analyze the single-source shortest path problem (SSSP) on synthetic urban road networks. The system computes the fastest route for emergency vehicles (e.g., ambulances) from a dispatch hub to a target hospital.

This project was built from scratch without external graph or pathfinding libraries to satisfy the academic requirements of the **Design & Analysis of Algorithms** course.

---

## 1. Problem Background & Real-World Motivation

In emergency services, every second counts. Finding the fastest route for an ambulance through an urban network can mean the difference between life and death. Traditional navigation services often rely on static pathfinding or proprietary black-box engines.

This project models a city road network as a weighted directed graph, where edges represent road segments with varying speed limits and lengths. Instead of routing by shortest distance (which may lead through slow, congested residential streets), the planner routes by **shortest travel time** (in minutes) to simulate emergency vehicle behavior.

---

## 2. Advanced Project Features

### 2.1 Road Closure Simulation
Emergency situations often involve road closures due to accidents, flooding, or construction. The graph supports toggling edges active or inactive ($E_{\text{active}} \subseteq E$). Dijkstra, A*, and Bellman-Ford solvers dynamically bypass closed edges.
*   **Key Graph Methods:** `CloseEdge(from, to)`, `OpenEdge(from, to)`, `CloseRandomEdges(rate, seed)`, and `ResetClosures()`.

### 2.2 Traffic Condition Modifier & Time-Dependent Traffic
*   **Dynamic Traffic Congestion Levels:** Supports traffic levels (`Low`, `Normal`, `High`, `Severe`) which scale edge traversal weights:
    *   **Traffic Multipliers:** Low ($0.8\times$), Normal ($1.0\times$), High ($1.5\times$), Severe ($2.5\times$).
*   **Departure Time-Periods:** Incorporates time periods (`MorningRush`, `Midday`, `EveningRush`, `Night`):
    *   **Time Period Multipliers:** MorningRush ($1.8\times$), Midday ($1.0\times$), EveningRush ($2.0\times$), Night ($0.7\times$).
*   **Dynamic Weight Formula:**
    $$\text{EffectiveTravelTimeMinutes}(e) = \text{TravelTimeMinutes}(e) \times \text{TrafficMultiplier}(e) \times \text{TimePeriodMultiplier}(e)$$

### 2.3 Emergency Priority Lane
Ambulances can utilize designated lanes to bypass traffic congestion.
*   **Emergency Lane Multiplier:** Edges marked with `HasEmergencyLane = true` receive a $0.6\times$ discount when emergency mode is active.
*   **Weight Calculation:**
    $$\text{Weight}(e) = \text{EffectiveTravelTimeMinutes}(e) \times 0.6 \quad (\text{if emergencyMode is active and } HasEmergencyLane)$$

### 2.4 Multi-Hospital Nearest Route Mode
Ambulances can find the closest hospital from a list of target candidate locations.
*   **Dijkstra Multi-Target Solver:** Executes Dijkstra once from the source to solve the SSSP to all nodes, then identifies the hospital with the shortest travel time. Highly efficient for multiple targets ($O((V+E)\log V)$ total).
*   **A\* Multi-Target Solver:** Executes A* iteratively to each target hospital. Fast for a single target, but total work scales linearly with target count ($O(k \cdot (V+E)\log V)$).

### 2.5 Bidirectional Dijkstra Solver
Speeds up single-source single-target queries. Runs two simultaneous Dijkstra priority queues from the source (forward) and target (backward).
*   **Reverse Graph:** Uses `ReverseAdjacencyList` where `ReverseAdjacencyList[to]` stores incoming edges to `to`.
*   **Stopping Criterion:** Terminated early when `minForward + minBackward >= bestDistance` is met.

### 2.6 Alternative Route Planning (K-Shortest Paths)
*   **Repeated Penalty Heuristic:** Runs Dijkstra repeatedly, applying a $2.0\times$ penalty factor to edges utilized in prior paths. Calculates overlap percentage relative to the primary route.
*   **Yen's Exact Algorithm:** Dynamically disables spur edges and root path nodes to identify the exact K-shortest loopless paths.

### 2.7 Robust Risk-Aware Routing
*   Balances time and safety. Edges are assigned `ClosureRisk` ($[0.0, 0.1]$) and `TrafficRisk` ($[0.0, 0.3]$).
*   **Robust Weight Formula:**
    $$\text{RobustWeight}(e) = \text{TravelTime}(e) + \lambda \times (\text{ClosureRisk}(e) + \text{TrafficRisk}(e))$$
*   Trades off distance for a significantly safer, less congested path.

### 2.8 Multiple Graph Families
Benchmarks evaluate algorithms across two distinct structural network models:
*   **RandomSparse:** A highly randomized network layout with $E \approx 5V$.
*   **GridCity:** A structured 2D grid resembling real city planning. Nodes are connected to horizontal/vertical grid neighbors and organic diagonal avenues, providing a highly informative spatial layout for A*'s heuristic.

---

## 3. Analytical & Validation Frameworks

### 3.1 Self-Contained Correctness Test Suite
Run the suite (CLI Option 11) to execute 7 unit tests covering:
1. Simple 5-vertex graph path validation
2. Disconnected graph handling
3. Multiple equal-cost paths
4. Road closures re-routing
5. Traffic modifier shifts
6. Bellman-Ford negative cycle detection
7. Dijkstra vs. A* consistency checks

### 3.2 Log-log Empirical Growth Regression
Determines the empirical growth exponent ($b$) in the runtime model $T(V) = a \cdot V^b$.
*   Uses a least-squares linear regression: $\ln(\text{runtime}) = \ln(a) + b \ln(V)$ to calculate and print $b$ for all solvers.

### 3.3 Memory Usage Profiling
Captures memory consumption using `GC.GetTotalMemory(true)` immediately before and after solver execution, saving the allocated bytes directly to CSV.

### 3.4 Graphviz DOT Exporter
Generates a `docs/graph_demo.dot` file showing vertices (source is green, target is red, path is yellow) and edges (thick dark-green path edges, dashed red closed edges).

---

## 4. Repository Structure

```
SmartEmergencyRoutePlanner/
├── src/
│   ├── Models/
│   │   ├── Edge.cs                  # Edge representation with GetWeight()
│   │   ├── Vertex.cs                # Coordinates and naming details
│   │   ├── Graph.cs                 # Graph structure with ReverseAdjacencyList
│   │   ├── PathResult.cs            # Solver output container
│   │   └── TrafficLevel.cs          # Enum for low, normal, high, severe congestion
│   ├── DataStructures/
│   │   └── BinaryMinHeap.cs         # Custom priority queue with Peek() method
│   ├── Algorithms/
│   │   ├── DijkstraSolver.cs        # Dijkstra algorithm (from scratch)
│   │   ├── AStarSolver.cs           # A* algorithm (from scratch)
│   │   ├── BellmanFordSolver.cs     # Bellman-Ford validator (from scratch)
│   │   ├── DijkstraMultiTargetSolver.cs # Single-source multi-hospital solver
│   │   ├── AStarMultiTargetSolver.cs    # Multi-run A* multi-hospital solver
│   │   ├── BidirectionalDijkstraSolver.cs # Bi-directional Dijkstra search
│   │   ├── AlternativeRouteSolver.cs    # Penalty-based alternatives
│   │   ├── YenKShortestPathsSolver.cs   # Yen's Exact K-shortest loopless paths
│   │   └── RobustRouteSolver.cs         # Risk-aware solver
│   ├── Generators/
│   │   ├── GraphFamily.cs           # Enum for RandomSparse, RandomMedium, GridCity
│   │   └── CityGraphGenerator.cs    # Multi-family urban network builder
│   ├── Benchmark/
│   │   ├── BenchmarkCase.cs         # Size configuration (V, E, seed)
│   │   ├── BenchmarkResult.cs       # Metrics storage with timing stats and memory
│   │   └── BenchmarkRunner.cs       # Coordinates benchmarks and scenario matrices
│   ├── Analysis/
│   │   └── EmpiricalGrowthAnalyzer.cs   # Fits regression exponents
│   ├── Utilities/
│   │   ├── CsvWriter.cs             # CSV file generator
│   │   ├── PathFormatter.cs         # Detailed route explainer
│   │   └── GraphVizExporter.cs      # Graphviz DOT exporter
│   ├── Tests/
│   │   └── AlgorithmCorrectnessTests.cs # 7-case correctness suite
│   └── Program.cs                   # Interactive 14-option CLI menu
├── visualizer/
│   ├── index.html                   # Visualizer frontend entrypoint (Double-click to open)
│   ├── style.css                    # Glassmorphic dark mode styling sheet
│   └── app.js                       # Visualizer logic, generators, and canvas renderer
├── bench/
│   ├── benchmark_results.csv        # Consolidated timing and memory benchmark report
│   └── scenario_results.csv         # Matrix experiment results
├── docs/
│   ├── screenshots/                 # Folder for application execution captures
│   ├── architecture_diagram.md      # Mermaid specification of the system architecture
│   ├── graph_demo.dot               # Graphviz DOT output file
│   └── sample_output.txt            # Console output logs
├── README.md                        # Project instruction manual (This file)
├── Report_Outline.md                # Comprehensive outline for academic report
└── SmartEmergencyRoutePlanner.csproj # .NET 10 Project file
```

---

## 5. Getting Started

### Prerequisites
*   .NET 10.0 SDK or .NET 8.0 SDK installed on your system.

### How to Build
Open a terminal in the repository root folder and run:
```bash
dotnet build
```

### How to Run
To run the interactive CLI application:
```bash
dotnet run --project SmartEmergencyRoutePlanner.csproj
```

---

## 6. How to Run Benchmarks & Reproduce Results

1.  Start the program using `dotnet run --project SmartEmergencyRoutePlanner.csproj`.
2.  Choose option **`2. Run Full Benchmark Suite`** from the interactive menu.
3.  The program will run the solvers on 10 configurations spanning two families: **RandomSparse ($E \approx 5V$)** and **GridCity ($E \approx 10V$)**.
4.  Each solver is run **5 times** per case (after JIT warm-up) to calculate Min, Max, and Average Ms, GC memory, and regression slope exponents.
5.  Results are saved to `bench/benchmark_results.csv`.
6.  Choose option **`13. Run Scenario Comparison Matrix`** to run matrix tests and output `bench/scenario_results.csv`.

---

## 7. Rendering Graphviz Visualizations

To render `docs/graph_demo.dot`:
1.  Run option **`12. Export Demo Graph to DOT`** from the CLI menu.
2.  Open `docs/graph_demo.dot` and paste its content to an online viewer such as [Graphviz Online](https://dreampuf.github.io/GraphvizOnline/).
3.  Alternatively, if you have Graphviz installed locally, compile via terminal:
4.  The output PNG will show highlighted path edges and closed road segments.

---

## 8. Interactive Web Visualizer

We provide a beautiful, fully client-side **Single-Page Application (SPA) Web Visualizer** to interactively play with the routing algorithms, topologies, road closures, and traffic conditions in real-time.

### Key Visual Features:
*   **Frontier Expansion Animations:** Visualizes step-by-step visited frontiers and edge relaxation frames for Dijkstra, A*, and Bidirectional Dijkstra.
*   **Vertex Relocation & Editing:** Drag nodes to modify coordinates dynamically (instantly recalculating Euclidean distances and travel times), double-click to toggle emergency hospital targets, and right-click to redefine start/target nodes.
*   **Edge Closure Modifiers:** Click any edge line directly to toggle road closure states, forcing the solvers to dynamically route around obstacles.
*   **Dashboard Analytics:** Features live counters for travel time, nodes expanded, path distance, solver latency, and a comparative performance matrix.
*   **Bellman-Ford Validator:** Automatically validates query results against a Bellman-Ford reference solver in JavaScript and alerts you in case of heuristic suboptimality or negative weight cycles.

### How to Open:
No local server, node packages, or installations are required. Simply navigate to the `visualizer` folder and double-click `index.html` to open it in Chrome, Edge, or Firefox.

Alternatively, execute in your shell:
```powershell
# Windows PowerShell
Start-Process visualizer/index.html
```

---

## 9. Academic Integrity and Project Team

We pledge that the code and documentation in this repository have been written entirely by our team members. No external library code has been integrated into the core pathfinding algorithms.

### Team Members
*   **Member 1** (Student ID: [Placeholder])
*   **Member 2** (Student ID: [Placeholder])
*   **Member 3** (Student ID: [Placeholder])
