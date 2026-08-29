namespace Throughput.Sim;

public sealed class Machine
{
    public required string Id { get; init; }
    public required string ProcessId { get; init; }
    public long CycleTimeMs { get; init; }
    public GridPosition Position { get; set; }
    public required string InputQueueId { get; init; }
    public required string FeedsQueueId { get; init; }
    public MachineState State { get; private set; } = MachineState.WaitingForInput;
    public long StateStartTime { get; private set; }
    public long[] TimeInState { get; } = new long[4];
    public string? CurrentItemId { get; set; }
    public string? OutputItemId { get; set; }

    public void SetState(MachineState state, long now)
    {
        if (State == state)
        {
            return;
        }

        TimeInState[(int)State] += now - StateStartTime;
        State = state;
        StateStartTime = now;
    }
}
