using Throughput.Scenarios;
using Throughput.Sim;
using Xunit;

namespace Throughput.Sim.Tests;

public class ReplayTests
{
    [Fact]
    public void SerializedReplay_ReplaysToSameHash()
    {
        var scenario = ScenarioCatalog.Get("garage-01-mill-near");
        var original = new Simulation(scenario).Run();

        var record = new ReplayRecord
        {
            ScenarioId = scenario.Id,
            ScenarioVersion = scenario.Version,
            RulesetVersion = scenario.Ruleset.Version,
            Commands = original.Commands
        };

        var json = record.ToJson();
        var loaded = ReplayRecord.FromJson(json);
        var replayed = new Simulation(ScenarioCatalog.Get(loaded.ScenarioId), loaded.Commands).Run();

        Assert.Equal(original.FinalStateHash, replayed.FinalStateHash);
        Assert.Equal(original.Summary.Shipped, replayed.Summary.Shipped);
    }

    [Fact]
    public void ReplayFile_IsStableText()
    {
        var scenario = ScenarioCatalog.Get("garage-01-mill-near");
        var record = new ReplayRecord
        {
            ScenarioId = scenario.Id,
            ScenarioVersion = scenario.Version,
            RulesetVersion = scenario.Ruleset.Version,
            Commands = new List<SimCommand>
            {
                new(0, CommandType.MoveMachine, MachineId: "MILL_01", Position: new GridPosition(10, 6))
            }
        };

        var first = record.ToJson();
        var second = record.ToJson();
        Assert.Equal(first, second);
        Assert.Contains("garage-01", first);
        Assert.Contains("MOVE_MACHINE", first);
    }

    [Fact]
    public void TimedCommand_ChangesResultDeterministically()
    {
        var scenario = ScenarioCatalog.Get("garage-01");
        var baseline = new Simulation(scenario).Run();

        var midShift = new List<SimCommand>
        {
            new(7_200_000, CommandType.MoveMachine, MachineId: "MILL_01", Position: new GridPosition(10, 6))
        };
        var changed = new Simulation(scenario, midShift).Run();
        var changedAgain = new Simulation(scenario, midShift).Run();

        Assert.NotEqual(baseline.FinalStateHash, changed.FinalStateHash);
        Assert.Equal(changed.FinalStateHash, changedAgain.FinalStateHash);
    }
}
