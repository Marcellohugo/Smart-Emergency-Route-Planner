using SmartEmergencyRoutePlanner.Generators;

namespace SmartEmergencyRoutePlanner.Benchmark
{
    public class BenchmarkResult
    {
        public GraphFamily Family { get; set; }
        public int VertexCount { get; set; }
        public int EdgeCount { get; set; }
        public int Seed { get; set; }

        // Runtimes: Dijkstra
        public double DijkstraAvgMs { get; set; }
        public double DijkstraMinMs { get; set; }
        public double DijkstraMaxMs { get; set; }

        // Runtimes: A*
        public double AStarAvgMs { get; set; }
        public double AStarMinMs { get; set; }
        public double AStarMaxMs { get; set; }

        // Runtimes: Bidirectional Dijkstra
        public double BiDijkstraAvgMs { get; set; }
        public double BiDijkstraMinMs { get; set; }
        public double BiDijkstraMaxMs { get; set; }

        // Runtimes: Bellman-Ford
        public double? BellmanFordAvgMs { get; set; }
        public double? BellmanFordMinMs { get; set; }
        public double? BellmanFordMaxMs { get; set; }
        public string BellmanFordStatus { get; set; } = string.Empty;

        // Travel Times / Distances
        public double DijkstraDistance { get; set; }
        public double AStarDistance { get; set; }
        public double BiDijkstraDistance { get; set; }
        public double? BellmanFordDistance { get; set; }

        // Node Expansions
        public int DijkstraExpandedNodes { get; set; }
        public int AStarExpandedNodes { get; set; }
        public int BiDijkstraExpandedNodes { get; set; }

        // Relaxation Counts
        public long DijkstraRelaxations { get; set; }
        public long AStarRelaxations { get; set; }
        public long BiDijkstraRelaxations { get; set; }

        // Path Lengths
        public int DijkstraPathLength { get; set; }
        public int AStarPathLength { get; set; }
        public int BiDijkstraPathLength { get; set; }

        // Quality and Speedups
        public bool SameDistance { get; set; } // Dijkstra equals A*
        public double DistanceDifference { get; set; }
        public bool BiDijkstraEqualsDijkstra { get; set; }
        public bool BellmanFordEqualsDijkstra { get; set; }
        public string CostConsistencyStatus { get; set; } = string.Empty;
        public double AStarSpeedup { get; set; }
        public double BiDijkstraSpeedup { get; set; }
        public double ExpandedNodeReductionPercent { get; set; }

        // Memory Profiling (bytes)
        public long DijkstraMemoryBytes { get; set; }
        public long AStarMemoryBytes { get; set; }
        public long BiDijkstraMemoryBytes { get; set; }
        public long BellmanFordMemoryBytes { get; set; }
    }
}
