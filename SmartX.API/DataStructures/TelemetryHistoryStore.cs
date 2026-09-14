using SmartX.API.Models;

namespace SmartX.API.DataStructures;

public class TelemetryHistoryStore
{
    private readonly List<TelemetryRecord> _records = new();

    // Raw historical telemetry represented as jagged arrays.
    private readonly List<double[][]> _rawBatches = new();

    public IReadOnlyList<TelemetryRecord> Records => _records;

    public void Add(TelemetryRecord record)
    {
        _records.Add(record);
    }

    public void AddRawBatch(double[][] batch)
    {
        _rawBatches.Add(batch);
    }

    public List<TelemetryRecord> GetLatest(int count)
    {
        return _records
            .OrderByDescending(x => x.Timestamp)
            .Take(count)
            .ToList();
    }

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

    public int Count => _records.Count;
}