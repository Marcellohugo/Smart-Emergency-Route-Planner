using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner.Algorithms
{
    public class AlternativeRouteSolver
    {
        private const double PenaltyMultiplier = 2.0;

        /// <summary>
        /// Finds K alternative routes by running Dijkstra repeatedly and penalizing used edges.
        /// </summary>
        public List<PathResult> FindAlternativeRoutes(Graph graph, int source, int target, int k = 3, bool emergencyMode = false)
        {
            var results = new List<PathResult>();
            var dijkstra = new DijkstraSolver();

            // 1. Find the primary shortest route
            var primaryResult = dijkstra.Solve(graph, source, target, emergencyMode);
            primaryResult.AlgorithmName = "Primary Route (Fastest)";
            results.Add(primaryResult);

            if (!primaryResult.IsReachable || primaryResult.Path.Count == 0)
            {
                return results;
            }

            var penalizedEdges = new Dictionary<(int, int), double>();

            // 2. Iteratively find alternative routes
            for (int step = 2; step <= k; step++)
            {
                // Penalize all edges from previously found paths
                foreach (var res in results)
                {
                    if (res.IsReachable && res.Path.Count > 1)
                    {
                        for (int i = 0; i < res.Path.Count - 1; i++)
                        {
                            int u = res.Path[i];
                            int v = res.Path[i + 1];
                            penalizedEdges[(u, v)] = PenaltyMultiplier;
                        }
                    }
                }

                // Run Dijkstra with penalties
                var altResult = dijkstra.Solve(graph, source, target, emergencyMode, penalizedEdges);
                altResult.AlgorithmName = step == 2 ? "Alternative Route (Backup)" : $"Route {step} (Alternative)";

                if (altResult.IsReachable && altResult.Path.Count > 0)
                {
                    results.Add(altResult);
                }
                else
                {
                    break; // No more paths reachable
                }
            }

            return results;
        }

        /// <summary>
        /// Calculates the percentage of edge overlap between routeB and primaryRoute (routeA).
        /// </summary>
        public static double CalculatePathOverlap(List<int> routeA, List<int> routeB)
        {
            if (routeA == null || routeB == null || routeA.Count < 2 || routeB.Count < 2)
            {
                return 0.0;
            }

            var edgesA = new HashSet<(int, int)>();
            for (int i = 0; i < routeA.Count - 1; i++)
            {
                edgesA.Add((routeA[i], routeA[i + 1]));
            }

            int overlapCount = 0;
            for (int i = 0; i < routeB.Count - 1; i++)
            {
                if (edgesA.Contains((routeB[i], routeB[i + 1])))
                {
                    overlapCount++;
                }
            }

            return ((double)overlapCount / (routeB.Count - 1)) * 100.0;
        }
    }
}
