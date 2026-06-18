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
        public PathResult Solve(Graph graph, int source, int target, bool emergencyMode = false, Dictionary<(int, int), double>? edgePenalties = null)
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
            var expandedList = new List<int>();
            long relaxationCount = 0;
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
                expandedList.Add(u);

                if (u == target)
                {
                    reached = true;
                    break;
                }

                visited[u] = true;

                foreach (var edge in graph.GetNeighbors(u))
                {
                    if (edge.IsClosed) continue;

                    relaxationCount++;
                    int v = edge.To;
                    if (visited[v]) continue;

                    double weight = edge.GetWeight(emergencyMode);
                    if (edgePenalties != null && edgePenalties.TryGetValue((edge.From, edge.To), out double penalty))
                    {
                        weight *= penalty;
                    }

                    double newDist = dist[u] + weight;
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
                ExpandedNodesList = expandedList,
                RelaxationCount = relaxationCount,
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

                // Re-calculate the actual unpenalized travel time along the path
                double totalTime = 0;
                for (int i = 0; i < path.Count - 1; i++)
                {
                    int uFrom = path[i];
                    int uTo = path[i + 1];
                    foreach (var edge in graph.GetNeighbors(uFrom))
                    {
                        if (edge.To == uTo)
                        {
                            totalTime += edge.GetWeight(emergencyMode);
                            break;
                        }
                    }
                }
                result.TotalTravelTimeMinutes = totalTime;
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
