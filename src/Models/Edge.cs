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
        public double EffectiveTravelTimeMinutes => TravelTimeMinutes * TrafficMultiplier;

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
        }
    }
}
