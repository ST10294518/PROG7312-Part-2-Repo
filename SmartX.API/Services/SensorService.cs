using SmartX.API.DataStructures;
using SmartX.API.Models;

namespace SmartX.API.Services;

public class SensorService
{
    private readonly SensorCollection _sensors = new();

    public SensorService()
    {
        SeedSensors();
    }

    public List<Sensor> GetAll()
    {
        return _sensors.ToList();
    }

    public Sensor? GetById(Guid id)
    {
        return _sensors.FindById(id);
    }

    public Sensor? GetByMac(string mac)
    {
        return _sensors.FindByMac(mac);
    }

    public (bool Success, string Message, Sensor? Sensor)
        Register(
            string macAddress,
            string deploymentLocation,
            string sensorCategory)
    {
        macAddress = macAddress.Trim();
        deploymentLocation = deploymentLocation.Trim();
        sensorCategory = sensorCategory.Trim();

        if (string.IsNullOrWhiteSpace(macAddress))
            return (false, "MAC address is required.", null);

        if (!IsValidMacAddress(macAddress))
            return (false, "Invalid MAC address format.", null);

        if (string.IsNullOrWhiteSpace(deploymentLocation))
            return (false, "Deployment location is required.", null);

        if (string.IsNullOrWhiteSpace(sensorCategory))
            return (false, "Sensor category is required.", null);

        if (_sensors.FindByMac(macAddress) != null)
            return (false, "A sensor with this MAC address already exists.", null);

        var sensor = new Sensor
        {
            MacAddress = macAddress,
            DeploymentLocation = deploymentLocation,
            SensorCategory = sensorCategory
        };

        _sensors.Add(sensor);

        return (true, "Sensor registered successfully.", sensor);
    }

    public void UpdateLastSeen(string sensorId)
    {
        if (!Guid.TryParse(sensorId, out var id))
            return;

        var sensor = _sensors.FindById(id);

        if (sensor == null)
            return;

        sensor.LastSeen = DateTime.UtcNow;
        sensor.Status = "Online";
        sensor.TotalReadings++;
    }

    public void UpdateAlertCount(string sensorId)
    {
        if (!Guid.TryParse(sensorId, out var id))
            return;

        var sensor = _sensors.FindById(id);

        if (sensor != null)
            sensor.TotalAlerts++;
    }

    private static bool IsValidMacAddress(string mac)
    {
        var parts = mac.Split(':', '-');

        if (parts.Length != 6)
            return false;

        return parts.All(part =>
            part.Length == 2 &&
            part.All(c => Uri.IsHexDigit(c)));
    }
    public void CheckDisconnectedSensors(int timeoutSeconds = 15)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

        foreach (var sensor in _sensors)
        {
            if (sensor.LastSeen < cutoff)
            {
                sensor.Status = "Disconnected";
            }
        }
    }

    private void SeedSensors()
    {
        Register(
            "AA:BB:CC:DD:EE:01",
            "Greenhouse Zone A",
            "Environmental");

        Register(
            "AA:BB:CC:DD:EE:02",
            "Utility Room",
            "Power Consumption");

        Register(
            "AA:BB:CC:DD:EE:03",
            "Control Room",
            "Actuator");
    }

}