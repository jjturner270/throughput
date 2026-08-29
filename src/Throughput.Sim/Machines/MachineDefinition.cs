namespace Throughput.Sim;

public sealed record MachineDefinition(
    string Id,
    string ProcessId,
    long CycleTimeMs,
    GridPosition Position,
    string InputQueueId,
    string FeedsQueueId);
