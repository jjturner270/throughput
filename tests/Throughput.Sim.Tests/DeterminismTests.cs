using Throughput.Scenarios;
using Throughput.Sim;
using Xunit;

namespace Throughput.Sim.Tests;

public class DeterminismTests
{
    public static IEnumerable<object[]> ScenarioData()
    {
        foreach (var id in ScenarioCatalog.KnownScenarios)
        {
            yield return new object[] { id };
        }
    }

    [Theory]
    [MemberData(nameof(ScenarioData))]
    public void SameInputs_ProduceSameHash(string scenarioId)
    {
        var scenario = ScenarioCatalog.Get(scenarioId);
        var run1 = new Simulation(scenario).Run();
        var run2 = new Simulation(scenario).Run();
        var run3 = new Simulation(scenario).Run();

        Assert.Equal(run1.FinalStateHash, run2.FinalStateHash);
        Assert.Equal(run1.FinalStateHash, run3.FinalStateHash);
        Assert.Equal(run1.Summary.Shipped, run2.Summary.Shipped);
        Assert.Equal(run1.Summary.ThroughputPerHour, run2.Summary.ThroughputPerHour);
    }

    [Fact]
    public void RunTwiceInParallel_ProducesSameHash()
    {
        var scenario = ScenarioCatalog.Get("garage-01");
        string h1 = "", h2 = "";

        Parallel.Invoke(
            () => h1 = new Simulation(scenario).Run().FinalStateHash,
            () => h2 = new Simulation(scenario).Run().FinalStateHash);

        Assert.Equal(h1, h2);
    }

    [Fact]
    public void DistinctConfigurations_ProduceDistinctHashes()
    {
        string[] distinct =
        [
            "garage-01-default",
            "garage-01-mill-near",
            "garage-01-worker-priority-a"
        ];

        var hashes = distinct.Select(id => new Simulation(ScenarioCatalog.Get(id)).Run().FinalStateHash).ToList();

        for (int i = 0; i < hashes.Count; i++)
        {
            for (int j = i + 1; j < hashes.Count; j++)
            {
                Assert.NotEqual(hashes[i], hashes[j]);
            }
        }
    }
}
