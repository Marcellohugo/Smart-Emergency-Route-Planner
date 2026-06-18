namespace SmartEmergencyRoutePlanner;

public sealed class HospitalItem
{
    public int NodeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsReachable { get; set; }
    public double TravelTimeMinutes { get; set; }
}
