using SmartX.API.Models;

namespace SmartX.API.DataStructures;

public class TelemetryHistoryStore
{
    // Optimized telemetry history using List<T>.
    private readonly List<TelemetryRecord> _records = new();

    // Raw historical telemetry represented as jagged arrays.
    private readonly List<double[][]> _rawBatches = new();

    public IReadOnlyList<TelemetryRecord> Records => _records;

    // ============================================================
    // ADD TELEMETRY RECORD
    // ============================================================

    public void Add(TelemetryRecord record)
    {
        _records.Add(record);

        /*
         * Numeric telemetry is also represented as a jagged array
         * batch before being retained in the optimized List<T>
         * collection.
         *
         * Each batch can contain a different number of rows,
         * demonstrating the flexible memory layout of jagged arrays.
         */
        if (record.IsNumeric)
        {
            double[][] rawBatch =
            {
                new[]
                {
                    record.NumericValue
                }
            };

            AddRawBatch(rawBatch);
        }
    }

    // ============================================================
    // ADD RAW JAGGED ARRAY BATCH
    // ============================================================

    public void AddRawBatch(double[][] batch)
    {
        _rawBatches.Add(batch);
    }

    // ============================================================
    // LATEST TELEMETRY
    // ============================================================

    public List<TelemetryRecord> GetLatest(int count)
    {
        return _records
            .OrderByDescending(x => x.Timestamp)
            .Take(count)
            .ToList();
    }

    // ============================================================
    // SENSOR TELEMETRY HISTORY
    // ============================================================

    public List<TelemetryRecord> GetSensorHistory(
        string sensorId,
        int count = 50)
    {
        return _records
            .Where(x => x.SensorId == sensorId)
            .OrderByDescending(x => x.Timestamp)
            .Take(count)
            .OrderBy(x => x.Timestamp)
            .ToList();
    }

    // ============================================================
    // COLLECTION COUNT
    // ============================================================

    public int Count => _records.Count;

    // ============================================================
    // RAW BATCH COUNT
    // ============================================================

    public int RawBatchCount => _rawBatches.Count;
}