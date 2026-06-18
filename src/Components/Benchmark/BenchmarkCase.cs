namespace SmartEmergencyRoutePlanner.Benchmark
{
    public class BenchmarkCase
    {
        public int VertexCount { get; }
        public int EdgeCount { get; }
        public int Seed { get; }

        public BenchmarkCase(int vertexCount, int edgeCount, int seed)
        {
            VertexCount = vertexCount;
            EdgeCount = edgeCount;
            Seed = seed;
        }
    }
}
