using System.Collections.Generic;

namespace SmartEmergencyRoutePlanner.Models
{
    public class PathResult
    {
        public string AlgorithmName { get; set; } = string.Empty;
        public bool IsReachable { get; set; }
        public double TotalTravelTimeMinutes { get; set; }
        public List<int> Path { get; set; } = new List<int>();
        public long RuntimeTicks { get; set; }
        public double RuntimeMilliseconds { get; set; }
        public int ExpandedNodes { get; set; }
        public long RelaxationCount { get; set; }
        public bool HasNegativeCycle { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
