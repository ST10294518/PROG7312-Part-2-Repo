using SmartX.API.Models;

namespace SmartX.API.DataStructures;

public class TelemetryHistoryStore
{
    private sealed class Timeline
    {
        public SortedDictionary<DateTime, List<TelemetryRecord>> Buckets { get; }
            = new();

        public SortedSet<DateTime> Timestamps { get; } = new();

        public void Add(TelemetryRecord record)
        {
            if (!Buckets.TryGetValue(record.Timestamp, out var bucket))
            {
                bucket = new List<TelemetryRecord>();
                Buckets.Add(record.Timestamp, bucket);
                Timestamps.Add(record.Timestamp);
            }

            bucket.Add(record);
        }

        public void Remove(TelemetryRecord record)
        {
            var bucket = Buckets[record.Timestamp];
            bucket.Remove(record);

            if (bucket.Count == 0)
            {
                Buckets.Remove(record.Timestamp);
                Timestamps.Remove(record.Timestamp);
            }
        }

        public IEnumerable<TelemetryRecord> Latest(int count)
        {
            var remaining = Math.Max(0, count);

            // SortedSet supplies descending traversal without buffering
            // the entire timestamp sequence.
            foreach (var timestamp in Timestamps.Reverse())
            {
                if (remaining == 0)
                    yield break;

                var bucket = Buckets[timestamp];

                for (var index = bucket.Count - 1; index >= 0; index--)
                {
                    if (remaining == 0)
                        yield break;

                    yield return bucket[index];
                    remaining--;
                }
            }
        }
    }

    private readonly Timeline _timeline = new();

    private readonly Dictionary<string, Timeline> _sensorTimelines =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<double[][]> _rawBatches = new();
    private readonly object _sync = new();
    private readonly int _capacity;
    private int _count;

    private const int MaxRawBatches = 256;

    public TelemetryHistoryStore(int capacity = 10000)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
    }

    public IReadOnlyList<TelemetryRecord> Records
    {
        get
        {
            lock (_sync)
            {
                var records = new List<TelemetryRecord>(_count);

                foreach (var bucket in _timeline.Buckets.Values)
                    records.AddRange(bucket);

                return records;
            }
        }
    }

    public void Add(TelemetryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.SensorId))
            throw new ArgumentException("Sensor ID is required.");

        lock (_sync)
        {
            if (record.IsNumeric)
            {
                double[][] rawBatch =
                {
                    new[] { record.NumericValue }
                };

                AddRawBatchCore(rawBatch);
            }

            _timeline.Add(record);

            if (!_sensorTimelines.TryGetValue(
                record.SensorId, out var sensorTimeline))
            {
                sensorTimeline = new Timeline();
                _sensorTimelines.Add(record.SensorId, sensorTimeline);
            }

            sensorTimeline.Add(record);
            _count++;

            while (_count > _capacity)
                RemoveOldest();
        }
    }

    private void RemoveOldest()
    {
        var timestamp = _timeline.Timestamps.Min;
        var record = _timeline.Buckets[timestamp][0];

        _timeline.Remove(record);

        var sensorTimeline = _sensorTimelines[record.SensorId];
        sensorTimeline.Remove(record);

        if (sensorTimeline.Buckets.Count == 0)
            _sensorTimelines.Remove(record.SensorId);

        _count--;
    }

    public void AddRawBatch(double[][] batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (_sync)
        {
            AddRawBatchCore(batch);
        }
    }

    private void AddRawBatchCore(double[][] batch)
    {
        _rawBatches.Add(batch);

        if (_rawBatches.Count > MaxRawBatches)
            _rawBatches.RemoveAt(0);
    }

    public List<TelemetryRecord> GetLatest(int count)
    {
        lock (_sync)
        {
            return _timeline.Latest(count).ToList();
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

            var latest = timeline.Latest(count).ToList();
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
                return _count;
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
