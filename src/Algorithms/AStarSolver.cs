using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.DataStructures;
using SmartEmergencyRoutePlanner.Utilities;

namespace SmartEmergencyRoutePlanner.Algorithms
{
    public class AStarSolver
    {
        /// <summary>
        /// Solves the shortest path problem using A* Search algorithm.
        /// </summary>
        public PathResult Solve(Graph graph, int source, int target, double maxSpeedKmh = 100.0)
        {
            var stopwatch = Stopwatch.StartNew();

            int n = graph.VertexCount;
            double[] gScore = new double[n];
            double[] fScore = new double[n];
            int[] prev = new int[n];
            bool[] visited = new bool[n];

            for (int i = 0; i < n; i++)
            {
                gScore[i] = double.PositiveInfinity;
                fScore[i] = double.PositiveInfinity;
                prev[i] = -1;
            }

            var targetVertex = graph.GetVertex(target);

            // Heuristic function: straight-line distance divided by max speed, in minutes
            double Heuristic(int u)
            {
                var uVertex = graph.GetVertex(u);
                double distKm = Geometry.CalculateEuclideanDistance(uVertex, targetVertex);
                return (distKm / maxSpeedKmh) * 60.0;
            }

            gScore[source] = 0;
            fScore[source] = Heuristic(source);

            var heap = new BinaryMinHeap();
            heap.Insert(source, fScore[source]);

            int expandedNodes = 0;
            bool reached = false;

            while (!heap.IsEmpty)
            {
                var minNode = heap.ExtractMin();
                int u = minNode.VertexId;
                double priority = minNode.Priority;

                // Lazy deletion of stale elements
                if (priority > fScore[u])
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

                    double tentativeG = gScore[u] + edge.TravelTimeMinutes;
                    if (tentativeG < gScore[v])
                    {
                        gScore[v] = tentativeG;
                        double f = tentativeG + Heuristic(v);
                        fScore[v] = f;
                        prev[v] = u;
                        heap.Insert(v, f);
                    }
                }
            }

            stopwatch.Stop();

            bool isReachable = reached || (gScore[target] < double.PositiveInfinity);

            var result = new PathResult
            {
                AlgorithmName = "A* Search",
                IsReachable = isReachable,
                RuntimeTicks = stopwatch.ElapsedTicks,
                RuntimeMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                ExpandedNodes = expandedNodes,
                HasNegativeCycle = false,
                Notes = isReachable ? "Optimal path found using heuristic." : "Target unreachable."
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
                result.TotalTravelTimeMinutes = gScore[target];
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
