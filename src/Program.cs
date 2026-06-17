using System;
using System.Collections.Generic;
using System.IO;
using SmartEmergencyRoutePlanner.Algorithms;
using SmartEmergencyRoutePlanner.Benchmark;
using SmartEmergencyRoutePlanner.Generators;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.Tests;
using SmartEmergencyRoutePlanner.Utilities;

namespace SmartEmergencyRoutePlanner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            bool running = true;

            while (running)
            {
                Console.WriteLine();
                Console.WriteLine("==================================================");
                Console.WriteLine("       === Smart Emergency Route Planner ===       ");
                Console.WriteLine("==================================================");
                Console.WriteLine("1. Run Small Demo (V = 12, E = 35)");
                Console.WriteLine("2. Run Full Benchmark Suite");
                Console.WriteLine("3. Run Custom Route Test");
                Console.WriteLine("4. Run Road Closure Scenario (5% closures)");
                Console.WriteLine("5. Run Traffic Modifier Scenario (Low to Severe)");
                Console.WriteLine("6. Run Nearest Hospital Scenario (Multi-Hospital)");
                Console.WriteLine("7. Run Bidirectional Dijkstra Comparison");
                Console.WriteLine("8. Run Alternative Routes (Penalty Heuristic vs Yen's Exact)");
                Console.WriteLine("9. Run Emergency Priority Lane Scenario");
                Console.WriteLine("10. Run Robust Route Scenario (Risk-Aware)");
                Console.WriteLine("11. Run Correctness Unit Tests");
                Console.WriteLine("12. Export Demo Graph to DOT (Graphviz)");
                Console.WriteLine("13. Run Scenario Comparison Matrix");
                Console.WriteLine("14. Exit");
                Console.WriteLine("==================================================");
                Console.Write("Select menu option (1-14): ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        RunSmallDemo();
                        break;
                    case "2":
                        RunFullBenchmark();
                        break;
                    case "3":
                        RunCustomRouteTest();
                        break;
                    case "4":
                        RunRoadClosureScenario();
                        break;
                    case "5":
                        RunTrafficScenario();
                        break;
                    case "6":
                        RunNearestHospitalScenario();
                        break;
                    case "7":
                        RunBidirectionalDijkstraComparison();
                        break;
                    case "8":
                        RunAlternativeRoutesScenario();
                        break;
                    case "9":
                        RunEmergencyLaneScenario();
                        break;
                    case "10":
                        RunRobustRouteScenario();
                        break;
                    case "11":
                        AlgorithmCorrectnessTests.RunSuite();
                        break;
                    case "12":
                        ExportDemoGraphToDot();
                        break;
                    case "13":
                        RunScenarioComparisonMatrix();
                        break;
                    case "14":
                        running = false;
                        Console.WriteLine("Thank you for using Smart Emergency Route Planner. Exiting...");
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid option. Please enter a number between 1 and 14.");
                        Console.ResetColor();
                        break;
                }
            }
        }

        private static void RunSmallDemo()
        {
            Console.WriteLine("\n--- Running Small Demo ---");
            int vertexCount = 12;
            int edgeCount = 35;
            int seed = 42;
            int source = 0;
            int target = vertexCount - 1;

            Console.WriteLine($"Generating synthetic city graph (V = {vertexCount}, E = {edgeCount}, Seed = {seed}, Family = RandomSparse)...");
            var graph = CityGraphGenerator.Generate(vertexCount, edgeCount, seed, GraphFamily.RandomSparse);

            Console.WriteLine($"Routing ambulance from Intersection {source} to Hospital Intersection {target}...\n");

            var dijkstra = new DijkstraSolver();
            var astar = new AStarSolver();
            var bf = new BellmanFordSolver();

            var resDijkstra = dijkstra.Solve(graph, source, target);
            var resAStar = astar.Solve(graph, source, target, 100.0);
            var resBf = bf.Solve(graph, source, target);

            Console.WriteLine("==================================================");
            Console.WriteLine("                DIJKSTRA ALGORITHM                ");
            Console.WriteLine("==================================================");
            Console.WriteLine(PathFormatter.FormatDetailed(resDijkstra, source, target));

            Console.WriteLine("==================================================");
            Console.WriteLine("                A* SEARCH ALGORITHM               ");
            Console.WriteLine("==================================================");
            Console.WriteLine(PathFormatter.FormatDetailed(resAStar, source, target));

            Console.WriteLine("==================================================");
            Console.WriteLine("              BELLMAN-FORD VALIDATOR              ");
            Console.WriteLine("==================================================");
            Console.WriteLine(PathFormatter.FormatDetailed(resBf, source, target));

            bool distMatch = Math.Abs(resDijkstra.TotalTravelTimeMinutes - resAStar.TotalTravelTimeMinutes) < 1e-5;
            bool bfMatch = Math.Abs(resDijkstra.TotalTravelTimeMinutes - resBf.TotalTravelTimeMinutes) < 1e-5;

            Console.WriteLine("==================================================");
            Console.WriteLine("                 CORRECTNESS CHECK                ");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Dijkstra Travel Time equals A* Travel Time : {distMatch} (Diff: {Math.Abs(resDijkstra.TotalTravelTimeMinutes - resAStar.TotalTravelTimeMinutes):E5} min)");
            Console.WriteLine($"Dijkstra Travel Time equals Bellman-Ford   : {bfMatch} (Diff: {Math.Abs(resDijkstra.TotalTravelTimeMinutes - resBf.TotalTravelTimeMinutes):E5} min)");
            Console.WriteLine($"Path Match (Dijkstra vs A*)                : {ComparePaths(resDijkstra.Path, resAStar.Path)}");
            Console.WriteLine($"Path Match (Dijkstra vs Bellman-Ford)      : {ComparePaths(resDijkstra.Path, resBf.Path)}");
            Console.WriteLine("==================================================");
        }

        private static void RunFullBenchmark()
        {
            Console.WriteLine("\n--- Running Full Benchmark Suite ---");
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "bench");
            string outputPath = Path.Combine(outputDirectory, "benchmark_results.csv");

            try
            {
                BenchmarkRunner.RunAll(outputPath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Benchmark execution failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void RunCustomRouteTest()
        {
            Console.WriteLine("\n--- Run Custom Route Test ---");
            
            int vertexCount = ReadIntInput("Enter number of vertices (V >= 2): ", 2, int.MaxValue);
            int maxEdges = vertexCount * (vertexCount - 1);
            int edgeCount = ReadIntInput($"Enter number of edges (V-1 <= E <= {maxEdges}): ", vertexCount - 1, maxEdges);
            
            Console.WriteLine("Select Graph Family:\n1. RandomSparse\n2. RandomMedium\n3. GridCity");
            int familyChoice = ReadIntInput("Choice (1-3): ", 1, 3);
            GraphFamily family = familyChoice == 1 ? GraphFamily.RandomSparse :
                                 familyChoice == 2 ? GraphFamily.RandomMedium :
                                 GraphFamily.GridCity;

            int seed = ReadIntInput("Enter random seed (integer): ", int.MinValue, int.MaxValue);
            int source = ReadIntInput($"Enter source vertex ID (0 <= ID < {vertexCount}): ", 0, vertexCount - 1);
            int target = ReadIntInput($"Enter target vertex ID (0 <= ID < {vertexCount}, must be != source): ", 0, vertexCount - 1);

            while (source == target)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Source and target vertices cannot be the same.");
                Console.ResetColor();
                target = ReadIntInput($"Enter target vertex ID (0 <= ID < {vertexCount}, must be != source): ", 0, vertexCount - 1);
            }

            Console.WriteLine($"\nGenerating graph ({family}, V = {vertexCount}, E = {edgeCount}, Seed = {seed})...");
            var graph = CityGraphGenerator.Generate(vertexCount, edgeCount, seed, family);

            var dijkstra = new DijkstraSolver();
            var astar = new AStarSolver();

            Console.WriteLine("Executing Dijkstra Solver...");
            var resDijkstra = dijkstra.Solve(graph, source, target);

            Console.WriteLine("Executing A* Solver...");
            var resAStar = astar.Solve(graph, source, target, 100.0);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("                  DIJKSTRA RESULTS                ");
            Console.WriteLine("==================================================");
            Console.WriteLine(PathFormatter.FormatDetailed(resDijkstra, source, target));

            Console.WriteLine("==================================================");
            Console.WriteLine("                    A* RESULTS                    ");
            Console.WriteLine("==================================================");
            Console.WriteLine(PathFormatter.FormatDetailed(resAStar, source, target));

            bool distMatch = Math.Abs(resDijkstra.TotalTravelTimeMinutes - resAStar.TotalTravelTimeMinutes) < 1e-5;
            Console.WriteLine("==================================================");
            Console.WriteLine($"Dijkstra and A* cost matches: {distMatch}");
            Console.WriteLine("==================================================");
        }

        private static void RunRoadClosureScenario()
        {
            Console.WriteLine("\n--- Road Closure Simulation ---");
            int V = 500;
            int E = 2500;
            int seed = 42;
            int source = 0;
            int target = V - 1;

            Console.WriteLine($"Generating Graph (GridCity, V = {V}, E = {E}, Seed = {seed})...");
            var graph = CityGraphGenerator.Generate(V, E, seed, GraphFamily.GridCity);

            var dijkstra = new DijkstraSolver();

            var normalResult = dijkstra.Solve(graph, source, target);

            double closureRate = 0.05;
            Console.WriteLine($"\nSimulating 5% road closures (closing {(int)(graph.AllEdges.Count * closureRate)} edges randomly)...");
            graph.CloseRandomEdges(closureRate, seed);

            var closedResult = dijkstra.Solve(graph, source, target);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("         ROAD CLOSURE SCENARIO COMPARISON         ");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Normal Travel Time            : {normalResult.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Normal Path                   : {PathFormatter.Format(normalResult.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Post-Closure Travel Time      : {(closedResult.IsReachable ? $"{closedResult.TotalTravelTimeMinutes:F2} min" : "Unreachable")}");
            Console.WriteLine($"Post-Closure Path             : {PathFormatter.Format(closedResult.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Is Target Still Reachable?      : {closedResult.IsReachable}");
            Console.WriteLine($"Did Path Change?                : {!ComparePaths(normalResult.Path, closedResult.Path)}");
            Console.WriteLine("==================================================");

            graph.ResetClosures();
        }

        private static void RunTrafficScenario()
        {
            Console.WriteLine("\n--- Traffic Condition Modifier Scenario ---");
            int V = 500;
            int E = 2500;
            int seed = 42;
            int source = 0;
            int target = V - 1;

            Console.WriteLine($"Generating Graph (GridCity, V = {V}, E = {E}, Seed = {seed})...");
            var graph = CityGraphGenerator.Generate(V, E, seed, GraphFamily.GridCity);

            var dijkstra = new DijkstraSolver();

            var normalResult = dijkstra.Solve(graph, source, target);

            Console.WriteLine("\nApplying dynamic traffic conditions (congestion factors 0.8x to 2.5x)...");
            graph.ApplyRandomTraffic(seed);

            var trafficResult = dijkstra.Solve(graph, source, target);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("            TRAFFIC SCENARIO COMPARISON           ");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Normal Travel Time            : {normalResult.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Normal Path                   : {PathFormatter.Format(normalResult.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Traffic-Adjusted Travel Time  : {trafficResult.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Traffic-Adjusted Path         : {PathFormatter.Format(trafficResult.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Travel Time Cost Increase     : {trafficResult.TotalTravelTimeMinutes - normalResult.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Did Route Shift to Bypass Jam?: {!ComparePaths(normalResult.Path, trafficResult.Path)}");
            Console.WriteLine("==================================================");

            graph.ResetTraffic();
        }

        private static void RunNearestHospitalScenario()
        {
            Console.WriteLine("\n--- Nearest Hospital Multi-Target Routing ---");
            int V = 500;
            int E = 2500;
            int seed = 42;
            int source = 0;

            Console.WriteLine($"Generating Graph (GridCity, V = {V}, E = {E}, Seed = {seed})...");
            var graph = CityGraphGenerator.Generate(V, E, seed, GraphFamily.GridCity);

            var random = new Random(seed);
            var hospitals = new List<int>();
            var hospitalSet = new HashSet<int>();

            while (hospitals.Count < 8)
            {
                int h = random.Next(1, V - 1);
                if (!hospitalSet.Contains(h))
                {
                    hospitalSet.Add(h);
                    hospitals.Add(h);
                }
            }

            Console.WriteLine("Candidate Hospital Vertex IDs: " + string.Join(", ", hospitals));

            var dijkstraMT = new DijkstraMultiTargetSolver();
            var astarMT = new AStarMultiTargetSolver();

            var resDijkstra = dijkstraMT.Solve(graph, source, hospitals);
            var resAStar = astarMT.Solve(graph, source, hospitals, 100.0);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("         DIJKSTRA MULTI-TARGET RESULTS            ");
            Console.WriteLine("==================================================");
            Console.WriteLine(PathFormatter.FormatDetailed(resDijkstra, source, -1));

            Console.WriteLine("==================================================");
            Console.WriteLine("            A* MULTI-TARGET RESULTS               ");
            Console.WriteLine("==================================================");
            Console.WriteLine(PathFormatter.FormatDetailed(resAStar, source, -1));

            Console.WriteLine("==================================================");
            Console.WriteLine("          MULTI-TARGET COMPLEXITY TRADE-OFF       ");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Dijkstra Single-Run Expanded Nodes: {resDijkstra.ExpandedNodes}");
            Console.WriteLine($"A* Multi-Run Total Expanded Nodes : {resAStar.ExpandedNodes}");
            Console.WriteLine($"Dijkstra Single-Run Relaxations   : {resDijkstra.RelaxationCount}");
            Console.WriteLine($"A* Multi-Run Total Relaxations    : {resAStar.RelaxationCount}");
            Console.WriteLine($"Dijkstra Solver Execution Time    : {resDijkstra.RuntimeMilliseconds:F3} ms");
            Console.WriteLine($"A* Solver Execution Time          : {resAStar.RuntimeMilliseconds:F3} ms");
            Console.WriteLine("==================================================");
        }

        private static void RunBidirectionalDijkstraComparison()
        {
            Console.WriteLine("\n--- Bidirectional Dijkstra Comparison ---");
            int V = 1000;
            int E = 10000;
            int seed = 42;
            int source = 0;
            int target = V - 1;

            Console.WriteLine($"Generating Graph (GridCity, V = {V}, E = {E}, Seed = {seed})...");
            var graph = CityGraphGenerator.Generate(V, E, seed, GraphFamily.GridCity);

            var dijkstra = new DijkstraSolver();
            var bidijkstra = new BidirectionalDijkstraSolver();

            Console.WriteLine("Executing Standard Dijkstra Solver...");
            var resD = dijkstra.Solve(graph, source, target);

            Console.WriteLine("Executing Bidirectional Dijkstra Solver...");
            var resBi = bidijkstra.Solve(graph, source, target);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("                STANDARD DIJKSTRA                 ");
            Console.WriteLine("==================================================");
            Console.WriteLine(PathFormatter.FormatDetailed(resD, source, target));

            Console.WriteLine("==================================================");
            Console.WriteLine("              BIDIRECTIONAL DIJKSTRA              ");
            Console.WriteLine("==================================================");
            Console.WriteLine(PathFormatter.FormatDetailed(resBi, source, target));

            Console.WriteLine("==================================================");
            Console.WriteLine("           BIDIRECTIONAL EFFICIENCY CHECK         ");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Standard Dijkstra Expanded Nodes: {resD.ExpandedNodes}");
            Console.WriteLine($"Bidirectional Dijkstra Exp Nodes: {resBi.ExpandedNodes}");
            double nodeSavings = ((double)(resD.ExpandedNodes - resBi.ExpandedNodes) / resD.ExpandedNodes) * 100.0;
            Console.WriteLine($"Search Space Node Reduction     : {nodeSavings:F1}%");
            Console.WriteLine($"Standard Dijkstra Relaxations   : {resD.RelaxationCount}");
            Console.WriteLine($"Bidirectional Dijk Relaxations  : {resBi.RelaxationCount}");
            Console.WriteLine($"Speedup Ratio (Dijk / BiDijk)   : {resD.RuntimeMilliseconds / resBi.RuntimeMilliseconds:F2}x");
            Console.WriteLine("==================================================");
        }

        private static void RunAlternativeRoutesScenario()
        {
            Console.WriteLine("\n--- Alternative Routes Generation ---");
            int V = 100;
            int E = 500;
            int seed = 42;
            int source = 0;
            int target = V - 1;

            Console.WriteLine($"Generating Graph (GridCity, V = {V}, E = {E}, Seed = {seed})...");
            var graph = CityGraphGenerator.Generate(V, E, seed, GraphFamily.GridCity);

            // 1. Repeated Penalty Alternative Routes
            Console.WriteLine("\nFinding alternative paths using Repeated Penalty Heuristic...");
            var penaltySolver = new AlternativeRouteSolver();
            var penaltyPaths = penaltySolver.FindAlternativeRoutes(graph, source, target, 3);

            // 2. Yen's Exact K-Shortest Paths
            Console.WriteLine("Finding alternative paths using Yen's Exact Algorithm...");
            var yenSolver = new YenKShortestPathsSolver();
            var yenPaths = yenSolver.FindKShortestPaths(graph, source, target, 3);

            Console.WriteLine("\n==================================================================================");
            Console.WriteLine("                      REPEATED PENALTY HEURISTIC PATHS                            ");
            Console.WriteLine("==================================================================================");
            for (int i = 0; i < penaltyPaths.Count; i++)
            {
                var pathRes = penaltyPaths[i];
                double overlap = i == 0 ? 0.0 : AlternativeRouteSolver.CalculatePathOverlap(penaltyPaths[0].Path, pathRes.Path);
                Console.WriteLine($"[Option {i + 1}] {pathRes.AlgorithmName}");
                Console.WriteLine($"  * Travel Time : {pathRes.TotalTravelTimeMinutes:F2} minutes");
                Console.WriteLine($"  * Overlap w/ Primary : {overlap:F1}%");
                Console.WriteLine($"  * Path        : {PathFormatter.Format(pathRes.Path)}");
                Console.WriteLine();
            }

            Console.WriteLine("==================================================================================");
            Console.WriteLine("                         YEN'S EXACT SHORT SHORT PATHS                            ");
            Console.WriteLine("==================================================================================");
            for (int i = 0; i < yenPaths.Count; i++)
            {
                var pathRes = yenPaths[i];
                double overlap = i == 0 ? 0.0 : AlternativeRouteSolver.CalculatePathOverlap(yenPaths[0].Path, pathRes.Path);
                Console.WriteLine($"[Option {i + 1}] {pathRes.AlgorithmName}");
                Console.WriteLine($"  * Travel Time : {pathRes.TotalTravelTimeMinutes:F2} minutes");
                Console.WriteLine($"  * Overlap w/ Primary : {overlap:F1}%");
                Console.WriteLine($"  * Path        : {PathFormatter.Format(pathRes.Path)}");
                Console.WriteLine();
            }
            Console.WriteLine("==================================================================================");
        }

        private static void RunEmergencyLaneScenario()
        {
            Console.WriteLine("\n--- Emergency Priority Lane Scenario ---");
            int V = 500;
            int E = 2500;
            int seed = 42;
            int source = 0;
            int target = V - 1;

            Console.WriteLine($"Generating Graph (GridCity, V = {V}, E = {E}, Seed = {seed})...");
            var graph = CityGraphGenerator.Generate(V, E, seed, GraphFamily.GridCity);

            // Apply traffic & assign some emergency priority lanes (done during generation but let's highlight)
            int laneCount = 0;
            foreach (var edge in graph.AllEdges)
            {
                if (edge.HasEmergencyLane) laneCount++;
            }
            Console.WriteLine($"Priority Lanes Present in Graph: {laneCount} edges ({(double)laneCount / graph.AllEdges.Count * 100.0:F1}%)");

            var dijkstra = new DijkstraSolver();

            // 1. Normal Routing
            var normalResult = dijkstra.Solve(graph, source, target, false);

            // 2. Emergency Mode Routing
            var emergencyResult = dijkstra.Solve(graph, source, target, true);

            double timeSaved = normalResult.TotalTravelTimeMinutes - emergencyResult.TotalTravelTimeMinutes;
            double percentSaved = (timeSaved / normalResult.TotalTravelTimeMinutes) * 100.0;

            Console.WriteLine("\n==================================================");
            Console.WriteLine("         EMERGENCY PRIORITY LANE COMPARISON       ");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Normal Travel Time : {normalResult.TotalTravelTimeMinutes:F2} minutes");
            Console.WriteLine($"Normal Path        : {PathFormatter.Format(normalResult.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Emergency Lane Time: {emergencyResult.TotalTravelTimeMinutes:F2} minutes");
            Console.WriteLine($"Emergency Path     : {PathFormatter.Format(emergencyResult.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Total Travel Time Saved : {timeSaved:F2} minutes");
            Console.WriteLine($"Percentage Reduction   : {percentSaved:F1}%");
            Console.WriteLine($"Did Path Change?       : {!ComparePaths(normalResult.Path, emergencyResult.Path)}");
            Console.WriteLine("==================================================");
        }

        private static void RunRobustRouteScenario()
        {
            Console.WriteLine("\n--- Robust Risk-Aware Route Scenario ---");
            int V = 500;
            int E = 2500;
            int seed = 42;
            int source = 0;
            int target = V - 1;

            Console.WriteLine($"Generating Graph (GridCity, V = {V}, E = {E}, Seed = {seed})...");
            var graph = CityGraphGenerator.Generate(V, E, seed, GraphFamily.GridCity);

            var dijkstra = new DijkstraSolver();
            var robust = new RobustRouteSolver();

            // 1. Fastest Route (Standard Dijkstra)
            var fastestResult = dijkstra.Solve(graph, source, target);

            // 2. Robust Route (Dijkstra minimizing TravelTime + lambda * Risk, lambda = 10)
            double lambda = 10.0;
            var robustResult = robust.Solve(graph, source, target, false, lambda);

            // Calculate risks along fastest path
            double fastestRisk = 0;
            for (int i = 0; i < fastestResult.Path.Count - 1; i++)
            {
                int u = fastestResult.Path[i];
                int v = fastestResult.Path[i + 1];
                foreach (var edge in graph.GetNeighbors(u))
                {
                    if (edge.To == v)
                    {
                        fastestRisk += edge.ClosureRisk + edge.TrafficRisk;
                        break;
                    }
                }
            }

            // Calculate risks along robust path
            double robustRisk = 0;
            for (int i = 0; i < robustResult.Path.Count - 1; i++)
            {
                int u = robustResult.Path[i];
                int v = robustResult.Path[i + 1];
                foreach (var edge in graph.GetNeighbors(u))
                {
                    if (edge.To == v)
                    {
                        robustRisk += edge.ClosureRisk + edge.TrafficRisk;
                        break;
                    }
                }
            }

            double riskDiff = fastestRisk - robustRisk;
            double riskReductionPercent = fastestRisk > 0 ? (riskDiff / fastestRisk) * 100.0 : 0;
            double timeDiff = robustResult.TotalTravelTimeMinutes - fastestResult.TotalTravelTimeMinutes;

            Console.WriteLine("\n==================================================");
            Console.WriteLine("          FASTEST ROUTE VS ROBUST ROUTE           ");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Fastest Route Travel Time : {fastestResult.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Fastest Route Risk Score  : {fastestRisk:F4}");
            Console.WriteLine($"Fastest Route Path        : {PathFormatter.Format(fastestResult.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Robust Route Travel Time  : {robustResult.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Robust Route Risk Score   : {robustRisk:F4}");
            Console.WriteLine($"Robust Route Path         : {PathFormatter.Format(robustResult.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Additional Travel Time   : {timeDiff:F2} min");
            Console.WriteLine($"Risk Score Reduced By     : {riskDiff:F4} ({riskReductionPercent:F1}% safety increase)");
            Console.WriteLine($"Did Path Change?          : {!ComparePaths(fastestResult.Path, robustResult.Path)}");
            Console.WriteLine("==================================================");
        }

        private static void ExportDemoGraphToDot()
        {
            Console.WriteLine("\n--- Exporting Graph to DOT Format ---");
            int V = 12;
            int E = 35;
            int seed = 42;
            int source = 0;
            int target = V - 1;

            var graph = CityGraphGenerator.Generate(V, E, seed, GraphFamily.RandomSparse);
            var astar = new AStarSolver();
            var result = astar.Solve(graph, source, target, 100.0);

            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "docs");
            string outputPath = Path.Combine(outputDir, "graph_demo.dot");

            try
            {
                GraphVizExporter.ExportToDot(graph, result.Path, outputPath);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"GraphViz DOT file successfully generated: {outputPath}");
                Console.ResetColor();
                Console.WriteLine("\nHow to view this graph:");
                Console.WriteLine("1. Copy-paste the content of docs/graph_demo.dot to an online viewer like https://dreampuf.github.io/GraphvizOnline/");
                Console.WriteLine("2. Or install Graphviz local compiler and run the command:");
                Console.WriteLine("   dot -Tpng docs/graph_demo.dot -o docs/graph_demo.png");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Export failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void RunScenarioComparisonMatrix()
        {
            Console.WriteLine("\n--- Scenario Comparison Matrix ---");
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "bench");
            string outputPath = Path.Combine(outputDirectory, "scenario_results.csv");

            try
            {
                BenchmarkRunner.RunScenarioMatrix(outputPath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Matrix execution failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static int ReadIntInput(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();
                if (int.TryParse(input, out int value) && value >= min && value <= max)
                {
                    return value;
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid input. Please enter a valid integer between {min} and {max}.");
                Console.ResetColor();
            }
        }

        private static bool ComparePaths(List<int> p1, List<int> p2)
        {
            if (p1.Count != p2.Count) return false;
            for (int i = 0; i < p1.Count; i++)
            {
                if (p1[i] != p2[i]) return false;
            }
            return true;
        }
    }
}
