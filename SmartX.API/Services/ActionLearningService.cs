using SmartX.API.Models;

namespace SmartX.API.Services;

public sealed class ActionLearningService
{
    private readonly Queue<DeviceSearchObservation> _searches = new();
    private readonly Dictionary<ActionPatternKey, LearnedActionPattern>
        _patterns = new();

    private readonly HashSet<Guid> _recordedCommands = new();
    private readonly Queue<Guid> _commandOrder = new();
    private readonly object _sync = new();
    private readonly TimeProvider _clock;

    private DeviceSearchObservation? _latestSearch;

    private const int MaxSearches = 200;
    private const int MaxPatterns = 1000;
    private const int MaxRecordedCommands = 2000;

    public ActionLearningService() : this(TimeProvider.System)
    {
    }

    public ActionLearningService(TimeProvider clock)
    {
        _clock = clock;
    }

    public void RecordSearch(
        Guid sensorId,
        string query,
        IEnumerable<ActiveAlert> activeAlerts)
    {
        if (sensorId == Guid.Empty)
            throw new ArgumentException("Sensor ID is required.");

        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length > 100)
            throw new ArgumentException("Search text must contain 1-100 characters.");

        var faults = activeAlerts
            .Where(alert => alert.SensorId == sensorId)
            .Select(alert => alert.FaultType.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();

        var observation = new DeviceSearchObservation(
            sensorId,
            query.Trim(),
            _clock.GetUtcNow().UtcDateTime,
            Array.AsReadOnly(faults));

        lock (_sync)
        {
            _latestSearch = observation;
            _searches.Enqueue(observation);

            while (_searches.Count > MaxSearches)
                _searches.Dequeue();
        }
    }

    public int RecordCommand(
        ManualCommand command,
        IEnumerable<ActiveAlert> currentAlerts)
    {
        if (command.PreviousState == command.NewState)
            return 0;

        var currentFaults = currentAlerts
            .Select(alert => new AlertKey(
                alert.SensorId,
                alert.FaultType.Trim().ToUpperInvariant()))
            .ToHashSet();

        lock (_sync)
        {
            if (!_recordedCommands.Add(command.Id))
                return 0;

            _commandOrder.Enqueue(command.Id);

            while (_commandOrder.Count > MaxRecordedCommands)
                _recordedCommands.Remove(_commandOrder.Dequeue());

            var now = _clock.GetUtcNow().UtcDateTime;
            var search = _latestSearch;

            if (search is null ||
                now < search.Timestamp ||
                now - search.Timestamp > TimeSpan.FromMinutes(2) ||
                command.IssuedAt < search.Timestamp)
            {
                return 0;
            }

            var learned = 0;

            foreach (var fault in search.ActiveFaults)
            {
                // Learn only if the searched device's fault is still active.
                if (!currentFaults.Contains(new AlertKey(search.SensorId, fault)))
                    continue;

                var key = new ActionPatternKey(
                    search.SensorId,
                    fault,
                    command.SensorId,
                    command.NewState);

                if (_patterns.TryGetValue(key, out var existing))
                {
                    _patterns[key] = existing with
                    {
                        Support = existing.Support == int.MaxValue
                            ? int.MaxValue
                            : existing.Support + 1,
                        LastObserved = now
                    };
                }
                else
                {
                    if (_patterns.Count >= MaxPatterns)
                    {
                        var oldest = _patterns
                            .MinBy(entry => entry.Value.LastObserved).Key;

                        _patterns.Remove(oldest);
                    }

                    _patterns.Add(key, new LearnedActionPattern(key, 1, now));
                }

                learned++;
            }

            // A search supplies context to one successful command only.
            _latestSearch = null;
            return learned;
        }
    }

    public List<DeviceSearchObservation> GetSearchHistory(int count = 50)
    {
        lock (_sync)
        {
            return _searches
                .Reverse()
                .Take(Math.Clamp(count, 0, MaxSearches))
                .ToList();
        }
    }

    public List<LearnedActionPattern> GetPatterns()
    {
        lock (_sync)
        {
            return _patterns.Values
                .OrderByDescending(pattern => pattern.Support)
                .ThenByDescending(pattern => pattern.LastObserved)
                .ToList();
        }
    }
}
