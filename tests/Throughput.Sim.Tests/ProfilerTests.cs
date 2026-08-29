using Throughput.Scenarios;
using Throughput.Sim;
using Xunit;

namespace Throughput.Sim.Tests;

public class ProfilerTests
{
    [Fact]
    public void ShippedItemLeadTime_IsFullyAccountedByTimeline()
    {
        var run = new Simulation(ScenarioCatalog.Get("garage-01")).Run();
        var profile = run.SampleItemProfile;

        Assert.NotNull(profile);
        Assert.True(profile.LeadTimeMs > 0);
        long accounted = profile.Timeline.Sum(e => e.DurationMs);
        Assert.Equal(profile.LeadTimeMs, accounted);
    }

    [Fact]
    public void ProductProfile_HasBiggestDelay()
    {
        var run = new Simulation(ScenarioCatalog.Get("garage-01")).Run();
        Assert.NotNull(run.SampleItemProfile);
        Assert.NotNull(run.SampleItemProfile.BiggestDelay);
        Assert.Equal(run.SampleItemProfile.Timeline.Max(e => e.DurationMs), run.SampleItemProfile.BiggestDelay.DurationMs);
    }

    [Fact]
    public void WorkerStationTime_SumsToNonIdleTime()
    {
        var run = new Simulation(ScenarioCatalog.Get("garage-01")).Run();

        foreach (var w in run.Summary.Workers)
        {
            long stationTotal = w.TimePerStation.Values.Sum();
            long nonIdle = w.Moving + w.Loading + w.Unloading;
            Assert.Equal(nonIdle, stationTotal);
        }
    }
}
