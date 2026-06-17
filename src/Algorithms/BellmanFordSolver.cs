using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner.Algorithms
{
    public class BellmanFordSolver
    {
        /// <summary>
        /// Solves the shortest path problem using the Bellman-Ford algorithm.
        /// </summary>
        public PathResult Solve(Graph graph, int source, int target, bool emergencyMode = false)
        {
            var stopwatch = Stopwatch.StartNew();

            int n = graph.VertexCount;
            double[] dist = new double[n];
            int[] prev = new int[n];

            for (int i = 0; i < n; i++)
            {
                dist[i] = double.PositiveInfinity;
                prev[i] = -1;
            }

            dist[source] = 0;
            bool hasNegativeCycle = false;
            long relaxationCount = 0;

            // Relax edges V - 1 times
            for (int i = 1; i <= n - 1; i++)
            {
                bool relaxedAny = false;
                foreach (var edge in graph.AllEdges)
                {
                    if (edge.IsClosed) continue;

                    relaxationCount++;
                    int u = edge.From;
                    int v = edge.To;
                    double weight = edge.GetWeight(emergencyMode);

                    if (dist[u] < double.PositiveInfinity && dist[u] + weight < dist[v])
                    {
                        dist[v] = dist[u] + weight;
                        prev[v] = u;
                        relaxedAny = true;
                    }
                }

                // Early stop if no edge was relaxed in this iteration
                if (!relaxedAny)
                {
                    break;
                }
            }

            // Check for negative weight cycles
            foreach (var edge in graph.AllEdges)
            {
                if (edge.IsClosed) continue;

                relaxationCount++;
                int u = edge.From;
                int v = edge.To;
                double weight = edge.GetWeight(emergencyMode);

                if (dist[u] < double.PositiveInfinity && dist[u] + weight < dist[v])
                {
                    hasNegativeCycle = true;
                    break;
                }
            }

            stopwatch.Stop();

            bool isReachable = dist[target] < double.PositiveInfinity && !hasNegativeCycle;

            var result = new PathResult
            {
                AlgorithmName = "Bellman-Ford",
                IsReachable = isReachable,
                RuntimeTicks = stopwatch.ElapsedTicks,
                RuntimeMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                ExpandedNodes = 0,
                RelaxationCount = relaxationCount,
                HasNegativeCycle = hasNegativeCycle,
                Notes = hasNegativeCycle ? "Negative cycle detected!" : (isReachable ? "Optimal path found." : "Target unreachable.")
            };

            if (isReachable)
            {
                var path = new List<int>();
                int curr = target;
                while (curr != -1)
                {
                    path.Add(curr);
                    curr = prev[curr];
                }
                path.Reverse();
                result.Path = path;
                result.TotalTravelTimeMinutes = dist[target];
            }
            else
            {
                result.TotalTravelTimeMinutes = -1;
                result.Path = new List<int>();
            }

            return result;
        }
    }
}
