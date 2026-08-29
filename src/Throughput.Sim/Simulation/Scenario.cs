namespace Throughput.Sim;

public sealed class Scenario
{
    public required string Id { get; init; }
    public required int Version { get; init; }
    public required Ruleset Ruleset { get; init; }
    public required long ShiftStartMs { get; init; }
    public required long ShiftEndMs { get; init; }
    public required string SourceId { get; init; }
    public required GridPosition SourcePosition { get; init; }
    public required string SourceItemType { get; init; }
    public required string ProductId { get; init; }
    public required string DockId { get; init; }
    public required GridPosition DockPosition { get; init; }
    public required IReadOnlyList<ProcessDefinition> Processes { get; init; }
    public required IReadOnlyList<MachineDefinition> Machines { get; init; }
    public required IReadOnlyList<WorkerDefinition> Workers { get; init; }
    public IReadOnlyList<SimCommand> InitialCommands { get; init; } = [];

    public long ShiftDurationMs => ShiftEndMs - ShiftStartMs;
}
