using System.Collections;
using SmartX.API.Models;

namespace SmartX.API.DataStructures;

public class SensorCollection : IEnumerable<Sensor>
{
    private readonly List<Sensor> _items = new();

    public int Count => _items.Count;

    public void Add(Sensor sensor)
    {
        _items.Add(sensor);
    }

    public bool Remove(Guid id)
    {
        var sensor = _items.FirstOrDefault(x => x.Id == id);

        if (sensor == null)
            return false;

        return _items.Remove(sensor);
    }

    public Sensor? FindById(Guid id)
    {
        return _items.FirstOrDefault(x => x.Id == id);
    }

    public Sensor? FindByMac(string macAddress)
    {
        return _items.FirstOrDefault(
            x => x.MacAddress.Equals(
                macAddress,
                StringComparison.OrdinalIgnoreCase));
    }

    public List<Sensor> ToList()
    {
        return new List<Sensor>(_items);
    }

    public IEnumerator<Sensor> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}