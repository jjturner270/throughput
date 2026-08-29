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
    public Dictionary<string, long> TimePerStation { get; } = [];
    public GridPosition LegFrom { get; set; }
    public GridPosition LegTo { get; set; }
    public long LegStartTime { get; set; }
    public long LegEndTime { get; set; }
    public bool HasLeg { get; set; }

    public GridPosition PositionAt(long time)
    {
        if (!HasLeg || LegEndTime <= LegStartTime)
        {
            return Position;
        }

        long t = Math.Clamp(time, LegStartTime, LegEndTime);
        double k = (double)(t - LegStartTime) / (LegEndTime - LegStartTime);
        double fx = LegFrom.X + (LegTo.X - LegFrom.X) * k;
        double fy = LegFrom.Y + (LegTo.Y - LegFrom.Y) * k;
        return new GridPosition((int)Math.Round(fx), (int)Math.Round(fy));
    }

    public void SetState(WorkerState state, long now)
    {
        if (State == state)
        {
            return;
        }

        long delta = now - StateStartTime;
        TimeInState[(int)State] += delta;
        if (CurrentTask is not null && State != WorkerState.Idle)
        {
            string station = CurrentTask.Kind == TaskKind.LoadMachine
                ? CurrentTask.LoadMachineId!
                : CurrentTask.PickupEntityId;
            TimePerStation[station] = TimePerStation.GetValueOrDefault(station) + delta;
        }

        State = state;
        StateStartTime = now;
        if (state == WorkerState.Idle)
        {
            IdleSince = now;
        }
    }
}
