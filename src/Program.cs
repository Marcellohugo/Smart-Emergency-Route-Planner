using System;
using System.Collections.Generic;
using System.IO;
using SmartEmergencyRoutePlanner.Algorithms;
using SmartEmergencyRoutePlanner.Benchmark;
using SmartEmergencyRoutePlanner.Generators;
using SmartEmergencyRoutePlanner.Models;
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
                Console.WriteLine("7. Exit");
                Console.WriteLine("==================================================");
                Console.Write("Select menu option (1-7): ");

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
                        running = false;
                        Console.WriteLine("Thank you for using Smart Emergency Route Planner. Exiting...");
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid option. Please enter a number between 1 and 7.");
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
            var astar = new AStarSolver();

            // Run Normal Pathfinding
            var normalDijkstra = dijkstra.Solve(graph, source, target);
            var normalAStar = astar.Solve(graph, source, target, 100.0);

            // Apply 5% Closures
            double closureRate = 0.05;
            Console.WriteLine($"\nSimulating 5% road closures (closing {(int)(graph.AllEdges.Count * closureRate)} edges randomly)...");
            graph.CloseRandomEdges(closureRate, seed);

            // Run Pathfinding under Closure Condition
            var closedDijkstra = dijkstra.Solve(graph, source, target);
            var closedAStar = astar.Solve(graph, source, target, 100.0);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("         ROAD CLOSURE SCENARIO COMPARISON         ");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Normal Travel Time (Dijkstra) : {normalDijkstra.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Normal Travel Time (A*)       : {normalAStar.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Normal Path                   : {PathFormatter.Format(normalDijkstra.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Post-Closure Travel Time (Dijk) : {(closedDijkstra.IsReachable ? $"{closedDijkstra.TotalTravelTimeMinutes:F2} min" : "Unreachable")}");
            Console.WriteLine($"Post-Closure Travel Time (A*)   : {(closedAStar.IsReachable ? $"{closedAStar.TotalTravelTimeMinutes:F2} min" : "Unreachable")}");
            Console.WriteLine($"Post-Closure Path               : {PathFormatter.Format(closedDijkstra.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Is Target Still Reachable?      : {closedDijkstra.IsReachable}");
            Console.WriteLine($"Did Path Change?                : {!ComparePaths(normalDijkstra.Path, closedDijkstra.Path)}");
            Console.WriteLine("==================================================");

            // Cleanup
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
            var astar = new AStarSolver();

            // Run Normal Pathfinding
            var normalDijkstra = dijkstra.Solve(graph, source, target);
            var normalAStar = astar.Solve(graph, source, target, 100.0);

            // Apply Random Traffic congestion levels
            Console.WriteLine("\nApplying dynamic traffic conditions (congestion factors 0.8x to 2.5x)...");
            graph.ApplyRandomTraffic(seed);

            // Run Pathfinding under Dynamic Traffic
            var trafficDijkstra = dijkstra.Solve(graph, source, target);
            var trafficAStar = astar.Solve(graph, source, target, 100.0);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("            TRAFFIC SCENARIO COMPARISON           ");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Normal Travel Time            : {normalDijkstra.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Normal Path                   : {PathFormatter.Format(normalDijkstra.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Traffic-Adjusted Travel Time  : {trafficDijkstra.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Traffic-Adjusted Path         : {PathFormatter.Format(trafficDijkstra.Path)}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"Travel Time Cost Increase     : {trafficDijkstra.TotalTravelTimeMinutes - normalDijkstra.TotalTravelTimeMinutes:F2} min");
            Console.WriteLine($"Did Route Shift to Bypass Jam?: {!ComparePaths(normalDijkstra.Path, trafficDijkstra.Path)}");
            Console.WriteLine("==================================================");

            // Cleanup
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

            // Pick 8 candidate hospital vertices deterministically using random
            var random = new Random(seed);
            var hospitals = new List<int>();
            var hospitalSet = new HashSet<int>();

            while (hospitals.Count < 8)
            {
                int h = random.Next(1, V - 1); // Exclude source (0) and standard target (V-1)
                if (!hospitalSet.Contains(h))
                {
                    hospitalSet.Add(h);
                    hospitals.Add(h);
                }
            }

            Console.WriteLine("Candidate Hospital Vertex IDs: " + string.Join(", ", hospitals));

            var dijkstraMT = new DijkstraMultiTargetSolver();
            var astarMT = new AStarMultiTargetSolver();

            // Run Dijkstra Multi-Target (Runs Dijkstra once from source to find best target)
            Console.WriteLine("Executing Dijkstra Multi-Target (single query to all targets)...");
            var resDijkstra = dijkstraMT.Solve(graph, source, hospitals);

            // Run A* Multi-Target (Runs A* multiple times, once to each target)
            Console.WriteLine("Executing A* Multi-Target (iterative individual queries)...");
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
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("TIMING NOTE:");
            Console.WriteLine("Because Dijkstra solves the SSSP problem (distances to ALL vertices) in one single run,");
            Console.WriteLine("it remains highly efficient when searching for the nearest of multiple targets.");
            Console.WriteLine("A* is faster for a SINGLE target, but when queried iteratively for many targets,");
            Console.WriteLine("its total node expansions and runtimes scale linearly with the target count.");
            Console.WriteLine("==================================================");
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
