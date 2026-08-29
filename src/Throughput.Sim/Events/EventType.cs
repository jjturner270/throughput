namespace Throughput.Sim;

public enum EventType
{
    ShiftStart,
    ShiftEnd,
    WorkerArrivePickup,
    WorkerPickupComplete,
    WorkerArriveDrop,
    WorkerDropComplete,
    MachineCycleComplete,
    Command
}
