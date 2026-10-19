using SmartX.API.DataStructures;

namespace SmartX.API.Services;

public sealed class TelemetryQueueProcessor : BackgroundService
{
    private readonly TelemetryProcessingQueue _queue;
    private readonly TelemetryService _telemetry;
    private readonly ActiveAlertService _alerts;
    private readonly ILogger<TelemetryQueueProcessor> _logger;

    private long _processed;
    private long _failed;

    public TelemetryQueueProcessor(
        TelemetryProcessingQueue queue,
        TelemetryService telemetry,
        ActiveAlertService alerts,
        ILogger<TelemetryQueueProcessor> logger)
    {
        _queue = queue;
        _telemetry = telemetry;
        _alerts = alerts;
        _logger = logger;
    }

    public object GetStatus()
    {
        return new
        {
            queue = _queue.GetStatus(),
            processed = Interlocked.Read(ref _processed),
            failed = Interlocked.Read(ref _failed)
        };
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var handled = 0;

                // Limit each batch so the worker yields regularly.
                while (handled < 256 &&
                       !stoppingToken.IsCancellationRequested &&
                       _queue.TryDequeue(out var packet))
                {
                    if (packet is null)
                        continue;

                    try
                    {
                        var result = _telemetry.Process(packet.Input);

                        if (result.Success)
                        {
                            if (result.Record is not null)
                            {
                                var record = result.Record;
                                var sensorId = Guid.Parse(record.SensorId);
                                var faultType = "Telemetry:" + record.Metric.Trim();

                                if (record.IsAnomaly)
                                {
                                    var isNew = _alerts.Raise(
                                        sensorId,
                                        faultType,
                                        record.AlertMessage);

                                    if (isNew)
                                    {
                                        _logger.LogInformation(
                                            "New active fault for {SensorId}: {FaultType}",
                                            sensorId,
                                            faultType);
                                    }
                                }
                                else
                                {
                                    _alerts.Resolve(sensorId, faultType);
                                }
                            }

                            Interlocked.Increment(ref _processed);
                        }
                        else
                        {
                            Interlocked.Increment(ref _failed);

                            _logger.LogWarning(
                                "Telemetry packet {PacketId} failed: {Reason}",
                                packet.Id,
                                result.Message);
                        }
                    }
                    catch (Exception exception)
                    {
                        Interlocked.Increment(ref _failed);

                        _logger.LogError(
                            exception,
                            "Unexpected failure processing packet {PacketId}",
                            packet.Id);
                    }

                    handled++;
                }

                if (handled == 0)
                {
                    await Task.Delay(20, stoppingToken);
                }
                else
                {
                    await Task.Yield();
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }
}

