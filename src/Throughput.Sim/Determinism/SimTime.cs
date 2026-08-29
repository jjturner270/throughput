namespace Throughput.Sim;

public readonly record struct SimTime(long Milliseconds)
{
    public static readonly SimTime Zero = new(0);

    public SimTime Add(long ms) => new(Milliseconds + ms);

    public static string FormatClock(long ms)
    {
        long totalSeconds = ms / 1000;
        long h = totalSeconds / 3600;
        long m = (totalSeconds % 3600) / 60;
        long s = totalSeconds % 60;
        return $"{h:00}:{m:00}:{s:00}";
    }

    public static string FormatClockMs(long ms)
    {
        long totalSeconds = ms / 1000;
        long h = totalSeconds / 3600;
        long m = (totalSeconds % 3600) / 60;
        long s = totalSeconds % 60;
        return $"{h:00}:{m:00}:{s:00}.{ms % 1000:000}";
    }

    public static string FormatDuration(long ms)
    {
        long totalSeconds = ms / 1000;
        long h = totalSeconds / 3600;
        long m = (totalSeconds % 3600) / 60;
        long s = totalSeconds % 60;
        if (h > 0) return $"{h:00}:{m:00}:{s:00}";
        if (m > 0) return $"{m:00}:{s:00}";
        return $"00:{s:00}";
    }

    public static string FormatDurationMs(long ms)
    {
        long totalSeconds = ms / 1000;
        long h = totalSeconds / 3600;
        long m = (totalSeconds % 3600) / 60;
        long s = totalSeconds % 60;
        if (h > 0) return $"{h:00}:{m:00}:{s:00}.{ms % 1000:000}";
        if (m > 0) return $"{m:00}:{s:00}.{ms % 1000:000}";
        return $"00:{s:00}.{ms % 1000:000}";
    }
}
