using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartEmergencyRoutePlanner.Algorithms;
using SmartEmergencyRoutePlanner.Generators;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.Utilities;

namespace SmartEmergencyRoutePlanner.Benchmark
{
    public class BenchmarkRunner
    {
        private const int Repetitions = 3;

        /// <summary>
        /// Runs the complete benchmark suite, prints a consolidated summary table,
        /// and saves the output to a CSV file.
        /// </summary>
        public static List<BenchmarkResult> RunAll(string csvOutputPath)
        {
            Console.WriteLine("Initializing Benchmark Runner...");
            
            // Warm-up to trigger JIT compilation of critical execution paths
            WarmUp();

            // 5 sizes covering two orders of magnitude (V=100 to V=10000)
            // Includes both Sparse (E = 5V) and Medium (E = 10V) families for a stronger empirical study.
            var cases = new List<BenchmarkCase>
            {
                // Sparse family (E ≈ 5V)
                new BenchmarkCase(100, 500, 42),
                new BenchmarkCase(500, 2500, 42),
                new BenchmarkCase(1000, 5000, 42),
                new BenchmarkCase(5000, 25000, 42),
                new BenchmarkCase(10000, 50000, 42),

                // Medium family (E ≈ 10V)
                new BenchmarkCase(100, 1000, 42),
                new BenchmarkCase(500, 5000, 42),
                new BenchmarkCase(1000, 10000, 42),
                new BenchmarkCase(5000, 50000, 42),
                new BenchmarkCase(10000, 100000, 42)
            };

            var results = new List<BenchmarkResult>();

            Console.WriteLine("\n==========================================================================================");
            Console.WriteLine("                                  RUNNING BENCHMARKS                                      ");
            Console.WriteLine("==========================================================================================");
            Console.WriteLine($"{"Vertices",8} | {"Edges",8} | {"DijkMs",9} | {"AStarMs",9} | {"BF-Ms",9} | {"Speedup",7} | {"Match A*",8} | {"Match BF",8}");
            Console.WriteLine("------------------------------------------------------------------------------------------");

            var dijkstraSolver = new DijkstraSolver();
            var aStarSolver = new AStarSolver();
            var bellmanFordSolver = new BellmanFordSolver();

            foreach (var bCase in cases)
            {
                int V = bCase.VertexCount;
                int E = bCase.EdgeCount;
                int seed = bCase.Seed;

                // Generate network instance
                var graph = CityGraphGenerator.Generate(V, E, seed);
                int source = 0;
                int target = V - 1;

                // Under our synthetic map, speeds are generated from 20.0 to 100.0 km/h.
                // 100.0 km/h is the absolute maximum, making Euclidean time distance travel time admissible.
                double maxSpeed = 100.0;

                // --- 1. Dijkstra Solver ---
                double totalDijkstraMs = 0;
                PathResult dijkstraResult = null!;
                for (int r = 0; r < Repetitions; r++)
                {
                    var res = dijkstraSolver.Solve(graph, source, target);
                    if (r == 0) dijkstraResult = res;
                    totalDijkstraMs += res.RuntimeMilliseconds;
                }
                double avgDijkstraMs = totalDijkstraMs / Repetitions;

                // --- 2. A* Solver ---
                double totalAStarMs = 0;
                PathResult aStarResult = null!;
                for (int r = 0; r < Repetitions; r++)
                {
                    var res = aStarSolver.Solve(graph, source, target, maxSpeed);
                    if (r == 0) aStarResult = res;
                    totalAStarMs += res.RuntimeMilliseconds;
                }
                double avgAStarMs = totalAStarMs / Repetitions;

                // --- 3. Bellman-Ford Solver (Run only for small to medium scale) ---
                double? avgBellmanFordMs = null;
                double? bellmanFordDistance = null;
                string bfStatus = "Skipped (O(VE))";
                bool dijkstraEqualsBellmanFord = true;

                if (V <= 1000)
                {
                    double totalBellmanFordMs = 0;
                    PathResult bfResult = null!;
                    for (int r = 0; r < Repetitions; r++)
                    {
                        var res = bellmanFordSolver.Solve(graph, source, target);
                        if (r == 0) bfResult = res;
                        totalBellmanFordMs += res.RuntimeMilliseconds;
                    }
                    avgBellmanFordMs = totalBellmanFordMs / Repetitions;
                    bellmanFordDistance = bfResult.TotalTravelTimeMinutes;
                    bfStatus = "Completed";
                    
                    // Allow tiny rounding tolerances for floating-point travel times
                    dijkstraEqualsBellmanFord = Math.Abs(dijkstraResult.TotalTravelTimeMinutes - bfResult.TotalTravelTimeMinutes) < 1e-5;
                }

                bool dijkstraEqualsAStar = Math.Abs(dijkstraResult.TotalTravelTimeMinutes - aStarResult.TotalTravelTimeMinutes) < 1e-5;
                double speedup = avgAStarMs > 0 ? avgDijkstraMs / avgAStarMs : 0;

                var result = new BenchmarkResult
                {
                    VertexCount = V,
                    EdgeCount = E,
                    Seed = seed,
                    DijkstraMs = avgDijkstraMs,
                    AStarMs = avgAStarMs,
                    BellmanFordMs = avgBellmanFordMs,
                    DijkstraDistance = dijkstraResult.TotalTravelTimeMinutes,
                    AStarDistance = aStarResult.TotalTravelTimeMinutes,
                    BellmanFordDistance = bellmanFordDistance,
                    DijkstraExpandedNodes = dijkstraResult.ExpandedNodes,
                    AStarExpandedNodes = aStarResult.ExpandedNodes,
                    DijkstraPathLength = dijkstraResult.Path.Count,
                    AStarPathLength = aStarResult.Path.Count,
                    DijkstraEqualsAStar = dijkstraEqualsAStar,
                    DijkstraEqualsBellmanFord = dijkstraEqualsBellmanFord,
                    BellmanFordStatus = bfStatus,
                    AStarSpeedup = speedup
                };

                results.Add(result);

                // Display formatting
                string bfMsPrint = avgBellmanFordMs.HasValue ? $"{avgBellmanFordMs.Value,9:F3}" : $"{"Skipped",9}";
                Console.WriteLine($"{V,8} | {E,8} | {avgDijkstraMs,9:F3} | {avgAStarMs,9:F3} | {bfMsPrint} | {speedup,7:F2} | {dijkstraEqualsAStar,8} | {dijkstraEqualsBellmanFord,8}");
            }

            Console.WriteLine("==========================================================================================");

            // Write results to file
            CsvWriter.WriteResults(csvOutputPath, results);
            Console.WriteLine($"Benchmark successfully saved to {csvOutputPath}");

            return results;
        }

        private static void WarmUp()
        {
            Console.Write("Performing system JIT warm-up... ");
            var dummyGraph = CityGraphGenerator.Generate(50, 250, 999);
            var dijkstra = new DijkstraSolver();
            var astar = new AStarSolver();
            var bf = new BellmanFordSolver();

            // Run once on each solver
            dijkstra.Solve(dummyGraph, 0, 49);
            astar.Solve(dummyGraph, 0, 49, 100.0);
            bf.Solve(dummyGraph, 0, 49);
            Console.WriteLine("Done.");
        }
    }
}
