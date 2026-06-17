using System.Collections.Generic;
using System.IO;
using SmartEmergencyRoutePlanner.Benchmark;

namespace SmartEmergencyRoutePlanner.Utilities
{
    public static class CsvWriter
    {
        /// <summary>
        /// Writes benchmark results to a CSV file.
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
                // CSV header row
                writer.WriteLine("VertexCount,EdgeCount,Seed,DijkstraMs,AStarMs,BellmanFordMs,DijkstraDistance,AStarDistance,BellmanFordDistance,DijkstraExpandedNodes,AStarExpandedNodes,DijkstraPathLength,AStarPathLength,DijkstraEqualsAStar,DijkstraEqualsBellmanFord,BellmanFordStatus,AStarSpeedup");
                
                foreach (var res in results)
                {
                    string bfMsStr = res.BellmanFordMs.HasValue ? res.BellmanFordMs.Value.ToString("F4") : "N/A";
                    string bfDistStr = res.BellmanFordDistance.HasValue ? res.BellmanFordDistance.Value.ToString("F4") : "N/A";

                    writer.WriteLine($"{res.VertexCount}," +
                                     $"{res.EdgeCount}," +
                                     $"{res.Seed}," +
                                     $"{res.DijkstraMs:F4}," +
                                     $"{res.AStarMs:F4}," +
                                     $"{bfMsStr}," +
                                     $"{res.DijkstraDistance:F4}," +
                                     $"{res.AStarDistance:F4}," +
                                     $"{bfDistStr}," +
                                     $"{res.DijkstraExpandedNodes}," +
                                     $"{res.AStarExpandedNodes}," +
                                     $"{res.DijkstraPathLength}," +
                                     $"{res.AStarPathLength}," +
                                     $"{res.DijkstraEqualsAStar.ToString().ToLower()}," +
                                     $"{res.DijkstraEqualsBellmanFord.ToString().ToLower()}," +
                                     $"{res.BellmanFordStatus}," +
                                     $"{res.AStarSpeedup:F4}");
                }
            }
        }
    }
}
