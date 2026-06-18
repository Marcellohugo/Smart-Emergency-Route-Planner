# Smart Emergency Route Planner

Blazor WebAssembly app for simulating emergency ambulance routing on synthetic city road networks. The UI is Blazor, while the routing algorithms remain plain C# implementations for coursework, inspection, and benchmarking.

## Features

- Dijkstra, A*, Bellman-Ford validation, Bidirectional Dijkstra.
- Multi-hospital routing, alternative routes, Yen's K-shortest paths, and robust risk-aware routing.
- Road closures, traffic multipliers, time factors, and emergency-lane routing.
- Interactive SVG map, route metrics, method comparison table, hospital-distance table, and in-app correctness test runner.

## Structure

```text
src/
├── Algorithms/      # Pathfinding solvers
├── Components/      # Blazor UI panels
├── Models/          # Graph, edge, vertex, path result, traffic models
├── Pages/           # Blazor page orchestration
├── Services/        # Route-planning application services
├── Tests/           # Self-contained correctness suite
├── ViewModels/      # UI-only data records for panels and logs
└── Program.cs       # Blazor WebAssembly bootstrap
wwwroot/             # Blazor static assets
bench/               # Benchmark CSV outputs
docs/                # Diagrams and sample output
```

Generated `bin/` and `obj/` folders are ignored and must not be committed.

## Requirements

- .NET 10 SDK

## Build

```powershell
dotnet build
```

## Run

```powershell
dotnet run --project SmartEmergencyRoutePlanner.csproj
```

Open the local URL printed by .NET. For a fixed local URL:

```powershell
dotnet run --project SmartEmergencyRoutePlanner.csproj --urls http://127.0.0.1:5178
```

## Notes

The project intentionally keeps the core graph algorithms in local C# code instead of external pathfinding libraries so their behavior can be reviewed for Design and Analysis of Algorithms coursework.
