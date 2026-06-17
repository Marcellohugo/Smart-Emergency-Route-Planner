using System.Collections.Generic;
using System.IO;
using SmartEmergencyRoutePlanner.Benchmark;

namespace SmartEmergencyRoutePlanner.Utilities
{
    public static class CsvWriter
    {
        /// <summary>
        /// Writes advanced phase 2 benchmark results to a CSV file.
        /// </summary>
        public static void WriteResults(string filePath, List<BenchmarkResult> results)
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var writer = new StreamWriter(filePath, false))
            {
                // Write detailed headers mapping to all new parameters including memory
                writer.WriteLine("GraphFamily,VertexCount,EdgeCount,Seed," +
                                 "DijkstraAvgMs,DijkstraMinMs,DijkstraMaxMs," +
                                 "AStarAvgMs,AStarMinMs,AStarMaxMs," +
                                 "BiDijkstraAvgMs,BiDijkstraMinMs,BiDijkstraMaxMs," +
                                 "BellmanFordAvgMs,BellmanFordMinMs,BellmanFordMaxMs,BellmanFordStatus," +
                                 "DijkstraDistance,AStarDistance,BiDijkstraDistance,BellmanFordDistance," +
                                 "DijkstraExpandedNodes,AStarExpandedNodes,BiDijkstraExpandedNodes," +
                                 "DijkstraRelaxations,AStarRelaxations,BiDijkstraRelaxations," +
                                 "DijkstraPathLength,AStarPathLength,BiDijkstraPathLength," +
                                 "SameDistance,DistanceDifference,BiDijkstraEqualsDijkstra," +
                                 "AStarSpeedup,BiDijkstraSpeedup,ExpandedNodeReductionPercent," +
                                 "DijkstraMemoryBytes,AStarMemoryBytes,BiDijkstraMemoryBytes,BellmanFordMemoryBytes");
                
                foreach (var res in results)
                {
                    string bfAvgMsStr = res.BellmanFordAvgMs.HasValue ? res.BellmanFordAvgMs.Value.ToString("F4") : "N/A";
                    string bfMinMsStr = res.BellmanFordMinMs.HasValue ? res.BellmanFordMinMs.Value.ToString("F4") : "N/A";
                    string bfMaxMsStr = res.BellmanFordMaxMs.HasValue ? res.BellmanFordMaxMs.Value.ToString("F4") : "N/A";
                    string bfDistStr = res.BellmanFordDistance.HasValue ? res.BellmanFordDistance.Value.ToString("F4") : "N/A";

                    writer.WriteLine($"{res.Family}," +
                                     $"{res.VertexCount}," +
                                     $"{res.EdgeCount}," +
                                     $"{res.Seed}," +
                                     $"{res.DijkstraAvgMs:F4}," +
                                     $"{res.DijkstraMinMs:F4}," +
                                     $"{res.DijkstraMaxMs:F4}," +
                                     $"{res.AStarAvgMs:F4}," +
                                     $"{res.AStarMinMs:F4}," +
                                     $"{res.AStarMaxMs:F4}," +
                                     $"{res.BiDijkstraAvgMs:F4}," +
                                     $"{res.BiDijkstraMinMs:F4}," +
                                     $"{res.BiDijkstraMaxMs:F4}," +
                                     $"{bfAvgMsStr}," +
                                     $"{bfMinMsStr}," +
                                     $"{bfMaxMsStr}," +
                                     $"{res.BellmanFordStatus}," +
                                     $"{res.DijkstraDistance:F4}," +
                                     $"{res.AStarDistance:F4}," +
                                     $"{res.BiDijkstraDistance:F4}," +
                                     $"{bfDistStr}," +
                                     $"{res.DijkstraExpandedNodes}," +
                                     $"{res.AStarExpandedNodes}," +
                                     $"{res.BiDijkstraExpandedNodes}," +
                                     $"{res.DijkstraRelaxations}," +
                                     $"{res.AStarRelaxations}," +
                                     $"{res.BiDijkstraRelaxations}," +
                                     $"{res.DijkstraPathLength}," +
                                     $"{res.AStarPathLength}," +
                                     $"{res.BiDijkstraPathLength}," +
                                     $"{res.SameDistance.ToString().ToLower()}," +
                                     $"{res.DistanceDifference:F4}," +
                                     $"{res.BiDijkstraEqualsDijkstra.ToString().ToLower()}," +
                                     $"{res.AStarSpeedup:F4}," +
                                     $"{res.BiDijkstraSpeedup:F4}," +
                                     $"{res.ExpandedNodeReductionPercent:F2}," +
                                     $"{res.DijkstraMemoryBytes}," +
                                     $"{res.AStarMemoryBytes}," +
                                     $"{res.BiDijkstraMemoryBytes}," +
                                     $"{res.BellmanFordMemoryBytes}");
                }
            }
        }
    }
}
