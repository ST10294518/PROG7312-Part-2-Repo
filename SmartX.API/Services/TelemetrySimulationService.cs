using System.Text.Json;
using SmartX.API.Models;

namespace SmartX.API.Services;

public class TelemetrySimulationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Random _random = new();

    public TelemetrySimulationService(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _serviceProvider.CreateScope();

                var sensorService =
                    scope.ServiceProvider
                        .GetRequiredService<SensorService>();

                var telemetryService =
                    scope.ServiceProvider
                        .GetRequiredService<TelemetryService>();

                var sensors =
                    sensorService.GetAll();

                foreach (var sensor in sensors)
                {
                    if (sensor.SensorCategory ==
                        "Environmental")
                    {
                        Send(
                            telemetryService,
                            sensor,
                            "Temperature",
                            "float",
                            GenerateTemperature());

                        Send(
                            telemetryService,
                            sensor,
                            "Moisture",
                            "float",
                            _random.NextDouble() * 100);
                    }
                    else if (
                        sensor.SensorCategory ==
                        "Power Consumption")
                    {
                        Send(
                            telemetryService,
                            sensor,
                            "Power",
                            "int",
                            _random.Next(500, 5000));
                    }
                    else
                    {
                        Send(
                            telemetryService,
                            sensor,
                            "ValveState",
                            "bool",
                            _random.Next(0, 2) == 1);
                    }
                }
            }
            catch
            {
                // Keep the simulator alive.
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }

    private double GenerateTemperature()
    {
        // Occasionally create an anomaly
        // so the dashboard can demonstrate alerts.
        if (_random.Next(0, 10) == 0)
            return _random.Next(36, 46);

        return 18 + _random.NextDouble() * 12;
    }

    private static void Send(
        TelemetryService service,
        Sensor sensor,
        string metric,
        string type,
        object value)
    {
        var json =
            JsonSerializer.SerializeToElement(value);

        service.Process(
            new TelemetryInput
            {
                SensorId = sensor.Id.ToString(),
                Metric = metric,
                ValueType = type,
                Value = json
            });
    }
}