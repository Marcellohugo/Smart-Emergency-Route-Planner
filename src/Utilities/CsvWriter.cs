using System.Collections.Generic;
using System.IO;
using SmartEmergencyRoutePlanner.Benchmark;

namespace SmartEmergencyRoutePlanner.Utilities
{
    public static class CsvWriter
    {
        /// <summary>
        /// Writes advanced benchmark results to a CSV file.
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
                // Detailed headers mapping to all new parameters
                writer.WriteLine("GraphFamily,VertexCount,EdgeCount,Seed,DijkstraAvgMs,DijkstraMinMs,DijkstraMaxMs,AStarAvgMs,AStarMinMs,AStarMaxMs,BellmanFordAvgMs,BellmanFordStatus,DijkstraDistance,AStarDistance,DijkstraExpandedNodes,AStarExpandedNodes,DijkstraRelaxations,AStarRelaxations,DijkstraPathLength,AStarPathLength,SameDistance,DistanceDifference,AStarSpeedup,ExpandedNodeReductionPercent");
                
                foreach (var res in results)
                {
                    string bfAvgMsStr = res.BellmanFordAvgMs.HasValue ? res.BellmanFordAvgMs.Value.ToString("F4") : "N/A";

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
                                     $"{bfAvgMsStr}," +
                                     $"{res.BellmanFordStatus}," +
                                     $"{res.DijkstraDistance:F4}," +
                                     $"{res.AStarDistance:F4}," +
                                     $"{res.DijkstraExpandedNodes}," +
                                     $"{res.AStarExpandedNodes}," +
                                     $"{res.DijkstraRelaxations}," +
                                     $"{res.AStarRelaxations}," +
                                     $"{res.DijkstraPathLength}," +
                                     $"{res.AStarPathLength}," +
                                     $"{res.SameDistance.ToString().ToLower()}," +
                                     $"{res.DistanceDifference:F4}," +
                                     $"{res.AStarSpeedup:F4}," +
                                     $"{res.ExpandedNodeReductionPercent:F2}");
                }
            }
        }
    }
}
