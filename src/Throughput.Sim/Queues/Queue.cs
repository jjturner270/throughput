namespace Throughput.Sim;

public sealed class Queue
{
    public required string Id { get; init; }
    public required string FeedsMachineId { get; init; }
    public List<string> Items { get; } = [];
}
