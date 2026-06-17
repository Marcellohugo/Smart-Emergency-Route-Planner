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
        private const int Repetitions = 5;

        /// <summary>
        /// Runs the complete advanced benchmark suite across multiple graph families,
        /// calculates timing statistics (Min, Max, Avg) over 5 repetitions, prints
        /// a copy-pasteable report table, and saves to CSV.
        /// </summary>
        public static List<BenchmarkResult> RunAll(string csvOutputPath)
        {
            Console.WriteLine("Initializing Advanced Benchmark Runner...");
            
            // Warm-up to trigger JIT compilation of critical execution paths
            WarmUp();

            var cases = new List<(GraphFamily Family, int V, int E, int Seed)>
            {
                // RandomSparse family (E ≈ 5V)
                (GraphFamily.RandomSparse, 100, 500, 42),
                (GraphFamily.RandomSparse, 500, 2500, 42),
                (GraphFamily.RandomSparse, 1000, 5000, 42),
                (GraphFamily.RandomSparse, 5000, 25000, 42),
                (GraphFamily.RandomSparse, 10000, 50000, 42),

                // GridCity family (E ≈ 10V to simulate dense streets)
                (GraphFamily.GridCity, 100, 1000, 42),
                (GraphFamily.GridCity, 500, 5000, 42),
                (GraphFamily.GridCity, 1000, 10000, 42),
                (GraphFamily.GridCity, 5000, 50000, 42),
                (GraphFamily.GridCity, 10000, 100000, 42)
            };

            var results = new List<BenchmarkResult>();

            Console.WriteLine("\n==========================================================================================");
            Console.WriteLine("                                  RUNNING BENCHMARKS                                      ");
            Console.WriteLine("==========================================================================================");
            Console.WriteLine($"{"Vertices",8} | {"Edges",8} | {"Family",12} | {"DijkAvgMs",9} | {"AStarAvgMs",10} | {"BF-AvgMs",8} | {"Speedup",7} | {"Match A*",8}");
            Console.WriteLine("------------------------------------------------------------------------------------------");

            var dijkstraSolver = new DijkstraSolver();
            var aStarSolver = new AStarSolver();
            var bellmanFordSolver = new BellmanFordSolver();

            foreach (var bCase in cases)
            {
                GraphFamily family = bCase.Family;
                int V = bCase.V;
                int E = bCase.E;
                int seed = bCase.Seed;

                // Generate network instance
                var graph = CityGraphGenerator.Generate(V, E, seed, family);
                int source = 0;
                int target = V - 1;

                // Max speed limit is 100.0 km/h in generator
                double maxSpeed = 100.0;

                // --- 1. Dijkstra Solver ---
                double totalDijkstraMs = 0;
                double minDijkstraMs = double.PositiveInfinity;
                double maxDijkstraMs = double.NegativeInfinity;
                PathResult dijkstraResult = null!;

                for (int r = 0; r < Repetitions; r++)
                {
                    var res = dijkstraSolver.Solve(graph, source, target);
                    if (r == 0) dijkstraResult = res;

                    double ms = res.RuntimeMilliseconds;
                    totalDijkstraMs += ms;
                    if (ms < minDijkstraMs) minDijkstraMs = ms;
                    if (ms > maxDijkstraMs) maxDijkstraMs = ms;
                }
                double avgDijkstraMs = totalDijkstraMs / Repetitions;

                // --- 2. A* Solver ---
                double totalAStarMs = 0;
                double minAStarMs = double.PositiveInfinity;
                double maxAStarMs = double.NegativeInfinity;
                PathResult aStarResult = null!;

                for (int r = 0; r < Repetitions; r++)
                {
                    var res = aStarSolver.Solve(graph, source, target, maxSpeed);
                    if (r == 0) aStarResult = res;

                    double ms = res.RuntimeMilliseconds;
                    totalAStarMs += ms;
                    if (ms < minAStarMs) minAStarMs = ms;
                    if (ms > maxAStarMs) maxAStarMs = ms;
                }
                double avgAStarMs = totalAStarMs / Repetitions;

                // --- 3. Bellman-Ford Solver (Run only for small to medium scale) ---
                double? avgBellmanFordMs = null;
                string bfStatus = "Skipped (O(VE))";

                if (V <= 1000)
                {
                    double totalBellmanFordMs = 0;
                    for (int r = 0; r < Repetitions; r++)
                    {
                        var res = bellmanFordSolver.Solve(graph, source, target);
                        totalBellmanFordMs += res.RuntimeMilliseconds;
                    }
                    avgBellmanFordMs = totalBellmanFordMs / Repetitions;
                    bfStatus = "Completed";
                }

                // Metric Calculations
                double distDiff = Math.Abs(dijkstraResult.TotalTravelTimeMinutes - aStarResult.TotalTravelTimeMinutes);
                bool sameDist = distDiff < 1e-5;
                double speedup = avgAStarMs > 0 ? avgDijkstraMs / avgAStarMs : 0;
                
                double nodeReductionPercent = dijkstraResult.ExpandedNodes > 0
                    ? ((double)(dijkstraResult.ExpandedNodes - aStarResult.ExpandedNodes) / dijkstraResult.ExpandedNodes) * 100.0
                    : 0.0;

                var result = new BenchmarkResult
                {
                    Family = family,
                    VertexCount = V,
                    EdgeCount = E,
                    Seed = seed,
                    
                    DijkstraAvgMs = avgDijkstraMs,
                    DijkstraMinMs = minDijkstraMs,
                    DijkstraMaxMs = maxDijkstraMs,
                    
                    AStarAvgMs = avgAStarMs,
                    AStarMinMs = minAStarMs,
                    AStarMaxMs = maxAStarMs,
                    
                    BellmanFordAvgMs = avgBellmanFordMs,
                    BellmanFordStatus = bfStatus,
                    
                    DijkstraDistance = dijkstraResult.TotalTravelTimeMinutes,
                    AStarDistance = aStarResult.TotalTravelTimeMinutes,
                    
                    DijkstraExpandedNodes = dijkstraResult.ExpandedNodes,
                    AStarExpandedNodes = aStarResult.ExpandedNodes,
                    DijkstraRelaxations = dijkstraResult.RelaxationCount,
                    AStarRelaxations = aStarResult.RelaxationCount,
                    
                    DijkstraPathLength = dijkstraResult.Path.Count,
                    AStarPathLength = aStarResult.Path.Count,
                    
                    SameDistance = sameDist,
                    DistanceDifference = distDiff,
                    AStarSpeedup = speedup,
                    ExpandedNodeReductionPercent = nodeReductionPercent
                };

                results.Add(result);

                // Inline status printing
                string bfMsPrint = avgBellmanFordMs.HasValue ? $"{avgBellmanFordMs.Value,8:F3}" : $"{"Skipped",8}";
                Console.WriteLine($"{V,8} | {E,8} | {family,-12} | {avgDijkstraMs,9:F3} | {avgAStarMs,10:F3} | {bfMsPrint} | {speedup,7:F2} | {sameDist,8}");
            }

            Console.WriteLine("==========================================================================================");

            // Print report table for easy copy-pasting into report
            PrintConsoleReportTable(results);

            // Write results to CSV
            CsvWriter.WriteResults(csvOutputPath, results);
            Console.WriteLine($"\nBenchmark successfully saved to {csvOutputPath}");

            return results;
        }

        private static void PrintConsoleReportTable(List<BenchmarkResult> results)
        {
            Console.WriteLine("\n==========================================================================================================");
            Console.WriteLine("                                     CONSOLIDATED ACADEMIC BENCHMARK REPORT                               ");
            Console.WriteLine("==========================================================================================================");
            Console.WriteLine($"{"Vertices (V)",12} | {"Edges (E)",9} | {"Family",12} | {"Dijk Avg Ms",12} | {"A* Avg Ms",10} | {"Speedup",8} | {"Same Dist",9} | {"A* Exp Reduc",12}");
            Console.WriteLine("----------------------------------------------------------------------------------------------------------");
            foreach (var r in results)
            {
                Console.WriteLine($"{r.VertexCount,12} | {r.EdgeCount,9} | {r.Family,-12} | {r.DijkstraAvgMs,12:F4} | {r.AStarAvgMs,10:F4} | {r.AStarSpeedup,7:F2}x | {r.SameDistance,9} | {r.ExpandedNodeReductionPercent,11:F1}%");
            }
            Console.WriteLine("==========================================================================================================");
        }

        private static void WarmUp()
        {
            Console.Write("Performing JIT warm-up... ");
            var dummyGraph = CityGraphGenerator.Generate(50, 250, 999, GraphFamily.RandomSparse);
            var dijkstra = new DijkstraSolver();
            var astar = new AStarSolver();
            var bf = new BellmanFordSolver();

            // Run once to trigger JIT compilation of solvers
            dijkstra.Solve(dummyGraph, 0, 49);
            astar.Solve(dummyGraph, 0, 49, 100.0);
            bf.Solve(dummyGraph, 0, 49);
            Console.WriteLine("Done.");
        }
    }
}
