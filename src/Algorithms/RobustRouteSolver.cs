using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.DataStructures;

namespace SmartEmergencyRoutePlanner.Algorithms
{
    public class RobustRouteSolver
    {
        /// <summary>
        /// Solves the shortest path problem under risk constraints, minimizing
        /// TotalTravelTime + lambda * (ClosureRisk + TrafficRisk).
        /// </summary>
        public PathResult Solve(Graph graph, int source, int target, bool emergencyMode = false, double lambda = 10.0)
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

                    // Robust weight: TravelTime + lambda * (ClosureRisk + TrafficRisk)
                    double robustWeight = edge.GetWeight(emergencyMode, true, lambda);
                    double newDist = dist[u] + robustWeight;
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
                AlgorithmName = "Robust Route Solver",
                IsReachable = isReachable,
                RuntimeTicks = stopwatch.ElapsedTicks,
                RuntimeMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                ExpandedNodes = expandedNodes,
                ExpandedNodesList = expandedList,
                RelaxationCount = relaxationCount,
                HasNegativeCycle = false,
                Notes = string.Empty
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

                // Calculate the actual unpenalized travel time and total risk along the path
                double totalTime = 0;
                double totalRisk = 0;
                for (int i = 0; i < path.Count - 1; i++)
                {
                    int uFrom = path[i];
                    int uTo = path[i + 1];
                    foreach (var edge in graph.GetNeighbors(uFrom))
                    {
                        if (edge.To == uTo)
                        {
                            totalTime += edge.GetWeight(emergencyMode);
                            totalRisk += edge.ClosureRisk + edge.TrafficRisk;
                            break;
                        }
                    }
                }
                result.TotalTravelTimeMinutes = totalTime;
                result.Notes = $"Total Path Risk Score: {totalRisk:F4} (Travel Time: {totalTime:F2} min)";
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
