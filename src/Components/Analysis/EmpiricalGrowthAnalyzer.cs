using System;
using System.Collections.Generic;

namespace SmartEmergencyRoutePlanner.Analysis
{
    public static class EmpiricalGrowthAnalyzer
    {
        /// <summary>
        /// Estimates the empirical growth exponent 'b' in runtime ~ a * V^b
        /// by performing a linear regression on log-transformed data.
        /// </summary>
        public static double EstimateExponent(List<int> vertexCounts, List<double> runtimes)
        {
            if (vertexCounts == null || runtimes == null || vertexCounts.Count != runtimes.Count || vertexCounts.Count < 2)
            {
                return 0.0;
            }

            var logX = new List<double>();
            var logY = new List<double>();

            for (int i = 0; i < vertexCounts.Count; i++)
            {
                double x = vertexCounts[i];
                double y = runtimes[i];

                // Exclude invalid timings
                if (x > 0 && y > 0)
                {
                    logX.Add(Math.Log(x));
                    logY.Add(Math.Log(y));
                }
            }

            int count = logX.Count;
            if (count < 2)
            {
                return 0.0;
            }

            double sumX = 0;
            double sumY = 0;
            double sumXY = 0;
            double sumXX = 0;

            for (int i = 0; i < count; i++)
            {
                sumX += logX[i];
                sumY += logY[i];
                sumXY += logX[i] * logY[i];
                sumXX += logX[i] * logX[i];
            }

            double denominator = (count * sumXX) - (sumX * sumX);
            if (Math.Abs(denominator) < 1e-9)
            {
                return 0.0;
            }

            double slope = ((count * sumXY) - (sumX * sumY)) / denominator;
            return slope;
        }
    }
}
