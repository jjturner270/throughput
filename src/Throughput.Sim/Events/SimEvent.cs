namespace Throughput.Sim;

public readonly record struct SimEvent(long Time, ulong Sequence, EventType Type, string Entity);
