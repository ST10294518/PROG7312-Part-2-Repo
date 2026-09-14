namespace SmartX.API.Models;

public class TelemetryRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string SensorId { get; set; } = string.Empty;

    public string Metric { get; set; } = string.Empty;

    public string ValueType { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public double NumericValue { get; set; }

    public bool IsNumeric { get; set; }

    public bool IsAnomaly { get; set; }

    public string AlertMessage { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}