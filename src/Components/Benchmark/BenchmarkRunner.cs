using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SmartEmergencyRoutePlanner.Algorithms;
using SmartEmergencyRoutePlanner.Analysis;
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
        /// profiling time (min, max, avg), GC memory allocations, and quality checks.
        /// </summary>
        public static List<BenchmarkResult> RunAll(string csvOutputPath)
        {
            Console.WriteLine("Initializing Advanced Benchmark Runner (Phase 2)...");
            WarmUp();

            var cases = new List<(GraphFamily Family, int V, int E, int Seed)>
            {
                // RandomSparse family (E ≈ 5V)
                (GraphFamily.RandomSparse, 100, 500, 42),
                (GraphFamily.RandomSparse, 500, 2500, 42),
                (GraphFamily.RandomSparse, 1000, 5000, 42),
                (GraphFamily.RandomSparse, 5000, 25000, 42),
                (GraphFamily.RandomSparse, 10000, 50000, 42),

                // GridCity family (E ≈ 10V)
                (GraphFamily.GridCity, 100, 1000, 42),
                (GraphFamily.GridCity, 500, 5000, 42),
                (GraphFamily.GridCity, 1000, 10000, 42),
                (GraphFamily.GridCity, 5000, 50000, 42),
                (GraphFamily.GridCity, 10000, 100000, 42)
            };

            var results = new List<BenchmarkResult>();

            Console.WriteLine("\n===========================================================================================================");
            Console.WriteLine("                                           RUNNING PHASE 2 BENCHMARKS                                      ");
            Console.WriteLine("===========================================================================================================");
            Console.WriteLine($"{"Vertices",8} | {"Edges",8} | {"Family",12} | {"DijkAvg",8} | {"AStarAvg",8} | {"BiDijkAvg",9} | {"BF-Avg",8} | {"A*Speed",7} | {"BiDijkSp",8}");
            Console.WriteLine("-----------------------------------------------------------------------------------------------------------");

            var dijkstra = new DijkstraSolver();
            var astar = new AStarSolver();
            var bidijkstra = new BidirectionalDijkstraSolver();
            var bellmanford = new BellmanFordSolver();

            foreach (var bCase in cases)
            {
                GraphFamily family = bCase.Family;
                int V = bCase.V;
                int E = bCase.E;
                int seed = bCase.Seed;

                var graph = CityGraphGenerator.Generate(V, E, seed, family);
                int source = 0;
                int target = V - 1;

                // --- 1. Dijkstra Timing & Memory ---
                double totalDMs = 0, minDMs = double.PositiveInfinity, maxDMs = double.NegativeInfinity;
                long dijkMemory = 0;
                PathResult resD = null!;

                for (int r = 0; r < Repetitions; r++)
                {
                    if (r == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        long memBefore = GC.GetTotalMemory(true);
                        resD = dijkstra.Solve(graph, source, target);
                        long memAfter = GC.GetTotalMemory(true);
                        dijkMemory = Math.Max(0, memAfter - memBefore);
                    }
                    else
                    {
                        resD = dijkstra.Solve(graph, source, target);
                    }

                    double ms = resD.RuntimeMilliseconds;
                    totalDMs += ms;
                    if (ms < minDMs) minDMs = ms;
                    if (ms > maxDMs) maxDMs = ms;
                }
                double avgDMs = totalDMs / Repetitions;

                // --- 2. A* Timing & Memory ---
                double totalAMs = 0, minAMs = double.PositiveInfinity, maxAMs = double.NegativeInfinity;
                long astarMemory = 0;
                PathResult resA = null!;

                for (int r = 0; r < Repetitions; r++)
                {
                    if (r == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        long memBefore = GC.GetTotalMemory(true);
                        resA = astar.Solve(graph, source, target, 100.0);
                        long memAfter = GC.GetTotalMemory(true);
                        astarMemory = Math.Max(0, memAfter - memBefore);
                    }
                    else
                    {
                        resA = astar.Solve(graph, source, target, 100.0);
                    }

                    double ms = resA.RuntimeMilliseconds;
                    totalAMs += ms;
                    if (ms < minAMs) minAMs = ms;
                    if (ms > maxAMs) maxAMs = ms;
                }
                double avgAMs = totalAMs / Repetitions;

                // --- 3. Bidirectional Dijkstra Timing & Memory ---
                double totalBiMs = 0, minBiMs = double.PositiveInfinity, maxBiMs = double.NegativeInfinity;
                long bidijkMemory = 0;
                PathResult resBi = null!;

                for (int r = 0; r < Repetitions; r++)
                {
                    if (r == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        long memBefore = GC.GetTotalMemory(true);
                        resBi = bidijkstra.Solve(graph, source, target);
                        long memAfter = GC.GetTotalMemory(true);
                        bidijkMemory = Math.Max(0, memAfter - memBefore);
                    }
                    else
                    {
                        resBi = bidijkstra.Solve(graph, source, target);
                    }

                    double ms = resBi.RuntimeMilliseconds;
                    totalBiMs += ms;
                    if (ms < minBiMs) minBiMs = ms;
                    if (ms > maxBiMs) maxBiMs = ms;
                }
                double avgBiMs = totalBiMs / Repetitions;

                // --- 4. Bellman-Ford Timing & Memory ---
                double? avgBFMs = null, minBFMs = null, maxBFMs = null;
                double? bfDist = null;
                long bfMemory = 0;
                string bfStatus = "Skipped (O(VE))";

                if (V <= 1000)
                {
                    double totalBFMs = 0;
                    double minBF = double.PositiveInfinity;
                    double maxBF = double.NegativeInfinity;
                    PathResult resBF = null!;

                    for (int r = 0; r < Repetitions; r++)
                    {
                        if (r == 0)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            long memBefore = GC.GetTotalMemory(true);
                            resBF = bellmanford.Solve(graph, source, target);
                            long memAfter = GC.GetTotalMemory(true);
                            bfMemory = Math.Max(0, memAfter - memBefore);
                            bfDist = resBF.TotalTravelTimeMinutes;
                        }
                        else
                        {
                            resBF = bellmanford.Solve(graph, source, target);
                        }

                        double ms = resBF.RuntimeMilliseconds;
                        totalBFMs += ms;
                        if (ms < minBF) minBF = ms;
                        if (ms > maxBF) maxBF = ms;
                    }
                    avgBFMs = totalBFMs / Repetitions;
                    minBFMs = minBF;
                    maxBFMs = maxBF;
                    bfStatus = "Completed";
                }

                // Metric Calculations
                double distDiff = Math.Abs(resD.TotalTravelTimeMinutes - resA.TotalTravelTimeMinutes);
                bool sameDist = distDiff < 1e-5;
                bool biEqualsDijk = Math.Abs(resD.TotalTravelTimeMinutes - resBi.TotalTravelTimeMinutes) < 1e-5;

                double aStarSpeedup = avgAMs > 0 ? avgDMs / avgAMs : 0;
                double biDijkSpeedup = avgBiMs > 0 ? avgDMs / avgBiMs : 0;
                double nodeReductionPercent = resD.ExpandedNodes > 0
                    ? ((double)(resD.ExpandedNodes - resA.ExpandedNodes) / resD.ExpandedNodes) * 100.0
                    : 0.0;

                var result = new BenchmarkResult
                {
                    Family = family,
                    VertexCount = V,
                    EdgeCount = E,
                    Seed = seed,

                    DijkstraAvgMs = avgDMs,
                    DijkstraMinMs = minDMs,
                    DijkstraMaxMs = maxDMs,

                    AStarAvgMs = avgAMs,
                    AStarMinMs = minAMs,
                    AStarMaxMs = maxAMs,

                    BiDijkstraAvgMs = avgBiMs,
                    BiDijkstraMinMs = minBiMs,
                    BiDijkstraMaxMs = maxBiMs,

                    BellmanFordAvgMs = avgBFMs,
                    BellmanFordMinMs = minBFMs,
                    BellmanFordMaxMs = maxBFMs,
                    BellmanFordStatus = bfStatus,

                    DijkstraDistance = resD.TotalTravelTimeMinutes,
                    AStarDistance = resA.TotalTravelTimeMinutes,
                    BiDijkstraDistance = resBi.TotalTravelTimeMinutes,
                    BellmanFordDistance = bfDist,

                    DijkstraExpandedNodes = resD.ExpandedNodes,
                    AStarExpandedNodes = resA.ExpandedNodes,
                    BiDijkstraExpandedNodes = resBi.ExpandedNodes,

                    DijkstraRelaxations = resD.RelaxationCount,
                    AStarRelaxations = resA.RelaxationCount,
                    BiDijkstraRelaxations = resBi.RelaxationCount,

                    DijkstraPathLength = resD.Path.Count,
                    AStarPathLength = resA.Path.Count,
                    BiDijkstraPathLength = resBi.Path.Count,

                    SameDistance = sameDist,
                    DistanceDifference = distDiff,
                    BiDijkstraEqualsDijkstra = biEqualsDijk,
                    AStarSpeedup = aStarSpeedup,
                    BiDijkstraSpeedup = biDijkSpeedup,
                    ExpandedNodeReductionPercent = nodeReductionPercent,

                    DijkstraMemoryBytes = dijkMemory,
                    AStarMemoryBytes = astarMemory,
                    BiDijkstraMemoryBytes = bidijkMemory,
                    BellmanFordMemoryBytes = bfMemory
                };

                results.Add(result);

                // Print inline result
                string bfMsPrint = avgBFMs.HasValue ? $"{avgBFMs.Value,8:F3}" : $"{"Skipped",8}";
                Console.WriteLine($"{V,8} | {E,8} | {family,-12} | {avgDMs,8:F3} | {avgAMs,8:F3} | {avgBiMs,9:F3} | {bfMsPrint} | {aStarSpeedup,7:F2} | {biDijkSpeedup,8:F2}");
            }

            Console.WriteLine("===========================================================================================================");

            // Print Consolidated copy-paste report table
            PrintConsoleReportTable(results);

            // Compute and display Empirical Growth Exponents
            CalculateAndPrintExponents(results);

            // Save results to CSV
            CsvWriter.WriteResults(csvOutputPath, results);
            Console.WriteLine($"\nBenchmark successfully saved to {csvOutputPath}");

            return results;
        }

        private static void PrintConsoleReportTable(List<BenchmarkResult> results)
        {
            Console.WriteLine("\n============================================================================================================");
            Console.WriteLine("                                      CONSOLIDATED ACADEMIC BENCHMARK REPORT                                ");
            Console.WriteLine("============================================================================================================");
            Console.WriteLine($"{"Vertices (V)",12} | {"Edges (E)",9} | {"Family",12} | {"Dijk Avg Ms",12} | {"A* Avg Ms",10} | {"BiDijk Avg",10} | {"A*Speed",7} | {"BiDijkSp",8} | {"A*ExpRed",8}");
            Console.WriteLine("------------------------------------------------------------------------------------------------------------");
            foreach (var r in results)
            {
                Console.WriteLine($"{r.VertexCount,12} | {r.EdgeCount,9} | {r.Family,-12} | {r.DijkstraAvgMs,12:F4} | {r.AStarAvgMs,10:F4} | {r.BiDijkstraAvgMs,10:F4} | {r.AStarSpeedup,6:F2}x | {r.BiDijkstraSpeedup,7:F2}x | {r.ExpandedNodeReductionPercent,7:F1}%");
            }
            Console.WriteLine("============================================================================================================");
        }

        private static void CalculateAndPrintExponents(List<BenchmarkResult> results)
        {
            // Group cases by family for separate exponents
            var families = new[] { GraphFamily.RandomSparse, GraphFamily.GridCity };

            Console.WriteLine("\n==================================================");
            Console.WriteLine("            EMPIRICAL GROWTH EXPONENTS            ");
            Console.WriteLine("               (Model: time ~ a*V^b)              ");
            Console.WriteLine("==================================================");

            foreach (var family in families)
            {
                var vList = new List<int>();
                var dijkTimes = new List<double>();
                var astarTimes = new List<double>();
                var bidijkTimes = new List<double>();
                var bfTimes = new List<double>();

                foreach (var r in results)
                {
                    if (r.Family == family)
                    {
                        vList.Add(r.VertexCount);
                        dijkTimes.Add(r.DijkstraAvgMs);
                        astarTimes.Add(r.AStarAvgMs);
                        bidijkTimes.Add(r.BiDijkstraAvgMs);
                        if (r.BellmanFordAvgMs.HasValue)
                        {
                            bfTimes.Add(r.BellmanFordAvgMs.Value);
                        }
                    }
                }

                double dijkExp = EmpiricalGrowthAnalyzer.EstimateExponent(vList, dijkTimes);
                double astarExp = EmpiricalGrowthAnalyzer.EstimateExponent(vList, astarTimes);
                double bidijkExp = EmpiricalGrowthAnalyzer.EstimateExponent(vList, bidijkTimes);

                Console.WriteLine($"Family: {family}");
                Console.WriteLine($"  * Dijkstra Exponent (b)       : {dijkExp:F4} (Theoretical: O(V log V) ~ 1.0 - 1.1)");
                Console.WriteLine($"  * A* Search Exponent (b)      : {astarExp:F4}");
                Console.WriteLine($"  * Bidirectional Dijk Exp (b) : {bidijkExp:F4}");

                if (bfTimes.Count >= 2)
                {
                    var bfVList = vList.GetRange(0, bfTimes.Count);
                    double bfExp = EmpiricalGrowthAnalyzer.EstimateExponent(bfVList, bfTimes);
                    Console.WriteLine($"  * Bellman-Ford Exponent (b)   : {bfExp:F4} (Theoretical: O(VE) ~ O(V^2) ~ 2.0)");
                }
                else
                {
                    Console.WriteLine($"  * Bellman-Ford Exponent (b)   : Insufficient data (V > 1000 skipped)");
                }
                Console.WriteLine();
            }
            Console.WriteLine("==================================================");
        }

        /// <summary>
        /// Runs scenario comparison matrix, saving to bench/scenario_results.csv and printing table.
        /// </summary>
        public static void RunScenarioMatrix(string matrixCsvPath)
        {
            Console.WriteLine("\nRunning Scenario Comparison Matrix Experiment...");

            int V = 1000;
            int E = 10000;
            int seed = 42;
            int source = 0;
            int target = V - 1;

            var graph = CityGraphGenerator.Generate(V, E, seed, GraphFamily.GridCity);

            var dijkstra = new DijkstraSolver();
            var astar = new AStarSolver();
            var robust = new RobustRouteSolver();

            var scenarioOutputs = new List<(string Name, string Algo, PathResult Result)>();

            // Scenario 1: Normal
            graph.ResetClosures();
            graph.ResetTraffic();
            scenarioOutputs.Add(("1. Normal Condition", "Dijkstra", dijkstra.Solve(graph, source, target)));
            scenarioOutputs.Add(("1. Normal Condition", "A* Search", astar.Solve(graph, source, target, 100.0)));

            // Scenario 2: 5% Closures
            graph.ResetClosures();
            graph.ResetTraffic();
            graph.CloseRandomEdges(0.05, seed);
            scenarioOutputs.Add(("2. 5% Road Closure", "Dijkstra", dijkstra.Solve(graph, source, target)));
            scenarioOutputs.Add(("2. 5% Road Closure", "A* Search", astar.Solve(graph, source, target, 100.0)));

            // Scenario 3: 10% Closures
            graph.ResetClosures();
            graph.ResetTraffic();
            graph.CloseRandomEdges(0.10, seed);
            scenarioOutputs.Add(("3. 10% Road Closure", "Dijkstra", dijkstra.Solve(graph, source, target)));
            scenarioOutputs.Add(("3. 10% Road Closure", "A* Search", astar.Solve(graph, source, target, 100.0)));

            // Scenario 4: High Traffic (constant 1.5x on all edges)
            graph.ResetClosures();
            graph.ResetTraffic();
            foreach (var edge in graph.AllEdges)
            {
                edge.Traffic = TrafficLevel.High;
                edge.TrafficMultiplier = 1.5;
            }
            scenarioOutputs.Add(("4. High Traffic", "Dijkstra", dijkstra.Solve(graph, source, target)));
            scenarioOutputs.Add(("4. High Traffic", "A* Search", astar.Solve(graph, source, target, 100.0)));

            // Scenario 5: Severe Traffic (constant 2.5x on all edges)
            graph.ResetClosures();
            graph.ResetTraffic();
            foreach (var edge in graph.AllEdges)
            {
                edge.Traffic = TrafficLevel.Severe;
                edge.TrafficMultiplier = 2.5;
            }
            scenarioOutputs.Add(("5. Severe Traffic", "Dijkstra", dijkstra.Solve(graph, source, target)));
            scenarioOutputs.Add(("5. Severe Traffic", "A* Search", astar.Solve(graph, source, target, 100.0)));

            // Scenario 6: Emergency priority lane
            // Reset traffic, enable priority lane factor
            graph.ResetClosures();
            graph.ResetTraffic();
            // Assign emergency lanes on grid cells
            var random = new Random(seed);
            foreach (var edge in graph.AllEdges)
            {
                edge.HasEmergencyLane = random.NextDouble() < 0.35; // 35% priority lanes
            }
            scenarioOutputs.Add(("6. Emergency priority lane", "Dijkstra", dijkstra.Solve(graph, source, target, true)));
            scenarioOutputs.Add(("6. Emergency priority lane", "A* Search", astar.Solve(graph, source, target, 100.0, true)));

            // Scenario 7: Robust Route Mode (penalizing risk, lambda = 10)
            graph.ResetClosures();
            graph.ResetTraffic();
            // Generate risks
            foreach (var edge in graph.AllEdges)
            {
                edge.ClosureRisk = random.NextDouble() * 0.1;
                edge.TrafficRisk = random.NextDouble() * 0.3;
            }
            scenarioOutputs.Add(("7. Robust Route Mode", "Robust Dijk", robust.Solve(graph, source, target, false, 10.0)));

            // Cleanup graph
            graph.ResetClosures();
            graph.ResetTraffic();

            // Write Scenario CSV
            using (var writer = new StreamWriter(matrixCsvPath, false))
            {
                writer.WriteLine("ScenarioName,VertexCount,EdgeCount,Algorithm,TravelTime,RuntimeMs,ExpandedNodes,PathLength,Reachable");
                foreach (var sc in scenarioOutputs)
                {
                    int pathLen = sc.Result.IsReachable && sc.Result.Path.Count > 0 ? sc.Result.Path.Count - 1 : 0;
                    writer.WriteLine($"{sc.Name},{V},{E},{sc.Algo},{sc.Result.TotalTravelTimeMinutes:F4},{sc.Result.RuntimeMilliseconds:F4},{sc.Result.ExpandedNodes},{pathLen},{sc.Result.IsReachable.ToString().ToLower()}");
                }
            }

            // Print Console Summary Table
            Console.WriteLine("\n==========================================================================================================");
            Console.WriteLine("                                       SCENARIO COMPARISON MATRIX                                         ");
            Console.WriteLine("==========================================================================================================");
            Console.WriteLine($"{"Scenario",30} | {"Algorithm",12} | {"Travel Time",12} | {"Runtime Ms",10} | {"Exp Nodes",9} | {"Path Len",8} | {"Reachable",9}");
            Console.WriteLine("----------------------------------------------------------------------------------------------------------");
            foreach (var sc in scenarioOutputs)
            {
                int pathLen = sc.Result.IsReachable && sc.Result.Path.Count > 0 ? sc.Result.Path.Count - 1 : 0;
                string distStr = sc.Result.IsReachable ? $"{sc.Result.TotalTravelTimeMinutes,10:F2} min" : $"{"N/A",10}";
                Console.WriteLine($"{sc.Name,30} | {sc.Algo,12} | {distStr} | {sc.Result.RuntimeMilliseconds,10:F4} | {sc.Result.ExpandedNodes,9} | {pathLen,8} | {sc.Result.IsReachable,9}");
            }
            Console.WriteLine("==========================================================================================================");

            // Display key findings
            Console.WriteLine("\nKEY INSIGHTS & FINDINGS:");
            Console.WriteLine("1. Road closures force the solvers to route around intersections, increasing travel times.");
            Console.WriteLine("2. Constant traffic multipliers (High/Severe) scale travel time uniformly without changing path structure.");
            Console.WriteLine("3. Emergency Priority Lanes cut travel times significantly (40% discount) on designated pathways.");
            Console.WriteLine("4. Robust Route mode trades off speed (longer travel times) for safety (avoiding high closure/congestion risk edges).");
            Console.WriteLine($"Scenario matrix saved to {matrixCsvPath}");
        }

        private static void WarmUp()
        {
            Console.Write("Performing JIT warm-up... ");
            var dummyGraph = CityGraphGenerator.Generate(50, 250, 999, GraphFamily.RandomSparse);
            var dijkstra = new DijkstraSolver();
            var astar = new AStarSolver();
            var bidijk = new BidirectionalDijkstraSolver();
            var bf = new BellmanFordSolver();
            var robust = new RobustRouteSolver();

            dijkstra.Solve(dummyGraph, 0, 49);
            astar.Solve(dummyGraph, 0, 49, 100.0);
            bidijk.Solve(dummyGraph, 0, 49);
            bf.Solve(dummyGraph, 0, 49);
            robust.Solve(dummyGraph, 0, 49);
            Console.WriteLine("Done.");
        }
    }
}
