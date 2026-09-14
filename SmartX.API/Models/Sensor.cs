namespace SmartX.API.Models;

public class Sensor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string MacAddress { get; set; } = string.Empty;

    public string DeploymentLocation { get; set; } = string.Empty;

    public string SensorCategory { get; set; } = string.Empty;

    public string Status { get; set; } = "Online";

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public int TotalReadings { get; set; }

    public int TotalAlerts { get; set; }
}