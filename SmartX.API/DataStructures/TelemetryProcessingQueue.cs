using SmartX.API.Models;

namespace SmartX.API.DataStructures;

public sealed record QueuedTelemetry(
    Guid Id,
    TelemetryInput Input,
    int Priority,
    DateTime EnqueuedAt);

public sealed class TelemetryProcessingQueue
{
    private readonly Queue<QueuedTelemetry> _standard = new();

    private readonly PriorityQueue<
        QueuedTelemetry,
        (int Urgency, long Sequence)> _urgent = new();

    private readonly object _sync = new();
    private readonly int _capacity;
    private long _sequence;
    private long _accepted;
    private long _dequeued;
    private long _rejected;

    public TelemetryProcessingQueue(int capacity = 10000)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
    }

    public bool TryEnqueue(
        TelemetryInput input,
        int priority,
        out QueuedTelemetry? queued)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (priority < 0 || priority > 2)
            throw new ArgumentOutOfRangeException(nameof(priority));

        lock (_sync)
        {
            queued = null;

            if (_standard.Count + _urgent.Count >= _capacity)
            {
                _rejected++;
                return false;
            }

            // Own a copy of the request and its JSON value.
            var copy = new TelemetryInput
            {
                SensorId = input.SensorId,
                Metric = input.Metric,
                ValueType = input.ValueType,
                Value = input.Value.Clone()
            };

            queued = new QueuedTelemetry(
                Guid.NewGuid(),
                copy,
                priority,
                DateTime.UtcNow);

            if (priority == 0)
            {
                _standard.Enqueue(queued);
            }
            else
            {
                // .NET removes the lowest priority key first.
                // Negation makes higher urgency run first.
                _urgent.Enqueue(queued, (-priority, _sequence++));
            }

            _accepted++;
            return true;
        }
    }

    public bool TryDequeue(out QueuedTelemetry? queued)
    {
        lock (_sync)
        {
            if (_urgent.TryDequeue(out var urgent, out _))
            {
                queued = urgent;
            }
            else if (_standard.TryDequeue(out var standard))
            {
                queued = standard;
            }
            else
            {
                queued = null;
                return false;
            }

            _dequeued++;
            return true;
        }
    }

    public object GetStatus()
    {
        lock (_sync)
        {
            return new
            {
                standardPending = _standard.Count,
                urgentPending = _urgent.Count,
                totalPending = _standard.Count + _urgent.Count,
                capacity = _capacity,
                accepted = _accepted,
                dequeued = _dequeued,
                rejected = _rejected
            };
        }
    }
}
