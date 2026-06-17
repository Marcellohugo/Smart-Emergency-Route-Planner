using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.DataStructures;

namespace SmartEmergencyRoutePlanner.Algorithms
{
    public class DijkstraSolver
    {
        /// <summary>
        /// Solves the shortest path problem using Dijkstra's algorithm.
        /// </summary>
        public PathResult Solve(Graph graph, int source, int target)
        {
            var stopwatch = Stopwatch.StartNew();

            int n = graph.VertexCount;
            double[] dist = new double[n];
            int[] prev = new int[n];
            bool[] visited = new bool[n];

            for (int i = 0; i < n; i++)
            {
                dist[i] = double.PositiveInfinity;
                prev[i] = -1;
            }

            dist[source] = 0;
            var heap = new BinaryMinHeap();
            heap.Insert(source, 0);

            int expandedNodes = 0;
            bool reached = false;

            while (!heap.IsEmpty)
            {
                var minNode = heap.ExtractMin();
                int u = minNode.VertexId;
                double currentDist = minNode.Priority;

                // Lazy deletion of stale elements
                if (currentDist > dist[u])
                {
                    continue;
                }

                expandedNodes++;

                if (u == target)
                {
                    reached = true;
                    break;
                }

                visited[u] = true;

                foreach (var edge in graph.GetNeighbors(u))
                {
                    int v = edge.To;
                    if (visited[v]) continue;

                    double newDist = dist[u] + edge.TravelTimeMinutes;
                    if (newDist < dist[v])
                    {
                        dist[v] = newDist;
                        prev[v] = u;
                        heap.Insert(v, newDist);
                    }
                }
            }

            stopwatch.Stop();

            bool isReachable = reached || (dist[target] < double.PositiveInfinity);

            var result = new PathResult
            {
                AlgorithmName = "Dijkstra",
                IsReachable = isReachable,
                RuntimeTicks = stopwatch.ElapsedTicks,
                RuntimeMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                ExpandedNodes = expandedNodes,
                HasNegativeCycle = false,
                Notes = isReachable ? "Optimal path found." : "Target unreachable."
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
