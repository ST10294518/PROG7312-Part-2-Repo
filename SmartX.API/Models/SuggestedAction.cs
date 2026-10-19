namespace SmartX.API.Models;

public sealed record SuggestedAction(
    Guid SourceSensorId,
    string SourceLocation,
    string FaultType,
    Guid TargetSensorId,
    string TargetLocation,
    bool TargetState,
    int Support,
    string Explanation);
