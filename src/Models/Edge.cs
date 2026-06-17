namespace SmartEmergencyRoutePlanner.Models
{
    public class Edge
    {
        public int From { get; set; }
        public int To { get; set; }
        public double DistanceKm { get; set; }
        public double SpeedKmh { get; set; }
        public double TravelTimeMinutes { get; set; }

        public bool IsClosed { get; set; }
        public TrafficLevel Traffic { get; set; }
        public double TrafficMultiplier { get; set; }
        
        // Phase 2 Advanced Properties
        public double TimePeriodMultiplier { get; set; }
        public bool HasEmergencyLane { get; set; }
        public double EmergencyMultiplier { get; set; }
        public double ClosureRisk { get; set; }
        public double TrafficRisk { get; set; }

        public double EffectiveTravelTimeMinutes => GetWeight(false);

        public Edge(int from, int to, double distanceKm, double speedKmh)
        {
            From = from;
            To = to;
            DistanceKm = distanceKm;
            SpeedKmh = speedKmh;
            TravelTimeMinutes = (distanceKm / speedKmh) * 60.0;
            
            IsClosed = false;
            Traffic = TrafficLevel.Normal;
            TrafficMultiplier = 1.0;

            // Phase 2 Defaults
            TimePeriodMultiplier = 1.0;
            HasEmergencyLane = false;
            EmergencyMultiplier = 0.6;
            ClosureRisk = 0.0;
            TrafficRisk = 0.0;
        }

        /// <summary>
        /// Calculates the dynamic weight of the edge based on active parameters.
        /// </summary>
        public double GetWeight(bool emergencyMode = false, bool robustMode = false, double lambda = 10.0)
        {
            double travelTime = TravelTimeMinutes * TrafficMultiplier * TimePeriodMultiplier;
            if (emergencyMode && HasEmergencyLane)
            {
                travelTime *= EmergencyMultiplier;
            }
            if (robustMode)
            {
                travelTime += lambda * (ClosureRisk + TrafficRisk);
            }
            return travelTime;
        }
    }
}
