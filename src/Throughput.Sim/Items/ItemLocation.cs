namespace Throughput.Sim;

public readonly record struct ItemLocation(string Kind, string Id)
{
    public static ItemLocation Source(string id) => new("SOURCE", id);
    public static ItemLocation Queue(string id) => new("QUEUE", id);
    public static ItemLocation Machine(string id) => new("MACHINE", id);
    public static ItemLocation Worker(string id) => new("WORKER", id);
    public static ItemLocation Dock(string id) => new("DOCK", id);
}
