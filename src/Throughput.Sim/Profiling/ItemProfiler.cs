namespace Throughput.Sim;

public sealed record ItemProfilerEntry(string Label, string Activity, string LocationId, long DurationMs);

public sealed record ItemProfileView(
    string ItemId,
    long CreatedAt,
    long ShippedAt,
    long LeadTimeMs,
    IReadOnlyList<ItemProfilerEntry> Timeline,
    ItemProfilerEntry? BiggestDelay);

public static class ItemProfiler
{
    public static List<ItemProfilerEntry> Timeline(Item item)
    {
        var entries = new List<ItemProfilerEntry>();
        for (int i = 0; i < item.Trace.Count - 1; i++)
        {
            var a = item.Trace[i];
            var b = item.Trace[i + 1];
            if (b.Time <= a.Time)
            {
                continue;
            }

            Add(entries, a, b.Time - a.Time);
        }

        var last = item.Trace[^1];
        long end = item.ShippedAt >= 0 ? item.ShippedAt : item.ActivityStartTime;
        if (end > last.Time)
        {
            Add(entries, last, end - last.Time);
        }

        return entries;
    }

    public static ItemProfilerEntry? BiggestDelay(List<ItemProfilerEntry> timeline) =>
        timeline.Count > 0 ? timeline.OrderByDescending(e => e.DurationMs).First() : null;

    public static long TotalLeadTimeMs(Item item) =>
        item.ShippedAt >= 0 ? item.ShippedAt - item.CreatedAt : item.ActivityStartTime - item.CreatedAt;

    private static void Add(List<ItemProfilerEntry> entries, ItemTraceEntry entry, long durationMs)
    {
        if (durationMs <= 0)
        {
            return;
        }

        string label = FriendlyName(entry.Location);

        if (entries.Count > 0 && entries[^1].LocationId == entry.Location)
        {
            var prev = entries[^1];
            entries[^1] = prev with { DurationMs = prev.DurationMs + durationMs };
            return;
        }

        entries.Add(new ItemProfilerEntry(label, entry.Activity, entry.Location, durationMs));
    }

    private static string FriendlyName(string location)
    {
        if (location.StartsWith("WORKER", StringComparison.Ordinal))
        {
            return "TRANSPORT";
        }

        if (location == "RAW_SOURCE")
        {
            return "RAW SOURCE";
        }

        var parts = location.Split('_');
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(' ');
            }

            sb.Append(char.ToUpperInvariant(parts[i][0]));
            sb.Append(parts[i][1..].ToLowerInvariant());
        }

        return sb.ToString();
    }
}
