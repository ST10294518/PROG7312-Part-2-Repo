namespace SmartX.API.Models;

public sealed record DeviceSearchObservation(
    Guid SensorId,
    string Query,
    DateTime Timestamp,
    IReadOnlyList<string> ActiveFaults);

public sealed record ActionPatternKey(
    Guid SourceSensorId,
    string FaultType,
    Guid TargetSensorId,
    bool TargetState);

public sealed record LearnedActionPattern(
    ActionPatternKey Key,
    int Support,
    DateTime LastObserved);
