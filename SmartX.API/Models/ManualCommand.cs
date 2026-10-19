namespace SmartX.API.Models;

public sealed record ManualCommand(
    Guid Id,
    Guid SensorId,
    bool PreviousState,
    bool NewState,
    DateTime IssuedAt);

public sealed record CommandHistoryEntry(
    Guid CommandId,
    Guid SensorId,
    string Action,
    bool PreviousState,
    bool NewState,
    DateTime Timestamp);

public sealed record CommandResult(
    bool Success,
    string Message,
    ManualCommand? Command,
    bool? CurrentState);
