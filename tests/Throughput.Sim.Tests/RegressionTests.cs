using Throughput.Scenarios;
using Throughput.Sim;
using Xunit;

namespace Throughput.Sim.Tests;

public class RegressionTests
{
    [Theory]
    [InlineData("garage-01-default", "30961a8fb26794f2f623e358ccc45088f6931268c3dc154b5a413bcc2f093493")]
    [InlineData("garage-01-mill-near", "04b104b0881c83132d9ae77d10370f17cf52aefdb29aeb659a7bf92bf3b96faf")]
    [InlineData("garage-01-worker-priority-a", "e5d8c2e2afd9ac55fd7c9ce7525d7f31cbe7e04e9f62628e9088ce8fdcf0d8b0")]
    public void KnownConfiguration_KeepsKnownHash(string scenarioId, string expectedHash)
    {
        var hash = new Simulation(ScenarioCatalog.Get(scenarioId)).Run().FinalStateHash;
        Assert.Equal(expectedHash, hash);
    }
}
