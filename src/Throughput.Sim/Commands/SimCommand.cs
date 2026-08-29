namespace Throughput.Sim;

public enum CommandType
{
    MoveMachine,
    SetWorkerPriority,
    Pause
}

public sealed record SimCommand(
    long Time,
    CommandType Type,
    string? MachineId = null,
    GridPosition? Position = null,
    string? WorkerId = null,
    IReadOnlyList<string>? Priorities = null);
