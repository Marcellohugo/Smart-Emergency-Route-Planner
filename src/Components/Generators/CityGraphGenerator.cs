using System;
using System.Collections.Generic;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.Utilities;

namespace SmartEmergencyRoutePlanner.Generators
{
    public class CityGraphGenerator
    {
        private class UnionFind
        {
            private readonly int[] parent;
            public UnionFind(int n)
            {
                parent = new int[n];
                for (int i = 0; i < n; i++) parent[i] = i;
            }
            public int Find(int i)
            {
                if (parent[i] == i) return i;
                return parent[i] = Find(parent[i]);
            }
            public bool Union(int i, int j)
            {
                int rootI = Find(i);
                int rootJ = Find(j);
                if (rootI != rootJ)
                {
                    parent[rootI] = rootJ;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Generates a synthetic city road graph network belonging to a specific graph family.
        /// </summary>
        public static Graph Generate(int vertexCount, int edgeCount, int seed, GraphFamily family)
        {
            if (vertexCount < 2)
            {
                throw new ArgumentException("Vertex count must be at least 2.");
            }

            var graph = new Graph(vertexCount);
            var random = new Random(seed);

            int rows = 0;
            int cols = 0;

            // 1. Generate Vertex Coordinates based on Family
            if (family == GraphFamily.GridCity)
            {
                rows = (int)Math.Sqrt(vertexCount);
                if (rows < 2) rows = 2;
                cols = (int)Math.Ceiling((double)vertexCount / rows);

                for (int i = 0; i < vertexCount; i++)
                {
                    int r = i / cols;
                    int c = i % cols;

                    double baseX = cols > 1 ? c * (100.0 / (cols - 1)) : 50.0;
                    double baseY = rows > 1 ? r * (100.0 / (rows - 1)) : 50.0;

                    double jitterRangeX = cols > 1 ? (100.0 / (cols - 1)) * 0.15 : 0.0;
                    double jitterRangeY = rows > 1 ? (100.0 / (rows - 1)) * 0.15 : 0.0;

                    double x = baseX + (random.NextDouble() - 0.5) * jitterRangeX;
                    double y = baseY + (random.NextDouble() - 0.5) * jitterRangeY;

                    x = Math.Max(0.0, Math.Min(100.0, x));
                    y = Math.Max(0.0, Math.Min(100.0, y));

                    string name = i == 0 ? "Ambulance Dispatch Hub" :
                                  i == vertexCount - 1 ? "City Central Hospital" :
                                  $"Intersection Grid ({r},{c})";

                    graph.AddVertex(new Vertex(i, x, y, name));
                }
            }
            else
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    double x = random.NextDouble() * 100.0;
                    double y = random.NextDouble() * 100.0;
                    string name = i == 0 ? "Ambulance Dispatch Hub" :
                                  i == vertexCount - 1 ? "City Central Hospital" :
                                  $"Intersection {i}";
                    graph.AddVertex(new Vertex(i, x, y, name));
                }
            }

            var existingEdges = new HashSet<(int, int)>();
            long currentEdgeCount = 0;

            // Helper to populate advanced edge properties
            void PopulateAdvancedProperties(Edge edge)
            {
                edge.HasEmergencyLane = random.NextDouble() < 0.25; // 25% chance of priority lane
                edge.ClosureRisk = random.NextDouble() * 0.1;       // Closure risk: [0.0, 0.1]
                edge.TrafficRisk = random.NextDouble() * 0.3;       // Traffic risk: [0.0, 0.3]
            }

            void AddBidirectionalOrSingleEdge(int u, int v)
            {
                if (currentEdgeCount >= edgeCount) return;

                if (!existingEdges.Contains((u, v)))
                {
                    double distance = Geometry.CalculateEuclideanDistance(graph.GetVertex(u), graph.GetVertex(v));
                    double speed = 20.0 + (random.NextDouble() * 80.0);
                    var edge = graph.AddEdge(u, v, distance, speed);
                    PopulateAdvancedProperties(edge);
                    existingEdges.Add((u, v));
                    currentEdgeCount++;
                }

                if (currentEdgeCount < edgeCount && !existingEdges.Contains((v, u)))
                {
                    double distance = Geometry.CalculateEuclideanDistance(graph.GetVertex(v), graph.GetVertex(u));
                    double speed = 20.0 + (random.NextDouble() * 80.0);
                    var edge = graph.AddEdge(v, u, distance, speed);
                    PopulateAdvancedProperties(edge);
                    existingEdges.Add((v, u));
                    currentEdgeCount++;
                }
            }

            // Union-Find and Potential Edges setup
            var uf = new UnionFind(vertexCount);
            var mstUndirectedEdges = new List<(int u, int v)>();
            var potentialUndirectedEdges = new List<(int u, int v, double weight)>();

            if (family == GraphFamily.GridCity)
            {

                // Collect grid-neighbor edges (horizontal & vertical)
                for (int i = 0; i < vertexCount; i++)
                {
                    int r = i / cols;
                    int c = i % cols;

                    if (c + 1 < cols && i + 1 < vertexCount)
                    {
                        double dist = Geometry.CalculateEuclideanDistance(graph.GetVertex(i), graph.GetVertex(i + 1));
                        potentialUndirectedEdges.Add((i, i + 1, dist));
                    }
                    if (r + 1 < rows && i + cols < vertexCount)
                    {
                        double dist = Geometry.CalculateEuclideanDistance(graph.GetVertex(i), graph.GetVertex(i + cols));
                        potentialUndirectedEdges.Add((i, i + cols, dist));
                    }
                }

                // Shuffle grid-neighbors to make random-looking spanning trees
                for (int i = potentialUndirectedEdges.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = potentialUndirectedEdges[i];
                    potentialUndirectedEdges[i] = potentialUndirectedEdges[j];
                    potentialUndirectedEdges[j] = temp;
                }
            }
            else
            {
                // Collect all possible pairs of vertices
                for (int i = 0; i < vertexCount; i++)
                {
                    for (int j = i + 1; j < vertexCount; j++)
                    {
                        double dist = Geometry.CalculateEuclideanDistance(graph.GetVertex(i), graph.GetVertex(j));
                        potentialUndirectedEdges.Add((i, j, dist));
                    }
                }

                // Sort by Euclidean distance to connect nearby nodes first
                potentialUndirectedEdges.Sort((a, b) => a.weight.CompareTo(b.weight));
            }

            // Build MST to guarantee connectivity
            var selectedMstEdges = new HashSet<(int u, int v)>();
            foreach (var edge in potentialUndirectedEdges)
            {
                if (uf.Union(edge.u, edge.v))
                {
                    mstUndirectedEdges.Add((edge.u, edge.v));
                    selectedMstEdges.Add((edge.u, edge.v));
                }
            }

            // First pass: add MST edges in forward direction (weak connectivity)
            foreach (var edge in mstUndirectedEdges)
            {
                if (currentEdgeCount >= edgeCount) break;
                if (!existingEdges.Contains((edge.u, edge.v)))
                {
                    double distance = Geometry.CalculateEuclideanDistance(graph.GetVertex(edge.u), graph.GetVertex(edge.v));
                    double speed = 20.0 + (random.NextDouble() * 80.0);
                    var newEdge = graph.AddEdge(edge.u, edge.v, distance, speed);
                    PopulateAdvancedProperties(newEdge);
                    existingEdges.Add((edge.u, edge.v));
                    currentEdgeCount++;
                }
            }

            // Second pass: add MST edges in reverse direction (bidirectional strong connectivity)
            foreach (var edge in mstUndirectedEdges)
            {
                if (currentEdgeCount >= edgeCount) break;
                if (!existingEdges.Contains((edge.v, edge.u)))
                {
                    double distance = Geometry.CalculateEuclideanDistance(graph.GetVertex(edge.v), graph.GetVertex(edge.u));
                    double speed = 20.0 + (random.NextDouble() * 80.0);
                    var newEdge = graph.AddEdge(edge.v, edge.u, distance, speed);
                    PopulateAdvancedProperties(newEdge);
                    existingEdges.Add((edge.v, edge.u));
                    currentEdgeCount++;
                }
            }

            // Fill remaining edges with non-MST potential edges
            if (family == GraphFamily.GridCity)
            {
                // Add remaining non-MST grid edges first
                foreach (var edge in potentialUndirectedEdges)
                {
                    if (currentEdgeCount >= edgeCount) break;
                    if (!selectedMstEdges.Contains((edge.u, edge.v)))
                    {
                        AddBidirectionalOrSingleEdge(edge.u, edge.v);
                    }
                }

                // If budget permits, add diagonal connections
                if (currentEdgeCount < edgeCount)
                {
                    var diagonals = new List<(int u, int v)>();
                    for (int i = 0; i < vertexCount; i++)
                    {
                        int r = i / cols;
                        int c = i % cols;

                        if (r + 1 < rows && c + 1 < cols && i + cols + 1 < vertexCount)
                        {
                            diagonals.Add((i, i + cols + 1));
                        }
                        if (r + 1 < rows && c - 1 >= 0 && i + cols - 1 < vertexCount)
                        {
                            diagonals.Add((i, i + cols - 1));
                        }
                    }

                    // Shuffle diagonals
                    for (int i = diagonals.Count - 1; i > 0; i--)
                    {
                        int j = random.Next(i + 1);
                        var temp = diagonals[i];
                        diagonals[i] = diagonals[j];
                        diagonals[j] = temp;
                    }

                    foreach (var edge in diagonals)
                    {
                        if (currentEdgeCount >= edgeCount) break;
                        AddBidirectionalOrSingleEdge(edge.u, edge.v);
                    }
                }
            }
            else
            {
                // For random sparse/medium, add remaining edges in order of Euclidean distance
                foreach (var edge in potentialUndirectedEdges)
                {
                    if (currentEdgeCount >= edgeCount) break;
                    if (!selectedMstEdges.Contains((edge.u, edge.v)))
                    {
                        AddBidirectionalOrSingleEdge(edge.u, edge.v);
                    }
                }
            }

            // If still have budget, fill in random edges
            while (currentEdgeCount < edgeCount)
            {
                int from = random.Next(vertexCount);
                int to = random.Next(vertexCount);
                if (from == to) continue;
                AddBidirectionalOrSingleEdge(from, to);
            }

            return graph;
        }

        public static Graph Generate(int vertexCount, int edgeCount, int seed)
        {
            return Generate(vertexCount, edgeCount, seed, GraphFamily.RandomSparse);
        }
    }
}
