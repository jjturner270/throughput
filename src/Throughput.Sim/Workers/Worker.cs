namespace Throughput.Sim;

public sealed class Worker
{
    public required string Id { get; init; }
    public int SpeedMmPerSec { get; init; }
    public GridPosition Position { get; set; }
    public WorkerState State { get; private set; } = WorkerState.Idle;
    public long StateStartTime { get; private set; }
    public long[] TimeInState { get; } = new long[4];
    public long TotalDistanceMm { get; set; }
    public long TasksCompleted { get; set; }
    public long IdleSince { get; private set; }
    public List<string> Priority { get; set; } = [];
    public WorkerTask? CurrentTask { get; set; }

    public void SetState(WorkerState state, long now)
    {
        if (State == state)
        {
            return;
        }

        TimeInState[(int)State] += now - StateStartTime;
        State = state;
        StateStartTime = now;
        if (state == WorkerState.Idle)
        {
            IdleSince = now;
        }
    }
}
