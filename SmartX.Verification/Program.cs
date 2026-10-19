using SmartX.API.DataStructures;
using SmartX.API.Models;

var passed = 0;

void Check(bool condition, string description)
{
    if (!condition)
        throw new Exception("FAIL: " + description);

    passed++;
    Console.WriteLine("PASS: " + description);
}

var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

TelemetryRecord Reading(string sensor, int offset, double value) => new()
{
    SensorId = sensor,
    Timestamp = start.AddTicks(offset),
    Metric = "Temperature",
    ValueType = "float",
    Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture),
    NumericValue = value,
    IsNumeric = true
};

var store = new TelemetryHistoryStore(3);

var newest = Reading("sensor-a", 30, 30);
var oldest = Reading("sensor-b", 10, 10);
var middle = Reading("sensor-a", 20, 20);

store.Add(newest);
store.Add(oldest);
store.Add(middle);

Check(
    store.GetLatest(3).Select(record => record.Id).SequenceEqual(
        new[] { newest.Id, middle.Id, oldest.Id }),
    "Out-of-order insertions are returned newest first.");

var snapshot = store.Records;
var later = Reading("sensor-b", 40, 40);
store.Add(later);

Check(
    store.Count == 3 &&
    !store.Records.Any(record => record.Id == oldest.Id),
    "Capacity eviction removes the oldest timestamp.");

Check(
    store.GetSensorHistory("sensor-b").Count == 1 &&
    store.GetSensorHistory("sensor-b")[0].Id == later.Id,
    "Eviction updates the device-specific timeline.");

Check(
    snapshot.Count == 3 &&
    snapshot.Any(record => record.Id == oldest.Id),
    "Earlier snapshots retain their membership.");

var stale = Reading("sensor-a", 5, 5);
store.Add(stale);

Check(
    store.Count == 3 &&
    !store.Records.Any(record => record.Id == stale.Id),
    "A late-arriving old record cannot displace newer retained history.");

var tied = Reading("sensor-a", 30, 31);
store.Add(tied);

var sensorHistory = store.GetSensorHistory("sensor-a");

Check(
    sensorHistory.Select(record => record.Id).SequenceEqual(
        new[] { newest.Id, tied.Id }),
    "Identical timestamps retain both records in stable order.");

Check(
    store.GetLatest(2).Select(record => record.Id).SequenceEqual(
        new[] { later.Id, tied.Id }),
    "Latest-count selection remains correct after eviction and ties.");

Check(
    store.GetLatest(0).Count == 0 &&
    store.GetSensorHistory("sensor-a", 0).Count == 0 &&
    store.GetSensorHistory("missing").Count == 0,
    "Empty and missing-device requests return empty results.");

var single = new TelemetryHistoryStore(1);
single.Add(Reading("removed-device", 1, 1));
single.Add(Reading("retained-device", 2, 2));

Check(
    single.GetSensorHistory("removed-device").Count == 0 &&
    single.GetSensorHistory("retained-device").Count == 1,
    "A fully evicted device leaves no residual history.");

var bounded = new TelemetryHistoryStore();

for (var i = 0; i < 10050; i++)
    bounded.Add(Reading("load-device", i, i));

Check(
    bounded.Count == 10000 && bounded.Records.Count == 10000,
    "Default retention is bounded at 10,000 readings.");

Check(
    bounded.GetSensorHistory("load-device", 10000)[0].NumericValue == 50 &&
    bounded.GetLatest(1)[0].NumericValue == 10049,
    "Default retention preserves the newest 10,000 timestamps.");

Check(
    bounded.RawBatchCount == 256,
    "Numeric raw batches are bounded at 256.");

var rawOnly = new TelemetryHistoryStore();

for (var i = 0; i < 300; i++)
    rawOnly.AddRawBatch(new[] { new[] { (double)i } });

Check(
    rawOnly.RawBatchCount == 256 && rawOnly.Count == 0,
    "Direct raw-batch insertion respects its independent limit.");

var rejectedCapacity = false;

try
{
    _ = new TelemetryHistoryStore(0);
}
catch (ArgumentOutOfRangeException)
{
    rejectedCapacity = true;
}

Check(rejectedCapacity, "Invalid retention capacity is rejected.");

Console.WriteLine($"All {passed} retention and ordering checks passed.");
