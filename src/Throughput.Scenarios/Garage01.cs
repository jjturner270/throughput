namespace Throughput.Scenarios;

using Throughput.Sim;

public static class Garage01
{
    public const long ShiftStartMs = 8 * 3600 * 1000;
    public const long ShiftEndMs = 16 * 3600 * 1000;
    public const int TileMm = 1000;

    public static readonly Ruleset Ruleset = new(
        Version: "1.0",
        TileMm: TileMm,
        LoadTimeMs: 2000,
        UnloadTimeMs: 2000,
        DefaultWorkerSpeedMmPerSec: 1000);

    public static readonly ProcessDefinition[] Processes =
    [
        new("SAW", [new ProcessInput("RAW_STOCK")], [new ProcessOutput("CUT_BLANK")], 40000),
        new("MILL", [new ProcessInput("CUT_BLANK")], [new ProcessOutput("MACHINED_PART")], 60000),
        new("QC", [new ProcessInput("MACHINED_PART")], [new ProcessOutput("INSPECTED_PART")], 35000),
        new("ASSEMBLY", [new ProcessInput("INSPECTED_PART")], [new ProcessOutput("GEARBOX")], 100000)
    ];

    public static readonly MachineDefinition[] Machines =
    [
        new("SAW_01", "SAW", 40000, new GridPosition(6, 6), "QUEUE_RAW", "QUEUE_A"),
        new("MILL_01", "MILL", 60000, new GridPosition(14, 6), "QUEUE_A", "QUEUE_B"),
        new("QC_01", "QC", 35000, new GridPosition(18, 6), "QUEUE_B", "QUEUE_C"),
        new("ASSEMBLY_01", "ASSEMBLY", 100000, new GridPosition(22, 6), "QUEUE_C", "DOCK")
    ];

    public static readonly string[] UpstreamPriority =
        ["RAW_SOURCE", "SAW_01", "MILL_01", "QC_01", "ASSEMBLY_01", "DOCK"];

    public static readonly string[] DownstreamPriority =
        ["DOCK", "ASSEMBLY_01", "QC_01", "MILL_01", "SAW_01", "RAW_SOURCE"];

    public static readonly WorkerDefinition[] Workers =
    [
        new("WORKER_01", 1000, new GridPosition(0, 0), UpstreamPriority),
        new("WORKER_02", 1000, new GridPosition(0, 12), UpstreamPriority)
    ];

    public static Scenario Build(IReadOnlyList<SimCommand>? initialCommands = null)
    {
        return new Scenario
        {
            Id = "garage-01",
            Version = 1,
            Ruleset = Ruleset,
            ShiftStartMs = ShiftStartMs,
            ShiftEndMs = ShiftEndMs,
            SourceId = "RAW_SOURCE",
            SourcePosition = new GridPosition(2, 6),
            SourceItemType = "RAW_STOCK",
            ProductId = "GEARBOX",
            DockId = "DOCK",
            DockPosition = new GridPosition(26, 6),
            Processes = Processes,
            Machines = Machines,
            Workers = Workers,
            InitialCommands = initialCommands ?? []
        };
    }
}
