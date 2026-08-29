namespace Throughput.Sim;

public sealed class RunResult
{
    public required Scenario Scenario { get; init; }
    public required IReadOnlyList<SimCommand> Commands { get; init; }
    public required RunSummary Summary { get; init; }
    public required string FinalStateHash { get; init; }
    public required long EventsProcessed { get; init; }
    public ItemProfileView? SampleItemProfile { get; init; }
}
