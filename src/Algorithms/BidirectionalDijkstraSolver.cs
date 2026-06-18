using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.DataStructures;

namespace SmartEmergencyRoutePlanner.Algorithms
{
    public class BidirectionalDijkstraSolver
    {
        /// <summary>
        /// Solves the shortest path problem using Bidirectional Dijkstra's algorithm.
        /// </summary>
        public PathResult Solve(Graph graph, int source, int target, bool emergencyMode = false)
        {
            var stopwatch = Stopwatch.StartNew();

            int n = graph.VertexCount;
            double[] distF = new double[n];
            double[] distB = new double[n];
            int[] prevF = new int[n];
            int[] prevB = new int[n];
            bool[] visitedF = new bool[n];
            bool[] visitedB = new bool[n];

            for (int i = 0; i < n; i++)
            {
                distF[i] = double.PositiveInfinity;
                distB[i] = double.PositiveInfinity;
                prevF[i] = -1;
                prevB[i] = -1;
            }

            distF[source] = 0;
            distB[target] = 0;

            var heapF = new BinaryMinHeap();
            var heapB = new BinaryMinHeap();

            heapF.Insert(source, 0);
            heapB.Insert(target, 0);

            double bestDistance = double.PositiveInfinity;
            int meetingVertex = -1;

            int expandedNodes = 0;
            var expandedList = new List<int>();
            long relaxationCount = 0;

            while (!heapF.IsEmpty && !heapB.IsEmpty)
            {
                // Stopping condition: check if frontiers can meet with a better distance
                double minF = heapF.Peek().Priority;
                double minB = heapB.Peek().Priority;
                if (minF + minB >= bestDistance)
                {
                    break;
                }

                // Forward Step
                if (!heapF.IsEmpty)
                {
                    var node = heapF.ExtractMin();
                    int u = node.VertexId;
                    double currentD = node.Priority;

                    if (currentD <= distF[u])
                    {
                        visitedF[u] = true;
                        expandedNodes++;
                        expandedList.Add(u);

                        if (visitedB[u])
                        {
                            double totalD = distF[u] + distB[u];
                            if (totalD < bestDistance)
                            {
                                bestDistance = totalD;
                                meetingVertex = u;
                            }
                        }

                        foreach (var edge in graph.GetNeighbors(u))
                        {
                            if (edge.IsClosed) continue;

                            relaxationCount++;
                            int v = edge.To;
                            double w = edge.GetWeight(emergencyMode);

                            if (distF[u] + w < distF[v])
                            {
                                distF[v] = distF[u] + w;
                                prevF[v] = u;
                                heapF.Insert(v, distF[v]);

                                if (visitedB[v])
                                {
                                    double totalD = distF[v] + distB[v];
                                    if (totalD < bestDistance)
                                    {
                                        bestDistance = totalD;
                                        meetingVertex = v;
                                    }
                                }
                            }
                        }
                    }
                }

                // Backward Step
                if (!heapB.IsEmpty)
                {
                    var node = heapB.ExtractMin();
                    int u = node.VertexId;
                    double currentD = node.Priority;

                    if (currentD <= distB[u])
                    {
                        visitedB[u] = true;
                        expandedNodes++;
                        expandedList.Add(u);

                        if (visitedF[u])
                        {
                            double totalD = distF[u] + distB[u];
                            if (totalD < bestDistance)
                            {
                                bestDistance = totalD;
                                meetingVertex = u;
                            }
                        }

                        // Traverse incoming edges to u backwards
                        foreach (var edge in graph.ReverseAdjacencyList[u])
                        {
                            if (edge.IsClosed) continue;

                            relaxationCount++;
                            int v = edge.From;
                            double w = edge.GetWeight(emergencyMode);

                            if (distB[u] + w < distB[v])
                            {
                                distB[v] = distB[u] + w;
                                prevB[v] = u; // u is the parent of v in the backward path
                                heapB.Insert(v, distB[v]);

                                if (visitedF[v])
                                {
                                    double totalD = distF[v] + distB[v];
                                    if (totalD < bestDistance)
                                    {
                                        bestDistance = totalD;
                                        meetingVertex = v;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            stopwatch.Stop();

            bool isReachable = meetingVertex != -1 && bestDistance < double.PositiveInfinity;

            var result = new PathResult
            {
                AlgorithmName = "Bidirectional Dijkstra",
                IsReachable = isReachable,
                RuntimeTicks = stopwatch.ElapsedTicks,
                RuntimeMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                ExpandedNodes = expandedNodes,
                ExpandedNodesList = expandedList,
                RelaxationCount = relaxationCount,
                HasNegativeCycle = false,
                Notes = isReachable ? $"Meeting vertex: {meetingVertex}" : "Target unreachable."
            };

            if (isReachable)
            {
                // Reconstruct forward path (source -> meetingVertex)
                var pathF = new List<int>();
                int curr = meetingVertex;
                while (curr != -1)
                {
                    pathF.Add(curr);
                    curr = prevF[curr];
                }
                pathF.Reverse();

                // Reconstruct backward path (meetingVertex -> target)
                var pathB = new List<int>();
                curr = meetingVertex;
                while (curr != -1)
                {
                    pathB.Add(curr);
                    curr = prevB[curr];
                }

                // Combine paths. Avoid duplicate meeting vertex.
                var finalPath = new List<int>(pathF);
                for (int i = 1; i < pathB.Count; i++)
                {
                    finalPath.Add(pathB[i]);
                }

                result.Path = finalPath;

                // Re-calculate the actual unpenalized travel time along the path
                double totalTime = 0;
                for (int i = 0; i < finalPath.Count - 1; i++)
                {
                    int uFrom = finalPath[i];
                    int uTo = finalPath[i + 1];
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
