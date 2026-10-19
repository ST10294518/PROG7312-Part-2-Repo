using System.Text.Json;
using SmartX.API.Models;

namespace SmartX.API.Services;

public sealed record TelemetryValidationResult(
    bool Success,
    string Message,
    int Priority);

public sealed class TelemetryInputValidator
{
    private readonly SensorService _sensors;

    public TelemetryInputValidator(SensorService sensors)
    {
        _sensors = sensors;
    }

    public TelemetryValidationResult Validate(TelemetryInput? input)
    {
        if (input is null)
            return Invalid("Telemetry input is required.");

        if (!Guid.TryParse(input.SensorId, out var sensorId))
            return Invalid("Invalid sensor ID.");

        if (_sensors.GetById(sensorId) is null)
            return Invalid("Sensor does not exist.");

        if (string.IsNullOrWhiteSpace(input.Metric))
            return Invalid("Metric is required.");

        if (input.Metric.Length > 100)
            return Invalid("Metric must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(input.ValueType))
            return Invalid("Value type is required.");

        var valueType = input.ValueType.ToLowerInvariant();
        double? numericValue = null;

        switch (valueType)
        {
            case "float":
                if (input.Value.ValueKind != JsonValueKind.Number ||
                    !input.Value.TryGetSingle(out var floatValue) ||
                    !float.IsFinite(floatValue))
                {
                    return Invalid("Value must be a finite float.");
                }

                numericValue = floatValue;
                break;

            case "int":
                if (input.Value.ValueKind != JsonValueKind.Number ||
                    !input.Value.TryGetInt32(out var intValue))
                {
                    return Invalid("Value must be a valid 32-bit integer.");
                }

                numericValue = intValue;
                break;

            case "bool":
                if (input.Value.ValueKind != JsonValueKind.True &&
                    input.Value.ValueKind != JsonValueKind.False)
                {
                    return Invalid("Value must be true or false.");
                }
                break;

            default:
                return Invalid("Supported value types are float, int and bool.");
        }

        var metric = input.Metric.Trim();
        var temperature = metric.Contains(
            "temperature", StringComparison.OrdinalIgnoreCase);
        var power = metric.Contains(
            "power", StringComparison.OrdinalIgnoreCase);
        var moisture = metric.Contains(
            "moisture", StringComparison.OrdinalIgnoreCase);

        if ((temperature || power || moisture) && numericValue is null)
            return Invalid("This metric requires a numeric value.");

        if (moisture &&
            numericValue is double moistureValue &&
            (moistureValue < 0 || moistureValue > 100))
        {
            return Invalid("Moisture must be between 0 and 100 percent.");
        }

        if (numericValue is not double value)
            return new(true, "Telemetry is valid.", 0);

        var priority = 0;

        if (temperature)
        {
            priority = value < 0 || value > 45
                ? 2
                : value < 5 || value > 35 ? 1 : 0;
        }

        if (power)
        {
            var powerPriority = value > 6000 ? 2 : value > 4000 ? 1 : 0;
            priority = Math.Max(priority, powerPriority);
        }

        if (moisture)
        {
            var moisturePriority = value < 10 ? 2 : value < 20 ? 1 : 0;
            priority = Math.Max(priority, moisturePriority);
        }

        return new(true, "Telemetry is valid.", priority);
    }

    private static TelemetryValidationResult Invalid(string message)
    {
        return new(false, message, 0);
    }
}
