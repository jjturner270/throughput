namespace Throughput.Sim;

public sealed record Ruleset(
    string Version,
    int TileMm = 1000,
    long LoadTimeMs = 2000,
    long UnloadTimeMs = 2000,
    int DefaultWorkerSpeedMmPerSec = 1500);
