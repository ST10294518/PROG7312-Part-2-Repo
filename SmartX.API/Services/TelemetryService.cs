using System.Globalization;
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

    // ============================================================
    // TELEMETRY RETRIEVAL
    // ============================================================

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

    // ============================================================
    // TELEMETRY STATISTICS
    // ============================================================

    public object GetStatistics()
    {
        var records = _history.Records;

        var numericRecords = records
            .Where(x => x.IsNumeric)
            .ToList();

        // Use the overloaded + operator for actual aggregation.
        var totalTelemetry = AggregateNumericTelemetry(numericRecords);

        var averageNumericValue = numericRecords.Count == 0
            ? 0
            : totalTelemetry.Value / numericRecords.Count;

        return new
        {
            totalReadings = records.Count,

            totalAlerts = records.Count(x => x.IsAnomaly),

            environmentalReadings =
                records.Count(x =>
                    x.Metric.Contains(
                        "temperature",
                        StringComparison.OrdinalIgnoreCase)),

            averageNumericValue = averageNumericValue
        };
    }

    // ============================================================
    // TELEMETRY PROCESSING
    // ============================================================

    public (bool Success, string Message, TelemetryRecord? Record)
        Process(TelemetryInput input)
    {
        if (!Guid.TryParse(input.SensorId, out var sensorGuid))
        {
            return (
                false,
                "Invalid sensor ID.",
                null);
        }

        var sensor = _sensorService.GetById(sensorGuid);

        if (sensor == null)
        {
            return (
                false,
                "Sensor does not exist.",
                null);
        }

        if (string.IsNullOrWhiteSpace(input.Metric))
        {
            return (
                false,
                "Metric is required.",
                null);
        }

        if (string.IsNullOrWhiteSpace(input.ValueType))
        {
            return (
                false,
                "Value type is required.",
                null);
        }

        try
        {
            TelemetryRecord record;

            switch (input.ValueType.ToLowerInvariant())
            {
                // ====================================================
                // FLOAT TELEMETRY
                // ====================================================

                case "float":

                    var floatValue = input.Value.GetSingle();

                    // Strongly typed generic telemetry packet.
                    var floatPacket =
                        new TelemetryPacket<float>(
                            input.SensorId,
                            input.Metric,
                            floatValue);

                    record = CreateNumericRecord(
                        floatPacket.SensorId,
                        floatPacket.Metric,
                        floatPacket.Value,
                        "float",
                        floatPacket.Timestamp);

                    break;

                // ====================================================
                // INTEGER TELEMETRY
                // ====================================================

                case "int":

                    var intValue = input.Value.GetInt32();

                    // Strongly typed generic telemetry packet.
                    var intPacket =
                        new TelemetryPacket<int>(
                            input.SensorId,
                            input.Metric,
                            intValue);

                    record = CreateNumericRecord(
                        intPacket.SensorId,
                        intPacket.Metric,
                        intPacket.Value,
                        "int",
                        intPacket.Timestamp);

                    break;

                // ====================================================
                // BOOLEAN TELEMETRY
                // ====================================================

                case "bool":

                    var boolValue = input.Value.GetBoolean();

                    // Strongly typed generic telemetry packet.
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
                        AlertMessage = string.Empty,
                        Timestamp = boolPacket.Timestamp
                    };

                    break;

                // ====================================================
                // UNSUPPORTED TYPE
                // ====================================================

                default:

                    return (
                        false,
                        "Supported value types are float, int and bool.",
                        null);
            }

            // Store the processed telemetry.
            _history.Add(record);

            // Update the sensor activity.
            _sensorService.UpdateLastSeen(input.SensorId);

            // Update alert count if an anomaly was detected.
            if (record.IsAnomaly)
            {
                _sensorService.UpdateAlertCount(input.SensorId);
            }

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

    // ============================================================
    // GENERIC NUMERIC TELEMETRY PROCESSING
    // ============================================================

    private TelemetryRecord CreateNumericRecord<T>(
        string sensorId,
        string metric,
        T value,
        string type,
        DateTime timestamp)
        where T : IConvertible
    {
        /*
         * The telemetry packet remains strongly typed as float or int.
         *
         * NumericTelemetry provides a dedicated numeric value wrapper
         * so that overloaded operators can be used for telemetry
         * comparisons, aggregation and delta calculations.
         */
        double numericValue =
            Convert.ToDouble(
                value,
                CultureInfo.InvariantCulture);

        var telemetryValue =
            new NumericTelemetry(numericValue);

        bool anomaly = false;
        string message = string.Empty;

        // ============================================================
        // TEMPERATURE ANOMALY DETECTION
        // ============================================================

        if (metric.Contains(
                "temperature",
                StringComparison.OrdinalIgnoreCase))
        {
            var maximumTemperature =
                new NumericTelemetry(35);

            var minimumTemperature =
                new NumericTelemetry(5);

            // Uses overloaded > operator.
            if (telemetryValue > maximumTemperature)
            {
                // Uses overloaded - operator to calculate
                // how far the reading is above the limit.
                var temperatureDelta =
                    telemetryValue - maximumTemperature;

                anomaly = true;

                message =
                    $"Temperature anomaly detected: " +
                    $"{numericValue:0.0}°C " +
                    $"({temperatureDelta.Value:0.0}°C above limit)";
            }

            // Uses overloaded < operator.
            else if (telemetryValue < minimumTemperature)
            {
                // Uses overloaded - operator to calculate
                // how far the reading is below the limit.
                var temperatureDelta =
                    minimumTemperature - telemetryValue;

                anomaly = true;

                message =
                    $"Low temperature anomaly detected: " +
                    $"{numericValue:0.0}°C " +
                    $"({temperatureDelta.Value:0.0}°C below limit)";
            }
        }

        // ============================================================
        // POWER ANOMALY DETECTION
        // ============================================================

        if (metric.Contains(
                "power",
                StringComparison.OrdinalIgnoreCase))
        {
            var maximumPower =
                new NumericTelemetry(4000);

            // Uses overloaded > operator.
            if (telemetryValue > maximumPower)
            {
                // Uses overloaded - operator to calculate
                // the power spike above the threshold.
                var powerDelta =
                    telemetryValue - maximumPower;

                anomaly = true;

                message =
                    $"Power spike detected: " +
                    $"{numericValue:0} W " +
                    $"({powerDelta.Value:0} W above limit)";
            }
        }

        // ============================================================
        // MOISTURE ANOMALY DETECTION
        // ============================================================

        if (metric.Contains(
                "moisture",
                StringComparison.OrdinalIgnoreCase))
        {
            var minimumMoisture =
                new NumericTelemetry(20);

            // Uses overloaded < operator.
            if (telemetryValue < minimumMoisture)
            {
                // Uses overloaded - operator to calculate
                // how far the reading is below the limit.
                var moistureDelta =
                    minimumMoisture - telemetryValue;

                anomaly = true;

                message =
                    $"Low moisture anomaly detected: " +
                    $"{numericValue:0.0}% " +
                    $"({moistureDelta.Value:0.0}% below limit)";
            }
        }

        return new TelemetryRecord
        {
            SensorId = sensorId,

            Metric = metric,

            ValueType = type,

            Value =
                Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture)
                ?? string.Empty,

            NumericValue = numericValue,

            IsNumeric = true,

            IsAnomaly = anomaly,

            AlertMessage = message,

            Timestamp = timestamp
        };
    }

    // ============================================================
    // TELEMETRY AGGREGATION
    // ============================================================

    private static NumericTelemetry AggregateNumericTelemetry(
        IEnumerable<TelemetryRecord> records)
    {
        /*
         * Start with zero and use the overloaded + operator
         * to aggregate numeric telemetry values.
         */
        var total =
            new NumericTelemetry(0);

        foreach (var record in records)
        {
            total +=
                new NumericTelemetry(
                    record.NumericValue);
        }

        return total;
    }
}

