using Throughput.Scenarios;
using Throughput.Sim;
using Xunit;

namespace Throughput.Sim.Tests;

public class IncrementalTests
{
    [Fact]
    public void IncrementalAdvance_MatchesOneShotRun()
    {
        var scenario = ScenarioCatalog.Get("garage-01");

        var oneShot = new Simulation(scenario).Run();

        var stepped = new Simulation(scenario);
        var rng = new Random(42);
        long duration = scenario.ShiftDurationMs;
        long t = 0;
        while (t < duration)
        {
            t += 1 + rng.Next(60_000);
            stepped.AdvanceTo(t);
        }

        var result = stepped.CompleteShift();

        Assert.Equal(oneShot.FinalStateHash, result.FinalStateHash);
        Assert.Equal(oneShot.Summary.Shipped, result.Summary.Shipped);
    }

    [Fact]
    public void EventBudgetAdvance_MatchesOneShotRun()
    {
        var scenario = ScenarioCatalog.Get("garage-01");

        var oneShot = new Simulation(scenario).Run();

        var stepped = new Simulation(scenario);
        for (int i = 0; i < 10_000; i++)
        {
            stepped.AdvanceEvents(17);
            if (stepped.CurrentTime >= scenario.ShiftDurationMs)
            {
                break;
            }
        }

        var result = stepped.CompleteShift();

        Assert.Equal(oneShot.FinalStateHash, result.FinalStateHash);
    }

    [Fact]
    public void AdvanceTo_DoesNotOvershootShift()
    {
        var scenario = ScenarioCatalog.Get("garage-01");
        var sim = new Simulation(scenario);

        sim.AdvanceTo(long.MaxValue);

        Assert.True(sim.CurrentTime <= scenario.ShiftDurationMs);
        var result = sim.CompleteShift();
        Assert.True(result.Summary.Shipped > 0);
    }
}
