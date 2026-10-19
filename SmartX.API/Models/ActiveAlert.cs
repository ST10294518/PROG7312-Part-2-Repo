namespace SmartX.API.Models;

public sealed record AlertKey(Guid SensorId, string FaultType);

public sealed record ActiveAlert(
    Guid SensorId,
    string FaultType,
    string Message,
    DateTime FirstSeen,
    DateTime LastSeen,
    int Occurrences);
