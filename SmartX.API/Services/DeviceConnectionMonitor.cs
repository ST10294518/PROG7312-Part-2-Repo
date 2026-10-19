namespace SmartX.API.Services;

public sealed class DeviceConnectionMonitor : BackgroundService
{
    private readonly SensorService _sensors;
    private readonly ActiveAlertService _alerts;
    private readonly ILogger<DeviceConnectionMonitor> _logger;
    private readonly TimeSpan _timeout;

    public DeviceConnectionMonitor(
        SensorService sensors,
        ActiveAlertService alerts,
        IConfiguration configuration,
        ILogger<DeviceConnectionMonitor> logger)
    {
        _sensors = sensors;
        _alerts = alerts;
        _logger = logger;

        var seconds = configuration.GetValue<int?>(
            "SmartX:DisconnectTimeoutSeconds") ?? 15;

        _timeout = TimeSpan.FromSeconds(Math.Clamp(seconds, 5, 3600));
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                CheckConnections();
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }

    private void CheckConnections()
    {
        var cutoff = DateTime.UtcNow - _timeout;

        // Activity updates and connection inspection share the sensor lock.
        _sensors.InspectConnections(cutoff, (sensor, disconnected) =>
        {
            if (disconnected)
            {
                if (!_alerts.IsActive(sensor.Id, "Disconnected") &&
                    _alerts.Raise(
                        sensor.Id,
                        "Disconnected",
                        $"No accepted telemetry received for at least " +
                        $"{_timeout.TotalSeconds:0} seconds."))
                {
                    _logger.LogInformation(
                        "Device {SensorId} disconnected.", sensor.Id);
                }
            }
            else if (_alerts.Resolve(sensor.Id, "Disconnected"))
            {
                _logger.LogInformation(
                    "Device {SensorId} reconnected.", sensor.Id);
            }
        });
    }
}
