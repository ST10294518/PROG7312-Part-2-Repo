namespace SmartX.API.Models;

public class TelemetryPacket<T>
{
    public Guid PacketId { get; set; } = Guid.NewGuid();

    public string SensorId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Metric { get; set; } = string.Empty;

    public T Value { get; set; } = default!;

    public TelemetryPacket()
    {
    }

    public TelemetryPacket(
        string sensorId,
        string metric,
        T value)
    {
        SensorId = sensorId;
        Metric = metric;
        Value = value;
        Timestamp = DateTime.UtcNow;
    }
}