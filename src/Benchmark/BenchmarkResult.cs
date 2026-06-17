using SmartEmergencyRoutePlanner.Generators;

namespace SmartEmergencyRoutePlanner.Benchmark
{
    public class BenchmarkResult
    {
        public GraphFamily Family { get; set; }
        public int VertexCount { get; set; }
        public int EdgeCount { get; set; }
        public int Seed { get; set; }

        // Runtimes
        public double DijkstraAvgMs { get; set; }
        public double DijkstraMinMs { get; set; }
        public double DijkstraMaxMs { get; set; }

        public double AStarAvgMs { get; set; }
        public double AStarMinMs { get; set; }
        public double AStarMaxMs { get; set; }

        public double? BellmanFordAvgMs { get; set; }
        public string BellmanFordStatus { get; set; } = string.Empty;

        // Routing Results
        public double DijkstraDistance { get; set; }
        public double AStarDistance { get; set; }

        // Expanded Nodes & Relaxations
        public int DijkstraExpandedNodes { get; set; }
        public int AStarExpandedNodes { get; set; }
        public long DijkstraRelaxations { get; set; }
        public long AStarRelaxations { get; set; }

        // Path Lengths
        public int DijkstraPathLength { get; set; }
        public int AStarPathLength { get; set; }

        // Quality and Speedup Metrics
        public bool SameDistance { get; set; }
        public double DistanceDifference { get; set; }
        public double AStarSpeedup { get; set; }
        public double ExpandedNodeReductionPercent { get; set; }
    }
}
