namespace SmartEmergencyRoutePlanner.Models
{
    public class Edge
    {
        public int From { get; set; }
        public int To { get; set; }
        public double DistanceKm { get; set; }
        public double SpeedKmh { get; set; }
        public double TravelTimeMinutes { get; set; }

        public Edge(int from, int to, double distanceKm, double speedKmh)
        {
            From = from;
            To = to;
            DistanceKm = distanceKm;
            SpeedKmh = speedKmh;
            TravelTimeMinutes = (distanceKm / speedKmh) * 60.0;
        }
    }
}
