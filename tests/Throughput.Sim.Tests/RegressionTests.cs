using Throughput.Scenarios;
using Throughput.Sim;
using Xunit;

namespace Throughput.Sim.Tests;

public class RegressionTests
{
    [Theory]
    [InlineData("garage-01-default", "e605883501c9936fe241e805753f01bc95f08d7e4d06321b390573bb41a89bb5")]
    [InlineData("garage-01-mill-near", "036fc67fa081b8327411aa38f46c979e25b42f415dd067f803f76940c8cf7a9c")]
    [InlineData("garage-01-worker-priority-a", "fa5cb752bdaa37f4d0575f6c337d28fe01a6e04b91ce23677e9b14437d6f33fe")]
    public void KnownConfiguration_KeepsKnownHash(string scenarioId, string expectedHash)
    {
        var hash = new Simulation(ScenarioCatalog.Get(scenarioId)).Run().FinalStateHash;
        Assert.Equal(expectedHash, hash);
    }
}
