namespace Throughput.Sim;

public sealed record MachineStats(
    string Id,
    long[] TimeInState,
    long TotalMs)
{
    public long WaitingForInput => TimeInState[(int)MachineState.WaitingForInput];
    public long WaitingForJob => TimeInState[(int)MachineState.WaitingForJob];
    public long WaitingForWorker => TimeInState[(int)MachineState.WaitingForWorker];
    public long Processing => TimeInState[(int)MachineState.Processing];

    public double Pct(long ms) => TotalMs > 0 ? ms * 100.0 / TotalMs : 0.0;
}

public sealed record WorkerStats(
    string Id,
    long[] TimeInState,
    long TotalMs,
    long DistanceMm,
    long TasksCompleted,
    IReadOnlyDictionary<string, long> TimePerStation)
{
    public long Idle => TimeInState[(int)WorkerState.Idle];
    public long Moving => TimeInState[(int)WorkerState.Moving];
    public long Loading => TimeInState[(int)WorkerState.Loading];
    public long Unloading => TimeInState[(int)WorkerState.Unloading];

    public double Pct(long ms) => TotalMs > 0 ? ms * 100.0 / TotalMs : 0.0;
    public double DistanceKm => DistanceMm / 1_000_000.0;
}

public sealed record QueueStats(string Id, double AverageLength, long CurrentLength);

public sealed record RunSummary(
    string ScenarioId,
    int ScenarioVersion,
    string RulesetVersion,
    long DurationMs,
    long Shipped,
    double ThroughputPerHour,
    long AverageLeadTimeMs,
    long MedianLeadTimeMs,
    double AverageWip,
    long PeakWip,
    IReadOnlyList<MachineStats> Machines,
    IReadOnlyList<WorkerStats> Workers,
    IReadOnlyList<QueueStats> Queues);
