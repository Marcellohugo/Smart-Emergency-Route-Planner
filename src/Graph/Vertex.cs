namespace SmartEmergencyRoutePlanner.Models
{
    public class Vertex
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string Name { get; set; }

        public Vertex(int id, double x, double y, string name)
        {
            Id = id;
            X = x;
            Y = y;
            Name = name;
        }
    }
}
