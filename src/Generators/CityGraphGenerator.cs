using System;
using System.Collections.Generic;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.Utilities;

namespace SmartEmergencyRoutePlanner.Generators
{
    public class CityGraphGenerator
    {
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

            // 1. Generate Vertex Coordinates based on Family
            if (family == GraphFamily.GridCity)
            {
                int rows = (int)Math.Sqrt(vertexCount);
                if (rows < 2) rows = 2;
                int cols = (int)Math.Ceiling((double)vertexCount / rows);

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

            // 2. Build Backbone path
            for (int i = 0; i < vertexCount - 1; i++)
            {
                int from = i;
                int to = i + 1;
                double distance = Geometry.CalculateEuclideanDistance(graph.GetVertex(from), graph.GetVertex(to));
                double speed = 20.0 + (random.NextDouble() * 80.0);

                var edge = graph.AddEdge(from, to, distance, speed);
                PopulateAdvancedProperties(edge);
                existingEdges.Add((from, to));
                currentEdgeCount++;
            }

            long maxPossibleEdges = (long)vertexCount * (vertexCount - 1);
            long targetEdges = Math.Min(edgeCount, maxPossibleEdges);

            // 3. Add topology specific edges
            if (family == GraphFamily.GridCity)
            {
                int rows = (int)Math.Sqrt(vertexCount);
                if (rows < 2) rows = 2;
                int cols = (int)Math.Ceiling((double)vertexCount / rows);

                // Add grid neighbor lines
                for (int i = 0; i < vertexCount; i++)
                {
                    if (currentEdgeCount >= targetEdges) break;
                    int r = i / cols;
                    int c = i % cols;

                    int[] neighbors = {
                        c + 1 < cols && i + 1 < vertexCount ? i + 1 : -1,
                        r + 1 < rows && i + cols < vertexCount ? i + cols : -1,
                        c - 1 >= 0 && i - 1 >= 0 ? i - 1 : -1,
                        r - 1 >= 0 && i - cols >= 0 ? i - cols : -1
                    };

                    foreach (int nb in neighbors)
                    {
                        if (nb != -1 && nb != i && !existingEdges.Contains((i, nb)))
                        {
                            if (currentEdgeCount >= targetEdges) break;
                            double distance = Geometry.CalculateEuclideanDistance(graph.GetVertex(i), graph.GetVertex(nb));
                            double speed = 20.0 + (random.NextDouble() * 80.0);
                            var edge = graph.AddEdge(i, nb, distance, speed);
                            PopulateAdvancedProperties(edge);
                            existingEdges.Add((i, nb));
                            currentEdgeCount++;
                        }
                    }
                }

                // Add diagonal links
                for (int i = 0; i < vertexCount; i++)
                {
                    if (currentEdgeCount >= targetEdges) break;
                    int r = i / cols;
                    int c = i % cols;

                    int[] diagonals = {
                        r + 1 < rows && c + 1 < cols && i + cols + 1 < vertexCount ? i + cols + 1 : -1,
                        r + 1 < rows && c - 1 >= 0 && i + cols - 1 < vertexCount ? i + cols - 1 : -1,
                        r - 1 >= 0 && c + 1 < cols && i - cols + 1 >= 0 ? i - cols + 1 : -1,
                        r - 1 >= 0 && c - 1 >= 0 && i - cols - 1 >= 0 ? i - cols - 1 : -1
                    };

                    foreach (int dg in diagonals)
                    {
                        if (dg != -1 && dg != i && !existingEdges.Contains((i, dg)))
                        {
                            if (currentEdgeCount >= targetEdges) break;
                            double distance = Geometry.CalculateEuclideanDistance(graph.GetVertex(i), graph.GetVertex(dg));
                            double speed = 20.0 + (random.NextDouble() * 80.0);
                            var edge = graph.AddEdge(i, dg, distance, speed);
                            PopulateAdvancedProperties(edge);
                            existingEdges.Add((i, dg));
                            currentEdgeCount++;
                        }
                    }
                }
            }

            // 4. Fill in remaining edges randomly
            while (currentEdgeCount < targetEdges)
            {
                int from = random.Next(vertexCount);
                int to = random.Next(vertexCount);

                if (from == to || existingEdges.Contains((from, to)))
                {
                    continue;
                }

                double distance = Geometry.CalculateEuclideanDistance(graph.GetVertex(from), graph.GetVertex(to));
                double speed = 20.0 + (random.NextDouble() * 80.0);

                var edge = graph.AddEdge(from, to, distance, speed);
                PopulateAdvancedProperties(edge);
                existingEdges.Add((from, to));
                currentEdgeCount++;
            }

            return graph;
        }

        public static Graph Generate(int vertexCount, int edgeCount, int seed)
        {
            return Generate(vertexCount, edgeCount, seed, GraphFamily.RandomSparse);
        }
    }
}
