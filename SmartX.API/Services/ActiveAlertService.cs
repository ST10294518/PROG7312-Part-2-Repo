using SmartX.API.Models;

namespace SmartX.API.Services;

public sealed class ActiveAlertService
{
    private readonly HashSet<AlertKey> _activeKeys = new();
    private readonly Dictionary<AlertKey, ActiveAlert> _details = new();
    private readonly object _sync = new();

    private static AlertKey CreateKey(Guid sensorId, string faultType)
    {
        if (sensorId == Guid.Empty)
            throw new ArgumentException("Sensor ID is required.", nameof(sensorId));

        if (string.IsNullOrWhiteSpace(faultType))
            throw new ArgumentException("Fault type is required.", nameof(faultType));

        return new AlertKey(
            sensorId,
            faultType.Trim().ToUpperInvariant());
    }

    public bool Raise(Guid sensorId, string faultType, string message)
    {
        var key = CreateKey(sensorId, faultType);
        var now = DateTime.UtcNow;

        lock (_sync)
        {
            if (_activeKeys.Add(key))
            {
                _details.Add(key, new ActiveAlert(
                    sensorId,
                    key.FaultType,
                    message,
                    now,
                    now,
                    1));

                return true;
            }

            var existing = _details[key];

            _details[key] = existing with
            {
                Message = message,
                LastSeen = now,
                Occurrences = existing.Occurrences == int.MaxValue
                    ? int.MaxValue
                    : existing.Occurrences + 1
            };

            return false;
        }
    }

    public bool Resolve(Guid sensorId, string faultType)
    {
        var key = CreateKey(sensorId, faultType);

        lock (_sync)
        {
            if (!_activeKeys.Remove(key))
                return false;

            _details.Remove(key);
            return true;
        }
    }

    public bool IsActive(Guid sensorId, string faultType)
    {
        var key = CreateKey(sensorId, faultType);

        lock (_sync)
        {
            return _activeKeys.Contains(key);
        }
    }

    public List<ActiveAlert> GetActive(Guid? sensorId = null)
    {
        lock (_sync)
        {
            return _details.Values
                .Where(alert =>
                    sensorId is null || alert.SensorId == sensorId.Value)
                .OrderByDescending(alert => alert.LastSeen)
                .ThenBy(alert => alert.SensorId)
                .ThenBy(alert => alert.FaultType)
                .ToList();
        }
    }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _activeKeys.Count;
            }
        }
    }
}
