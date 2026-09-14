using System.Globalization;
using System.Text.Json;
using SmartX.API.DataStructures;
using SmartX.API.Models;

namespace SmartX.API.Services;

public class TelemetryService
{
    private readonly TelemetryHistoryStore _history = new();
    private readonly SensorService _sensorService;

    public TelemetryService(SensorService sensorService)
    {
        _sensorService = sensorService;
    }

    public List<TelemetryRecord> GetLatest(int count = 50)
    {
        return _history.GetLatest(count);
    }

    public List<TelemetryRecord> GetSensorHistory(
        string sensorId,
        int count = 50)
    {
        return _history.GetSensorHistory(sensorId, count);
    }

    public object GetStatistics()
    {
        var records = _history.Records;

        return new
        {
            totalReadings = records.Count,
            totalAlerts = records.Count(x => x.IsAnomaly),
            environmentalReadings =
                records.Count(x =>
                    x.Metric.Contains(
                        "temperature",
                        StringComparison.OrdinalIgnoreCase)),
            averageNumericValue =
                records
                    .Where(x => x.IsNumeric)
                    .Select(x => x.NumericValue)
                    .DefaultIfEmpty()
                    .Average()
        };
    }

    public (bool Success, string Message, TelemetryRecord? Record)
        Process(TelemetryInput input)
    {
        if (!Guid.TryParse(input.SensorId, out var sensorGuid))
            return (false, "Invalid sensor ID.", null);

        var sensor = _sensorService.GetById(sensorGuid);

        if (sensor == null)
            return (false, "Sensor does not exist.", null);

        if (string.IsNullOrWhiteSpace(input.Metric))
            return (false, "Metric is required.", null);

        if (string.IsNullOrWhiteSpace(input.ValueType))
            return (false, "Value type is required.", null);

        try
        {
            TelemetryRecord record;

            switch (input.ValueType.ToLowerInvariant())
            {
                case "float":

                    var floatValue =
                        input.Value.GetSingle();

                    var floatPacket =
                        new TelemetryPacket<float>(
                            input.SensorId,
                            input.Metric,
                            floatValue);

                    record = CreateNumericRecord(
                        floatPacket.SensorId,
                        floatPacket.Metric,
                        floatPacket.Value,
                        "float");

                    break;

                case "int":

                    var intValue =
                        input.Value.GetInt32();

                    var intPacket =
                        new TelemetryPacket<int>(
                            input.SensorId,
                            input.Metric,
                            intValue);

                    record = CreateNumericRecord(
                        intPacket.SensorId,
                        intPacket.Metric,
                        intPacket.Value,
                        "int");

                    break;

                case "bool":

                    var boolValue =
                        input.Value.GetBoolean();

                    var boolPacket =
                        new TelemetryPacket<bool>(
                            input.SensorId,
                            input.Metric,
                            boolValue);

                    record = new TelemetryRecord
                    {
                        SensorId = boolPacket.SensorId,
                        Metric = boolPacket.Metric,
                        ValueType = "bool",
                        Value = boolPacket.Value.ToString(),
                        IsNumeric = false,
                        IsAnomaly = false,
                        Timestamp = boolPacket.Timestamp
                    };

                    break;

                default:
                    return (
                        false,
                        "Supported value types are float, int and bool.",
                        null);
            }

            _history.Add(record);

            _sensorService.UpdateLastSeen(input.SensorId);

            if (record.IsAnomaly)
                _sensorService.UpdateAlertCount(input.SensorId);

            return (
                true,
                record.IsAnomaly
                    ? record.AlertMessage
                    : "Telemetry accepted successfully.",
                record);
        }
        catch (Exception)
        {
            return (
                false,
                "The telemetry value could not be parsed for the selected type.",
                null);
        }
    }

    private TelemetryRecord CreateNumericRecord<T>(
        string sensorId,
        string metric,
        T value,
        string type)
        where T : IConvertible
    {
        double numericValue =
            Convert.ToDouble(
                value,
                CultureInfo.InvariantCulture);

        bool anomaly = false;
        string message = string.Empty;

        if (metric.Contains(
                "temperature",
                StringComparison.OrdinalIgnoreCase))
        {
            if (numericValue > 35)
            {
                anomaly = true;
                message =
                    $"Temperature anomaly detected: {numericValue:0.0}°C";
            }
            else if (numericValue < 5)
            {
                anomaly = true;
                message =
                    $"Low temperature anomaly detected: {numericValue:0.0}°C";
            }
        }

        if (metric.Contains(
                "power",
                StringComparison.OrdinalIgnoreCase))
        {
            if (numericValue > 4000)
            {
                anomaly = true;
                message =
                    $"Power spike detected: {numericValue:0} W";
            }
        }

        if (metric.Contains(
                "moisture",
                StringComparison.OrdinalIgnoreCase))
        {
            if (numericValue < 20)
            {
                anomaly = true;
                message =
                    $"Low moisture anomaly detected: {numericValue:0.0}%";
            }
        }

        return new TelemetryRecord
        {
            SensorId = sensorId,
            Metric = metric,
            ValueType = type,
            Value = Convert.ToString(
                value,
                CultureInfo.InvariantCulture) ?? string.Empty,
            NumericValue = numericValue,
            IsNumeric = true,
            IsAnomaly = anomaly,
            AlertMessage = message,
            Timestamp = DateTime.UtcNow
        };
    }

    // Operator overloading demonstration.
    public static NumericTelemetry operatorExample()
    {
        var meter1 = new NumericTelemetry(1200);
        var meter2 = new NumericTelemetry(800);

        return meter1 + meter2;
    }
}

public readonly struct NumericTelemetry
{
    public double Value { get; }

    public NumericTelemetry(double value)
    {
        Value = value;
    }

    public static NumericTelemetry operator +(
        NumericTelemetry left,
        NumericTelemetry right)
    {
        return new NumericTelemetry(
            left.Value + right.Value);
    }

    public static NumericTelemetry operator -(
        NumericTelemetry left,
        NumericTelemetry right)
    {
        return new NumericTelemetry(
            left.Value - right.Value);
    }

    public static bool operator >(
        NumericTelemetry left,
        NumericTelemetry right)
    {
        return left.Value > right.Value;
    }

    public static bool operator <(
        NumericTelemetry left,
        NumericTelemetry right)
    {
        return left.Value < right.Value;
    }

    public override string ToString()
    {
        return Value.ToString("0.00");
    }
}