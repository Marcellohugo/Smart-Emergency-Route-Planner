using System;
using System.Collections.Generic;

namespace SmartEmergencyRoutePlanner.Models
{
    public class Graph
    {
        public int VertexCount { get; private set; }
        public List<Vertex> Vertices { get; private set; }
        public List<Edge>[] AdjacencyList { get; private set; }
        public List<Edge> AllEdges { get; private set; }
        
        // Dictionary for O(1) vertex retrieval by ID
        private readonly Dictionary<int, Vertex> _vertexMap;

        public Graph(int vertexCount)
        {
            VertexCount = vertexCount;
            Vertices = new List<Vertex>(vertexCount);
            AdjacencyList = new List<Edge>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                AdjacencyList[i] = new List<Edge>();
            }
            AllEdges = new List<Edge>();
            _vertexMap = new Dictionary<int, Vertex>(vertexCount);
        }

        public void AddVertex(Vertex vertex)
        {
            Vertices.Add(vertex);
            _vertexMap[vertex.Id] = vertex;
        }

        public void AddEdge(int from, int to, double distanceKm, double speedKmh)
        {
            if (from < 0 || from >= VertexCount || to < 0 || to >= VertexCount)
            {
                throw new ArgumentException($"Vertex indices must be between 0 and {VertexCount - 1}.");
            }

            var edge = new Edge(from, to, distanceKm, speedKmh);
            AdjacencyList[from].Add(edge);
            AllEdges.Add(edge);
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
    }
}
