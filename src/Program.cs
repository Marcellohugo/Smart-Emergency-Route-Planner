using System;
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
                Console.WriteLine("2. Run Full Benchmark Suite (V = 100 to 10000)");
                Console.WriteLine("3. Run Custom Route Test");
                Console.WriteLine("4. Exit");
                Console.WriteLine("==================================================");
                Console.Write("Select menu option (1-4): ");

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
                        running = false;
                        Console.WriteLine("Thank you for using Smart Emergency Route Planner. Exiting...");
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid option. Please enter a number between 1 and 4.");
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

            Console.WriteLine($"Generating synthetic city graph (V = {vertexCount}, E = {edgeCount}, Seed = {seed})...");
            var graph = CityGraphGenerator.Generate(vertexCount, edgeCount, seed);

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
            PrintPathResult(resDijkstra);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("                A* SEARCH ALGORITHM               ");
            Console.WriteLine("==================================================");
            PrintPathResult(resAStar);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("              BELLMAN-FORD VALIDATOR              ");
            Console.WriteLine("==================================================");
            PrintPathResult(resBf);

            bool distMatch = Math.Abs(resDijkstra.TotalTravelTimeMinutes - resAStar.TotalTravelTimeMinutes) < 1e-5;
            bool bfMatch = Math.Abs(resDijkstra.TotalTravelTimeMinutes - resBf.TotalTravelTimeMinutes) < 1e-5;

            Console.WriteLine("\n==================================================");
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
            int seed = ReadIntInput("Enter random seed (integer): ", int.MinValue, int.MaxValue);
            int source = ReadIntInput($"Enter source vertex ID (0 <= ID < {vertexCount}): ", 0, vertexCount - 1);
            int target = ReadIntInput($"Enter target vertex ID (0 <= ID < {vertexCount}, must be != source): ", 0, vertexCount - 1);

            while (source == target)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Source and target vertices cannot be the same for route testing.");
                Console.ResetColor();
                target = ReadIntInput($"Enter target vertex ID (0 <= ID < {vertexCount}, must be != source): ", 0, vertexCount - 1);
            }

            Console.WriteLine($"\nGenerating graph (V = {vertexCount}, E = {edgeCount}, Seed = {seed})...");
            var graph = CityGraphGenerator.Generate(vertexCount, edgeCount, seed);

            var dijkstra = new DijkstraSolver();
            var astar = new AStarSolver();

            Console.WriteLine("Executing Dijkstra Solver...");
            var resDijkstra = dijkstra.Solve(graph, source, target);

            Console.WriteLine("Executing A* Solver...");
            var resAStar = astar.Solve(graph, source, target, 100.0);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("                  DIJKSTRA RESULTS                ");
            Console.WriteLine("==================================================");
            PrintPathResult(resDijkstra);

            Console.WriteLine("\n==================================================");
            Console.WriteLine("                    A* RESULTS                    ");
            Console.WriteLine("==================================================");
            PrintPathResult(resAStar);

            bool distMatch = Math.Abs(resDijkstra.TotalTravelTimeMinutes - resAStar.TotalTravelTimeMinutes) < 1e-5;
            Console.WriteLine("\n==================================================");
            Console.WriteLine($"Dijkstra and A* cost matches: {distMatch}");
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

        private static void PrintPathResult(PathResult result)
        {
            Console.WriteLine($"Algorithm Name : {result.AlgorithmName}");
            Console.WriteLine($"Reachable      : {result.IsReachable}");
            Console.WriteLine($"Travel Time    : {(result.IsReachable ? $"{result.TotalTravelTimeMinutes:F4} minutes" : "N/A")}");
            Console.WriteLine($"Path Length    : {result.Path.Count} nodes");
            Console.WriteLine($"Expanded Nodes : {result.ExpandedNodes}");
            Console.WriteLine($"Runtime (ms)   : {result.RuntimeMilliseconds:F4} ms");
            Console.WriteLine($"Runtime (ticks): {result.RuntimeTicks} ticks");
            Console.WriteLine($"Negative Cycle : {result.HasNegativeCycle}");
            Console.WriteLine($"Path           : {PathFormatter.Format(result.Path)}");
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
