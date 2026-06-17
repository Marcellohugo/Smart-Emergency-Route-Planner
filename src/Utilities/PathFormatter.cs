using System.Collections.Generic;
using System.Text;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner.Utilities
{
    public static class PathFormatter
    {
        /// <summary>
        /// Formats a list of vertex IDs into a readable path representation.
        /// </summary>
        public static string Format(List<int> path)
        {
            if (path == null || path.Count == 0)
            {
                return "Unreachable / No Path";
            }
            return string.Join(" -> ", path);
        }

        /// <summary>
        /// Generates a detailed explanation of the routing path result.
        /// </summary>
        public static string FormatDetailed(PathResult result, int source, int target)
        {
            var sb = new StringBuilder();
            int edgeCount = result.IsReachable && result.Path.Count > 0 ? result.Path.Count - 1 : 0;
            
            sb.AppendLine($"Algorithm: {result.AlgorithmName}");
            sb.AppendLine($"Source: {source}");
            sb.AppendLine($"Target: {target}");
            sb.AppendLine($"Total travel time: {(result.IsReachable ? $"{result.TotalTravelTimeMinutes:F2} minutes" : "N/A")}");
            sb.AppendLine($"Path length: {edgeCount} edges");
            sb.AppendLine($"Expanded nodes: {result.ExpandedNodes}");
            sb.AppendLine($"Relaxation count: {result.RelaxationCount}");
            sb.AppendLine($"Runtime: {result.RuntimeMilliseconds:F2} ms");
            sb.AppendLine($"Path: {Format(result.Path)}");
            
            return sb.ToString();
        }
    }
}
