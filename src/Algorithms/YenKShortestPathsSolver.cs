using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner.Algorithms
{
    public class YenKShortestPathsSolver
    {
        /// <summary>
        /// Finds the exact K shortest loopless paths from source to target using Yen's algorithm.
        /// </summary>
        public List<PathResult> FindKShortestPaths(Graph graph, int source, int target, int k = 3, bool emergencyMode = false)
        {
            var A = new List<PathResult>();
            var B = new List<PathResult>();
            var dijkstra = new DijkstraSolver();

            // 1. Find the first shortest path
            var path0 = dijkstra.Solve(graph, source, target, emergencyMode);
            if (!path0.IsReachable || path0.Path.Count == 0)
            {
                return A; // Target unreachable
            }

            path0.AlgorithmName = "Yen Path 1";
            A.Add(path0);

            // Keep track of temporarily closed edges to restore them
            var temporarilyClosedEdges = new List<Edge>();

            for (int ki = 1; ki < k; ki++)
            {
                // The spur node ranges from the first node to the second to last node in the previous path
                var previousPath = A[ki - 1].Path;

                for (int i = 0; i < previousPath.Count - 1; i++)
                {
                    int spurNode = previousPath[i];

                    // Root path is the subpath from source to spurNode
                    var rootPath = previousPath.GetRange(0, i + 1);

                    // A. Remove edges that share the same prefix as the root path
                    foreach (var path in A)
                    {
                        if (path.Path.Count > i + 1)
                        {
                            bool matches = true;
                            for (int j = 0; j <= i; j++)
                            {
                                if (path.Path[j] != rootPath[j])
                                {
                                    matches = false;
                                    break;
                                }
                            }

                            if (matches)
                            {
                                int from = path.Path[i];
                                int to = path.Path[i + 1];
                                foreach (var edge in graph.GetNeighbors(from))
                                {
                                    if (edge.To == to && !edge.IsClosed)
                                    {
                                        edge.IsClosed = true;
                                        temporarilyClosedEdges.Add(edge);
                                    }
                                }
                            }
                        }
                    }

                    // B. Close all nodes in rootPath (except spurNode) to prevent loops
                    for (int j = 0; j < rootPath.Count - 1; j++)
                    {
                        int rootNode = rootPath[j];
                        foreach (var edge in graph.GetNeighbors(rootNode))
                        {
                            if (!edge.IsClosed)
                            {
                                edge.IsClosed = true;
                                temporarilyClosedEdges.Add(edge);
                            }
                        }
                        foreach (var edge in graph.ReverseAdjacencyList[rootNode])
                        {
                            if (!edge.IsClosed)
                            {
                                edge.IsClosed = true;
                                temporarilyClosedEdges.Add(edge);
                            }
                        }
                    }

                    // C. Calculate the spur path from spurNode to target
                    var spurPathResult = dijkstra.Solve(graph, spurNode, target, emergencyMode);

                    if (spurPathResult.IsReachable && spurPathResult.Path.Count > 0)
                    {
                        // Combine root path and spur path
                        var combinedPath = new List<int>(rootPath);
                        // Skip the first node of the spur path since it is the spurNode itself
                        for (int j = 1; j < spurPathResult.Path.Count; j++)
                        {
                            combinedPath.Add(spurPathResult.Path[j]);
                        }

                        // Calculate unpenalized path weight
                        double totalTime = 0;
                        for (int j = 0; j < combinedPath.Count - 1; j++)
                        {
                            int uFrom = combinedPath[j];
                            int uTo = combinedPath[j + 1];
                            foreach (var edge in graph.GetNeighbors(uFrom))
                            {
                                if (edge.To == uTo)
                                {
                                    totalTime += edge.GetWeight(emergencyMode);
                                    break;
                                }
                            }
                        }

                        var candidate = new PathResult
                        {
                            AlgorithmName = $"Yen Path Candidate",
                            IsReachable = true,
                            TotalTravelTimeMinutes = totalTime,
                            Path = combinedPath,
                            ExpandedNodes = spurPathResult.ExpandedNodes, // representative
                            RelaxationCount = spurPathResult.RelaxationCount,
                            HasNegativeCycle = false
                        };

                        // Add to B if it's not already in A or B
                        if (!ContainsPath(A, candidate.Path) && !ContainsPath(B, candidate.Path))
                        {
                            B.Add(candidate);
                        }
                    }

                    // D. Restore temporarily closed edges
                    foreach (var edge in temporarilyClosedEdges)
                    {
                        edge.IsClosed = false;
                    }
                    temporarilyClosedEdges.Clear();
                }

                if (B.Count == 0)
                {
                    break;
                }

                // Sort B by travel time
                B.Sort((x, y) => x.TotalTravelTimeMinutes.CompareTo(y.TotalTravelTimeMinutes));

                // Move the best path from B to A
                var bestCandidate = B[0];
                bestCandidate.AlgorithmName = $"Yen Path {ki + 1}";
                A.Add(bestCandidate);
                B.RemoveAt(0);
            }

            return A;
        }

        private bool ContainsPath(List<PathResult> pathsList, List<int> queryPath)
        {
            foreach (var p in pathsList)
            {
                if (p.Path.Count == queryPath.Count)
                {
                    bool match = true;
                    for (int i = 0; i < p.Path.Count; i++)
                    {
                        if (p.Path[i] != queryPath[i])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match) return true;
                }
            }
            return false;
        }
    }
}
