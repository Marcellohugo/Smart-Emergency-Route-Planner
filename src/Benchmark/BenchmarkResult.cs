namespace SmartEmergencyRoutePlanner.Benchmark
{
    public class BenchmarkResult
    {
        public int VertexCount { get; set; }
        public int EdgeCount { get; set; }
        public int Seed { get; set; }
        public double DijkstraMs { get; set; }
        public double AStarMs { get; set; }
        public double? BellmanFordMs { get; set; }
        public double DijkstraDistance { get; set; }
        public double AStarDistance { get; set; }
        public double? BellmanFordDistance { get; set; }
        public int DijkstraExpandedNodes { get; set; }
        public int AStarExpandedNodes { get; set; }
        public int DijkstraPathLength { get; set; }
        public int AStarPathLength { get; set; }
        public bool DijkstraEqualsAStar { get; set; }
        public bool DijkstraEqualsBellmanFord { get; set; }
        public string BellmanFordStatus { get; set; } = string.Empty;
        public double AStarSpeedup { get; set; }
    }
}
