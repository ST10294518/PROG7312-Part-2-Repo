using System.Text.Json;

namespace SmartX.API.Models;

public class TelemetryInput
{
    public string SensorId { get; set; } = string.Empty;

    public string Metric { get; set; } = string.Empty;

    public string ValueType { get; set; } = string.Empty;

    public JsonElement Value { get; set; }
}