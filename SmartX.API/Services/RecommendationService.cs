using SmartX.API.Models;

namespace SmartX.API.Services;

public sealed class RecommendationService
{
    private readonly ActionLearningService _learning;
    private readonly ActiveAlertService _alerts;
    private readonly SensorService _sensors;
    private readonly CommandService _commands;

    public RecommendationService(
        ActionLearningService learning,
        ActiveAlertService alerts,
        SensorService sensors,
        CommandService commands)
    {
        _learning = learning;
        _alerts = alerts;
        _sensors = sensors;
        _commands = commands;
    }

    public List<SuggestedAction> GetSuggestions()
    {
        var activeFaults = _alerts.GetActive()
            .Select(alert => new AlertKey(alert.SensorId, alert.FaultType))
            .ToHashSet();

        var cutoff = DateTime.UtcNow.AddDays(-7);

        var candidates = _learning.GetPatterns()
            .Where(pattern =>
                pattern.Support >= 3 &&
                pattern.LastObserved >= cutoff &&
                activeFaults.Contains(new AlertKey(
                    pattern.Key.SourceSensorId,
                    pattern.Key.FaultType)))
            .ToList();

        var suggestions = new List<SuggestedAction>();

        foreach (var targetGroup in candidates.GroupBy(
            pattern => pattern.Key.TargetSensorId))
        {
            // Opposing learned actions need operator investigation.
            if (targetGroup.Select(pattern => pattern.Key.TargetState)
                .Distinct().Count() > 1)
            {
                continue;
            }

            var target = _sensors.GetById(targetGroup.Key);

            if (target is null ||
                !string.Equals(
                    target.Status, "Online",
                    StringComparison.OrdinalIgnoreCase) ||
                activeFaults.Contains(new AlertKey(
                    target.Id, "DISCONNECTED")))
            {
                continue;
            }

            var currentState = _commands.GetState(target.Id);

            if (currentState is null)
                continue;

            // Combine duplicate target actions into one recommendation,
            // using the strongest supporting association for explanation.
            var best = targetGroup
                .OrderByDescending(pattern => pattern.Support)
                .ThenByDescending(pattern => pattern.LastObserved)
                .First();

            if (currentState.Value == best.Key.TargetState)
                continue;

            var source = _sensors.GetById(best.Key.SourceSensorId);

            if (source is null)
                continue;

            var desiredState = best.Key.TargetState ? "ON" : "OFF";

            suggestions.Add(new SuggestedAction(
                source.Id,
                source.DeploymentLocation,
                best.Key.FaultType,
                target.Id,
                target.DeploymentLocation,
                best.Key.TargetState,
                best.Support,
                $"After searching this device during " +
                $"'{best.Key.FaultType}', the operator set this actuator " +
                $"{desiredState} on {best.Support} recorded occasions. " +
                $"The fault is active again. Review before applying."));
        }

        return suggestions
            .OrderByDescending(suggestion => suggestion.Support)
            .ThenBy(suggestion => suggestion.TargetSensorId)
            .Take(10)
            .ToList();
    }
}
