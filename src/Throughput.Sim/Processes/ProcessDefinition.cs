namespace Throughput.Sim;

public sealed record ProcessInput(string ItemType, int Quantity = 1);

public sealed record ProcessOutput(string ItemType, int Quantity = 1);

public sealed record ProcessDefinition(
    string Id,
    IReadOnlyList<ProcessInput> Inputs,
    IReadOnlyList<ProcessOutput> Outputs,
    long DurationMs);
