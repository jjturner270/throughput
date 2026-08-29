namespace Throughput.Sim;

public readonly record struct GridPosition(int X, int Y)
{
    public static readonly GridPosition Origin = new(0, 0);

    public int ManhattanDistance(GridPosition other) =>
        Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    public static long TravelTimeMs(GridPosition from, GridPosition to, int speedMmPerSec, int tileMm)
    {
        long distMm = (long)from.ManhattanDistance(to) * tileMm;
        return distMm * 1000 / speedMmPerSec;
    }
}
