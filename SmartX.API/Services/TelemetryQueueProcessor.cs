using SmartX.API.DataStructures;

namespace SmartX.API.Services;

public sealed class TelemetryQueueProcessor : BackgroundService
{
    private readonly TelemetryProcessingQueue _queue;
    private readonly TelemetryService _telemetry;
    private readonly ILogger<TelemetryQueueProcessor> _logger;

    private long _processed;
    private long _failed;

    public TelemetryQueueProcessor(
        TelemetryProcessingQueue queue,
        TelemetryService telemetry,
        ILogger<TelemetryQueueProcessor> logger)
    {
        _queue = queue;
        _telemetry = telemetry;
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
