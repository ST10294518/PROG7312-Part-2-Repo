using SmartX.API.Models;

namespace SmartX.API.DataStructures;

public class TelemetryHistoryStore
{
    private readonly List<TelemetryRecord> _records = new();
    private readonly List<double[][]> _rawBatches = new();

    private readonly SortedDictionary<DateTime, List<TelemetryRecord>>
        _timeline = new();

    private readonly Dictionary<
        string,
        SortedDictionary<DateTime, List<TelemetryRecord>>>
        _sensorTimelines = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _sync = new();

    public IReadOnlyList<TelemetryRecord> Records
    {
        get
        {
            lock (_sync)
            {
                return _records.ToList();
            }
        }
    }

    public void Add(TelemetryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_sync)
        {
            if (record.IsNumeric)
            {
                double[][] rawBatch =
                {
                    new[] { record.NumericValue }
                };

                _rawBatches.Add(rawBatch);
            }

            _records.Add(record);
            AddToTimeline(_timeline, record);

            if (!_sensorTimelines.TryGetValue(
                record.SensorId, out var sensorTimeline))
            {
                sensorTimeline =
                    new SortedDictionary<DateTime, List<TelemetryRecord>>();

                _sensorTimelines.Add(record.SensorId, sensorTimeline);
            }

            AddToTimeline(sensorTimeline, record);
        }
    }

    private static void AddToTimeline(
        SortedDictionary<DateTime, List<TelemetryRecord>> timeline,
        TelemetryRecord record)
    {
        if (!timeline.TryGetValue(record.Timestamp, out var bucket))
        {
            bucket = new List<TelemetryRecord>();
            timeline.Add(record.Timestamp, bucket);
        }

        bucket.Add(record);
    }

    public void AddRawBatch(double[][] batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (_sync)
        {
            _rawBatches.Add(batch);
        }
    }

    public List<TelemetryRecord> GetLatest(int count)
    {
        lock (_sync)
        {
            return _timeline
                .Reverse()
                .SelectMany(entry => entry.Value.AsEnumerable().Reverse())
                .Take(Math.Max(0, count))
                .ToList();
        }
    }

    public List<TelemetryRecord> GetSensorHistory(
        string sensorId,
        int count = 50)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(sensorId) ||
                !_sensorTimelines.TryGetValue(sensorId, out var timeline))
            {
                return new List<TelemetryRecord>();
            }

            var latest = timeline
                .Reverse()
                .SelectMany(entry => entry.Value.AsEnumerable().Reverse())
                .Take(Math.Max(0, count))
                .ToList();

            latest.Reverse();
            return latest;
        }
    }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _records.Count;
            }
        }
    }

    public int RawBatchCount
    {
        get
        {
            lock (_sync)
            {
                return _rawBatches.Count;
            }
        }
    }
}
