namespace Throughput.Sim;

public sealed record WorkerDefinition(
    string Id,
    int SpeedMmPerSec,
    GridPosition StartingPosition,
    IReadOnlyList<string> DefaultPriority);
