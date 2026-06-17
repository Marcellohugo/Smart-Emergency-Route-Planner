using System;
using System.Collections.Generic;

namespace SmartEmergencyRoutePlanner.Models
{
    public class Graph
    {
        public int VertexCount { get; private set; }
        public List<Vertex> Vertices { get; private set; }
        public List<Edge>[] AdjacencyList { get; private set; }
        public List<Edge>[] ReverseAdjacencyList { get; private set; }
        public List<Edge> AllEdges { get; private set; }
        
        private readonly Dictionary<int, Vertex> _vertexMap;

        public Graph(int vertexCount)
        {
            VertexCount = vertexCount;
            Vertices = new List<Vertex>(vertexCount);
            AdjacencyList = new List<Edge>[vertexCount];
            ReverseAdjacencyList = new List<Edge>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                AdjacencyList[i] = new List<Edge>();
                ReverseAdjacencyList[i] = new List<Edge>();
            }
            AllEdges = new List<Edge>();
            _vertexMap = new Dictionary<int, Vertex>(vertexCount);
        }

        public void AddVertex(Vertex vertex)
        {
            Vertices.Add(vertex);
            _vertexMap[vertex.Id] = vertex;
        }

        public Edge AddEdge(int from, int to, double distanceKm, double speedKmh)
        {
            if (from < 0 || from >= VertexCount || to < 0 || to >= VertexCount)
            {
                throw new ArgumentException($"Vertex indices must be between 0 and {VertexCount - 1}.");
            }

            var edge = new Edge(from, to, distanceKm, speedKmh);
            AdjacencyList[from].Add(edge);
            ReverseAdjacencyList[to].Add(edge);
            AllEdges.Add(edge);
            return edge;
        }

        public List<Edge> GetNeighbors(int vertexId)
        {
            if (vertexId < 0 || vertexId >= VertexCount)
            {
                throw new ArgumentException($"Vertex ID must be between 0 and {VertexCount - 1}.");
            }
            return AdjacencyList[vertexId];
        }

        public Vertex GetVertex(int id)
        {
            if (_vertexMap.TryGetValue(id, out var vertex))
            {
                return vertex;
            }
            throw new KeyNotFoundException($"Vertex with ID {id} does not exist in the graph.");
        }

        // ==================================================
        // ROAD CLOSURE METHODS
        // ==================================================

        public void CloseEdge(int from, int to)
        {
            foreach (var edge in AdjacencyList[from])
            {
                if (edge.To == to)
                {
                    edge.IsClosed = true;
                }
            }
        }

        public void OpenEdge(int from, int to)
        {
            foreach (var edge in AdjacencyList[from])
            {
                if (edge.To == to)
                {
                    edge.IsClosed = false;
                }
            }
        }

        public void CloseRandomEdges(double closureRate, int seed)
        {
            ResetClosures();
            var random = new Random(seed);
            int targetClosureCount = (int)(AllEdges.Count * closureRate);
            
            var indices = new List<int>(AllEdges.Count);
            for (int i = 0; i < AllEdges.Count; i++)
            {
                indices.Add(i);
            }

            // Shuffle indices using Fisher-Yates
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = indices[i];
                indices[i] = indices[j];
                indices[j] = temp;
            }

            for (int i = 0; i < targetClosureCount && i < indices.Count; i++)
            {
                AllEdges[indices[i]].IsClosed = true;
            }
        }

        public void ResetClosures()
        {
            foreach (var edge in AllEdges)
            {
                edge.IsClosed = false;
            }
        }

        // ==================================================
        // TRAFFIC CONDITION METHODS
        // ==================================================

        public void ApplyRandomTraffic(int seed)
        {
            var random = new Random(seed);
            foreach (var edge in AllEdges)
            {
                double r = random.NextDouble();
                if (r < 0.15)
                {
                    edge.Traffic = TrafficLevel.Low;
                    edge.TrafficMultiplier = 0.8;
                }
                else if (r < 0.65)
                {
                    edge.Traffic = TrafficLevel.Normal;
                    edge.TrafficMultiplier = 1.0;
                }
                else if (r < 0.90)
                {
                    edge.Traffic = TrafficLevel.High;
                    edge.TrafficMultiplier = 1.5;
                }
                else
                {
                    edge.Traffic = TrafficLevel.Severe;
                    edge.TrafficMultiplier = 2.5;
                }
            }
        }

        public void ResetTraffic()
        {
            foreach (var edge in AllEdges)
            {
                edge.Traffic = TrafficLevel.Normal;
                edge.TrafficMultiplier = 1.0;
            }
        }
    }
}
