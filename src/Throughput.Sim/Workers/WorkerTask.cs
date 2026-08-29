namespace Throughput.Sim;

public enum TaskKind
{
    LoadMachine,
    TransportItem
}

public sealed class WorkerTask
{
    public required long TaskId { get; init; }
    public required long CreatedAt { get; init; }
    public required TaskKind Kind { get; init; }
    public required string PickupEntityId { get; init; }
    public required GridPosition PickupPosition { get; init; }
    public string? DropQueueId { get; init; }
    public GridPosition DropPosition { get; init; }
    public string? LoadMachineId { get; init; }
    public string? AssignedWorkerId { get; set; }
    public string? CarriedItemId { get; set; }
}
