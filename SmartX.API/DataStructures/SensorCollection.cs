using System.Collections;
using SmartX.API.Models;

namespace SmartX.API.DataStructures;

public class SensorCollection : IEnumerable<Sensor>
{
    private readonly List<Sensor> _items = new();
    private readonly Dictionary<Guid, Sensor> _byId = new();
    private readonly Dictionary<string, Sensor> _byMac =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    private static string NormaliseMac(string macAddress)
    {
        return macAddress.Trim().Replace('-', ':').ToUpperInvariant();
    }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _items.Count;
            }
        }
    }

    public void Add(Sensor sensor)
    {
        ArgumentNullException.ThrowIfNull(sensor);
        var macKey = NormaliseMac(sensor.MacAddress);

        lock (_sync)
        {
            if (_byId.ContainsKey(sensor.Id))
                throw new InvalidOperationException(
                    "A sensor with this ID already exists.");

            if (_byMac.ContainsKey(macKey))
                throw new InvalidOperationException(
                    "A sensor with this MAC address already exists.");

            sensor.MacAddress = macKey;
            _items.Add(sensor);
            _byId.Add(sensor.Id, sensor);
            _byMac.Add(macKey, sensor);
        }
    }

    public bool Remove(Guid id)
    {
        lock (_sync)
        {
            if (!_byId.TryGetValue(id, out var sensor))
                return false;

            _byId.Remove(id);
            _byMac.Remove(NormaliseMac(sensor.MacAddress));
            _items.Remove(sensor);
            return true;
        }
    }

    public Sensor? FindById(Guid id)
    {
        lock (_sync)
        {
            return _byId.TryGetValue(id, out var sensor)
                ? sensor
                : null;
        }
    }

    public Sensor? FindByMac(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return null;

        var macKey = NormaliseMac(macAddress);

        lock (_sync)
        {
            return _byMac.TryGetValue(macKey, out var sensor)
                ? sensor
                : null;
        }
    }

    public List<Sensor> ToList()
    {
        lock (_sync)
        {
            return new List<Sensor>(_items);
        }
    }

    public IEnumerator<Sensor> GetEnumerator()
    {
        return ToList().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
