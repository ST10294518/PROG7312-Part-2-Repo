using System.Net;
using System.Net.Http.Json;
using SmartX.API.Models;

namespace SmartX.API.Services;

public sealed class TelemetrySimulationService : BackgroundService
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<TelemetrySimulationService> _logger;
    private readonly SemaphoreSlim _cycleLock = new(1, 1);
    private readonly HashSet<Guid> _pausedSensors = new();

    public TelemetrySimulationService(
        IHttpClientFactory clients,
        ILogger<TelemetrySimulationService> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task SetPausedAsync(
        Guid sensorId,
        bool paused,
        CancellationToken cancellationToken)
    {
        await _cycleLock.WaitAsync(cancellationToken);

        try
        {
            if (paused)
                _pausedSensors.Add(sensorId);
            else
                _pausedSensors.Remove(sensorId);
        }
        finally
        {
            _cycleLock.Release();
        }
    }

    public async Task<List<Guid>> GetPausedAsync(
        CancellationToken cancellationToken)
    {
        await _cycleLock.WaitAsync(cancellationToken);

        try
        {
            return _pausedSensors.OrderBy(id => id).ToList();
        }
        finally
        {
            _cycleLock.Release();
        }
    }

    public async Task<int> RunCycleAsync(CancellationToken cancellationToken)
    {
        await _cycleLock.WaitAsync(cancellationToken);

        try
        {
            using var client = _clients.CreateClient("SmartXSimulator");

            var sensors = await client.GetFromJsonAsync<List<Sensor>>(
                "api/Sensor", cancellationToken) ?? new List<Sensor>();

            var accepted = 0;

            foreach (var sensor in sensors)
            {
                if (_pausedSensors.Contains(sensor.Id))
                    continue;

                if (sensor.SensorCategory == "Environmental")
                {
                    var temperature = Random.Shared.Next(10) == 0
                        ? 46f
                        : 18f + Random.Shared.NextSingle() * 12f;

                    accepted += await SendAsync(
                        client, sensor.Id, "Temperature", "float",
                        temperature, cancellationToken);

                    accepted += await SendAsync(
                        client, sensor.Id, "Moisture", "float",
                        Random.Shared.NextSingle() * 100f, cancellationToken);
                }
                else if (sensor.SensorCategory == "Power Consumption")
                {
                    accepted += await SendAsync(
                        client, sensor.Id, "Power", "int",
                        Random.Shared.Next(500, 7001), cancellationToken);
                }
                else if (sensor.SensorCategory == "Actuator")
                {
                    accepted += await SendAsync(
                        client, sensor.Id, "ValveState", "bool",
                        await GetActuatorStateAsync(client, sensor.Id, cancellationToken), cancellationToken);
                }
            }

            return accepted;
        }
        finally
        {
            _cycleLock.Release();
        }
    }

    private static async Task<bool> GetActuatorStateAsync(
        HttpClient client,
        Guid sensorId,
        CancellationToken cancellationToken)
    {
        var response = await client.GetFromJsonAsync<ActuatorStateResponse>(
            $"api/Command/state/{sensorId}", cancellationToken);

        if (response is null)
            throw new InvalidOperationException("Actuator state response was empty.");

        return response.State;
    }

    private async Task<int> SendAsync<T>(
        HttpClient client,
        Guid sensorId,
        string metric,
        string valueType,
        T value,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "api/Telemetry",
            new
            {
                sensorId = sensorId.ToString(),
                metric,
                valueType,
                value
            },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Accepted)
            return 1;

        _logger.LogWarning(
            "Simulated telemetry for {SensorId} received HTTP {StatusCode}",
            sensorId,
            (int)response.StatusCode);

        return 0;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            // Allow the HTTP server to start before the first request.
            await Task.Delay(1000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Simulation cycle failed; retrying on the next cycle.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }
}


