using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.DataStructures;

namespace SmartEmergencyRoutePlanner.Algorithms
{
    public class DijkstraMultiTargetSolver
    {
        /// <summary>
        /// Solves the multi-hospital routing query by executing a single-source Dijkstra run,
        /// then identifying the target hospital with the minimum travel time.
        /// </summary>
        public PathResult Solve(Graph graph, int source, List<int> targetHospitals, bool emergencyMode = false)
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
            long relaxationCount = 0;

            // Run Dijkstra to completion to capture distances to all vertices
            while (!heap.IsEmpty)
            {
                var minNode = heap.ExtractMin();
                int u = minNode.VertexId;
                double currentDist = minNode.Priority;

                // Skip stale entry
                if (currentDist > dist[u])
                {
                    continue;
                }

                expandedNodes++;
                visited[u] = true;

                foreach (var edge in graph.GetNeighbors(u))
                {
                    if (edge.IsClosed) continue;

                    relaxationCount++;
                    int v = edge.To;
                    if (visited[v]) continue;

                    double newDist = dist[u] + edge.GetWeight(emergencyMode);
                    if (newDist < dist[v])
                    {
                        dist[v] = newDist;
                        prev[v] = u;
                        heap.Insert(v, newDist);
                    }
                }
            }

            // Find target hospital with lowest travel time
            double bestTime = double.PositiveInfinity;
            int bestHospital = -1;

            foreach (int hospital in targetHospitals)
            {
                if (dist[hospital] < bestTime)
                {
                    bestTime = dist[hospital];
                    bestHospital = hospital;
                }
            }

            stopwatch.Stop();

            bool isReachable = bestHospital != -1 && bestTime < double.PositiveInfinity;

            var result = new PathResult
            {
                AlgorithmName = "Dijkstra Multi-Target",
                IsReachable = isReachable,
                RuntimeTicks = stopwatch.ElapsedTicks,
                RuntimeMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                ExpandedNodes = expandedNodes,
                RelaxationCount = relaxationCount,
                HasNegativeCycle = false,
                Notes = isReachable ? $"Closest Hospital: Vertex {bestHospital}" : "No hospital is reachable."
            };

            if (isReachable)
            {
                var path = new List<int>();
                int curr = bestHospital;
                while (curr != -1)
                {
                    path.Add(curr);
                    curr = prev[curr];
                }
                path.Reverse();
                result.Path = path;
                result.TotalTravelTimeMinutes = bestTime;
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