// ====================================================================
// NUMERIC TELEMETRY VALUE OBJECT
// ====================================================================

public readonly struct NumericTelemetry
{
    public double Value { get; }

    public NumericTelemetry(double value)
    {
        Value = value;
    }

    // ================================================================
    // ADDITION OPERATOR
    // Used for telemetry aggregation.
    // ================================================================

    public static NumericTelemetry operator +(
        NumericTelemetry left,
        NumericTelemetry right)
    {
        return new NumericTelemetry(
            left.Value + right.Value);
    }

    // ================================================================
    // SUBTRACTION OPERATOR
    // Used for telemetry delta/deviation calculations.
    // ================================================================

    public static NumericTelemetry operator -(
        NumericTelemetry left,
        NumericTelemetry right)
    {
        return new NumericTelemetry(
            left.Value - right.Value);
    }

    // ================================================================
    // GREATER-THAN OPERATOR
    // Used for upper anomaly thresholds.
    // ================================================================

    public static bool operator >(
        NumericTelemetry left,
        NumericTelemetry right)
    {
        return left.Value > right.Value;
    }

    // ================================================================
    // LESS-THAN OPERATOR
    // Used for lower anomaly thresholds.
    // ================================================================

    public static bool operator <(
        NumericTelemetry left,
        NumericTelemetry right)
    {
        return left.Value < right.Value;
    }

    // ================================================================
    // STRING REPRESENTATION
    // ================================================================

    public override string ToString()
    {
        return Value.ToString(
            "0.00",
            CultureInfo.InvariantCulture);
    }
}