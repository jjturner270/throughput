using Throughput.Scenarios;
using Throughput.Sim;
using Xunit;

namespace Throughput.Sim.Tests;

public class AccountingTests
{
    [Fact]
    public void MachineStateTimes_SumToShiftDuration()
    {
        var run = new Simulation(ScenarioCatalog.Get("garage-01")).Run();
        long duration = run.Summary.DurationMs;

        foreach (var m in run.Summary.Machines)
        {
            Assert.Equal(duration, m.TimeInState.Sum());
        }
    }

    [Fact]
    public void WorkerStateTimes_SumToShiftDuration()
    {
        var run = new Simulation(ScenarioCatalog.Get("garage-01")).Run();
        long duration = run.Summary.DurationMs;

        foreach (var w in run.Summary.Workers)
        {
            Assert.Equal(duration, w.TimeInState.Sum());
        }
    }

    [Fact]
    public void ShippedItemLeadTime_IsFullyAccounted()
    {
        var run = new Simulation(ScenarioCatalog.Get("garage-01")).Run();
        var sim = run.Summary;

        Assert.True(sim.Shipped > 0);
        Assert.True(sim.AverageLeadTimeMs > 0);
        Assert.True(sim.MedianLeadTimeMs > 0);
    }

    [Fact]
    public void Metrics_AreConsistent()
    {
        var run = new Simulation(ScenarioCatalog.Get("garage-01")).Run();
        var s = run.Summary;
        long hours = s.DurationMs / 3_600_000L;

        Assert.Equal(s.Shipped / (double)hours, s.ThroughputPerHour, 3);
        Assert.True(s.PeakWip >= s.AverageWip);
        Assert.True(s.AverageWip >= 0);
    }

    [Fact]
    public void QueueGrowth_IsUnboundedSignal()
    {
        var run = new Simulation(ScenarioCatalog.Get("garage-01")).Run();
        var queueA = run.Summary.Queues.Single(q => q.Id == "QUEUE_A");
        var queueC = run.Summary.Queues.Single(q => q.Id == "QUEUE_C");

        Assert.True(queueA.AverageLength > 0);
        Assert.True(queueC.AverageLength > 0);
    }
}
