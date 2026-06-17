using System;
using System.Collections.Generic;
using SmartEmergencyRoutePlanner.Models;
using SmartEmergencyRoutePlanner.Utilities;

namespace SmartEmergencyRoutePlanner.Generators
{
    public class CityGraphGenerator
    {
        /// <summary>
        /// Generates a synthetic city road graph network with reproducible characteristics.
        /// </summary>
        /// <param name="vertexCount">Number of vertices (intersections).</param>
        /// <param name="edgeCount">Target number of directed edges.</param>
        /// <param name="seed">Random seed for reproducibility.</param>
        /// <returns>A connected Graph instance.</returns>
        public static Graph Generate(int vertexCount, int edgeCount, int seed)
        {
            if (vertexCount < 2)
            {
                throw new ArgumentException("Vertex count must be at least 2.");
            }

            var graph = new Graph(vertexCount);
            var random = new Random(seed);

            // 1. Generate Vertices with coordinates in 100km x 100km area
            for (int i = 0; i < vertexCount; i++)
            {
                double x = random.NextDouble() * 100.0;
                double y = random.NextDouble() * 100.0;
                string name = i == 0 ? "Ambulance Dispatch Hub" :
                              i == vertexCount - 1 ? "City Central Hospital" :
                              $"Intersection {i}";
                graph.AddVertex(new Vertex(i, x, y, name));
            }

            // Keep track of existing directed edges to avoid duplicates and self-loops
            var existingEdges = new HashSet<(int, int)>();

            // 2. Build Backbone path: 0 -> 1 -> 2 -> ... -> vertexCount-1
            // This guarantees the target is always reachable from source (vertex 0)
            for (int i = 0; i < vertexCount - 1; i++)
            {
                int from = i;
                int to = i + 1;
                double distance = Geometry.CalculateEuclideanDistance(graph.GetVertex(from), graph.GetVertex(to));
                double speed = 20.0 + (random.NextDouble() * 80.0); // Speed limit between 20 and 100 km/h

                graph.AddEdge(from, to, distance, speed);
                existingEdges.Add((from, to));
            }

            // 3. Add random edges to reach the target edge count
            // Cap targetEdges to avoid infinite loop on dense graphs
            long maxPossibleEdges = (long)vertexCount * (vertexCount - 1);
            long targetEdges = Math.Min(edgeCount, maxPossibleEdges);
            long currentEdgeCount = vertexCount - 1;

            while (currentEdgeCount < targetEdges)
            {
                int from = random.Next(vertexCount);
                int to = random.Next(vertexCount);

                // Check self-loop or existing duplicate edge
                if (from == to || existingEdges.Contains((from, to)))
                {
                    continue;
                }

                double distance = Geometry.CalculateEuclideanDistance(graph.GetVertex(from), graph.GetVertex(to));
                double speed = 20.0 + (random.NextDouble() * 80.0); // Speed limit between 20 and 100 km/h

                graph.AddEdge(from, to, distance, speed);
                existingEdges.Add((from, to));
                currentEdgeCount++;
            }

            return graph;
        }
    }
}
