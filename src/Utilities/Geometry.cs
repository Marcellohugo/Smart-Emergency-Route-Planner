using System;
using SmartEmergencyRoutePlanner.Models;

namespace SmartEmergencyRoutePlanner.Utilities
{
    public static class Geometry
    {
        /// <summary>
        /// Calculates the Euclidean distance between two vertices.
        /// </summary>
        public static double CalculateEuclideanDistance(Vertex a, Vertex b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
