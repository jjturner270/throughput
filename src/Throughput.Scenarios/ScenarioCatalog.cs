namespace Throughput.Scenarios;

using Throughput.Sim;

public static class ScenarioCatalog
{
    public static readonly string[] KnownScenarios =
    [
        "garage-01",
        "garage-01-default",
        "garage-01-mill-near",
        "garage-01-worker-priority-a",
        "garage-01-worker-priority-b"
    ];

    public static Scenario Get(string id)
    {
        return id switch
        {
            "garage-01" or "garage-01-default" => Garage01.Build(),
            "garage-01-mill-near" => Garage01.Build(
            [
                new SimCommand(0, CommandType.MoveMachine, MachineId: "MILL_01", Position: new GridPosition(10, 6))
            ]),
            "garage-01-worker-priority-a" => Garage01.Build(
            [
                new SimCommand(0, CommandType.SetWorkerPriority, WorkerId: "WORKER_01", Priorities: Garage01.DownstreamPriority),
                new SimCommand(0, CommandType.SetWorkerPriority, WorkerId: "WORKER_02", Priorities: Garage01.DownstreamPriority)
            ]),
            "garage-01-worker-priority-b" => Garage01.Build(
            [
                new SimCommand(0, CommandType.SetWorkerPriority, WorkerId: "WORKER_01", Priorities: Garage01.UpstreamPriority),
                new SimCommand(0, CommandType.SetWorkerPriority, WorkerId: "WORKER_02", Priorities: Garage01.UpstreamPriority)
            ]),
            _ => throw new ArgumentException($"Unknown scenario '{id}'. Known: {string.Join(", ", KnownScenarios)}")
        };
    }
}
