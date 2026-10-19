using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SmartX.API.Controllers;
using SmartX.API.DataStructures;
using SmartX.API.Models;
using SmartX.API.Services;

public static class ConnectivityChecks
{
    public static int Run()
    {
        var passed = 0;

        void Check(bool condition, string description)
        {
            if (!condition)
                throw new Exception("FAIL: " + description);

            passed++;
            Console.WriteLine("PASS: " + description);
        }

        var sensors = new SensorService();
        var telemetry = new TelemetryService(sensors);
        var alerts = new ActiveAlertService();
        var queue = new TelemetryProcessingQueue(1);

        using var worker = new TelemetryQueueProcessor(
            queue,
            telemetry,
            alerts,
            NullLogger<TelemetryQueueProcessor>.Instance);

        var controller = new TelemetryController(
            telemetry,
            sensors,
            new TelemetryInputValidator(sensors),
            queue,
            worker);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };

        var sensor = sensors.GetAll()
            .First(item => item.SensorCategory == "Environmental");

        sensor.LastSeen = DateTime.UtcNow.AddMinutes(-1);

        void Inspect(DateTime cutoff)
        {
            sensors.InspectConnections(cutoff, (device, disconnected) =>
            {
                if (disconnected)
                {
                    if (!alerts.IsActive(device.Id, "Disconnected"))
                        alerts.Raise(device.Id, "Disconnected", "Offline.");
                }
                else
                {
                    alerts.Resolve(device.Id, "Disconnected");
                }
            });
        }

        TelemetryInput Input() => new()
        {
            SensorId = sensor.Id.ToString(),
            Metric = "Temperature",
            ValueType = "float",
            Value = JsonSerializer.SerializeToElement(25f)
        };

        Inspect(DateTime.UtcNow.AddSeconds(-15));

        Check(
            sensor.Status == "Disconnected" &&
            alerts.IsActive(sensor.Id, "Disconnected"),
            "A stale device is marked disconnected.");

        var result = controller.PostTelemetry(Input());
        var acceptedAt = sensor.LastSeen;

        Check(
            result is AcceptedResult &&
            sensor.Status == "Online" &&
            acceptedAt > DateTime.UtcNow.AddSeconds(-5),
            "An accepted HTTP arrival refreshes device activity.");

        Check(
            sensor.TotalReadings == 0 &&
            telemetry.GetSensorHistory(sensor.Id.ToString()).Count == 0,
            "Activity is refreshed before queued processing starts.");

        Inspect(DateTime.UtcNow.AddSeconds(-15));

        Check(
            !alerts.IsActive(sensor.Id, "Disconnected"),
            "Connection inspection clears the recovered device's disconnect alert.");

        var rejected = controller.PostTelemetry(Input());

        Check(
            rejected is ObjectResult { StatusCode: 503 } &&
            sensor.LastSeen == acceptedAt &&
            sensor.TotalReadings == 0,
            "A full-queue rejection does not refresh activity or reading counts.");

        // Simulate the accepted packet waiting beyond the timeout.
        Inspect(acceptedAt.AddSeconds(1));

        Check(
            sensor.Status == "Disconnected" &&
            alerts.IsActive(sensor.Id, "Disconnected"),
            "A device can time out while old work remains queued.");

        if (!queue.TryDequeue(out var pending) || pending is null)
            throw new Exception("Expected queued packet was missing.");

        var processed = telemetry.Process(pending.Input, pending.EnqueuedAt);

        Check(
            processed.Success &&
            sensor.TotalReadings == 1 &&
            sensor.LastSeen == acceptedAt &&
            sensor.Status == "Disconnected",
            "Delayed processing increments readings without faking reconnection.");

        var recovery = controller.PostTelemetry(Input());
        Inspect(DateTime.UtcNow.AddSeconds(-15));

        Check(
            recovery is AcceptedResult &&
            sensor.Status == "Online" &&
            !alerts.IsActive(sensor.Id, "Disconnected"),
            "A genuinely new accepted arrival restores connectivity.");

        var newer = sensor.LastSeen.AddSeconds(1);
        sensors.MarkReceived(sensor.Id, newer);
        sensors.MarkReceived(sensor.Id, newer.AddSeconds(-1));

        Check(
            sensor.LastSeen == newer,
            "Out-of-order activity timestamps cannot move last-seen backward.");

        return passed;
    }
}

