using SmartX.API.Models;

namespace SmartX.API.Services;

public sealed class CommandService
{
    private readonly SensorService _sensors;
    private readonly Dictionary<Guid, bool> _states = new();
    private readonly Stack<ManualCommand> _undoStack = new();
    private readonly List<CommandHistoryEntry> _history = new();
    private readonly object _sync = new();

    private const int MaxUndoCommands = 1000;
    private const int MaxHistoryEntries = 2000;

    public CommandService(SensorService sensors)
    {
        _sensors = sensors;
    }

    public bool? GetState(Guid sensorId)
    {
        lock (_sync)
        {
            if (!IsActuator(sensorId))
                return null;

            return _states.TryGetValue(sensorId, out var state)
                ? state
                : false;
        }
    }

    public int UndoCount
    {
        get
        {
            lock (_sync)
            {
                return _undoStack.Count;
            }
        }
    }

    public CommandResult Execute(Guid sensorId, bool newState)
    {
        lock (_sync)
        {
            if (!IsActuator(sensorId))
                return new(false, "Select a registered actuator.", null, null);

            var previousState = _states.TryGetValue(sensorId, out var state)
                && state;

            if (previousState == newState)
            {
                return new(
                    false,
                    "The actuator is already in the requested state.",
                    null,
                    previousState);
            }

            if (_undoStack.Count >= MaxUndoCommands)
            {
                return new(
                    false,
                    "Command history is full. Undo a command before continuing.",
                    null,
                    previousState);
            }

            var command = new ManualCommand(
                Guid.NewGuid(),
                sensorId,
                previousState,
                newState,
                DateTime.UtcNow);

            _states[sensorId] = newState;
            _undoStack.Push(command);

            AddHistory(new CommandHistoryEntry(
                command.Id,
                sensorId,
                "Execute",
                previousState,
                newState,
                command.IssuedAt));

            return new(true, "Actuator command applied.", command, newState);
        }
    }

    public CommandResult Undo()
    {
        lock (_sync)
        {
            if (!_undoStack.TryPeek(out var command))
                return new(false, "There are no commands to undo.", null, null);

            if (!IsActuator(command.SensorId))
            {
                return new(
                    false,
                    "The target actuator is unavailable. Command retained.",
                    command,
                    null);
            }

            _states[command.SensorId] = command.PreviousState;
            _undoStack.Pop();

            AddHistory(new CommandHistoryEntry(
                command.Id,
                command.SensorId,
                "Undo",
                command.NewState,
                command.PreviousState,
                DateTime.UtcNow));

            return new(
                true,
                "Previous actuator state restored.",
                command,
                command.PreviousState);
        }
    }

    public List<CommandHistoryEntry> GetHistory(int count = 50)
    {
        lock (_sync)
        {
            return _history
                .AsEnumerable()
                .Reverse()
                .Take(Math.Clamp(count, 0, 500))
                .ToList();
        }
    }

    private bool IsActuator(Guid sensorId)
    {
        var sensor = _sensors.GetById(sensorId);

        return sensor is not null &&
            string.Equals(
                sensor.SensorCategory,
                "Actuator",
                StringComparison.OrdinalIgnoreCase);
    }

    private void AddHistory(CommandHistoryEntry entry)
    {
        _history.Add(entry);

        if (_history.Count > MaxHistoryEntries)
            _history.RemoveAt(0);
    }
}
