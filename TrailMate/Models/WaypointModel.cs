namespace TrailMate.Models;

public class WaypointModel
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  
    public override string ToString() =>
        $"Lat: {Latitude:F5}, Lon: {Longitude:F5} @ {Timestamp:HH:mm:ss}";
}
