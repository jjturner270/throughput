namespace Throughput.Headless;

using System.Text;
using Throughput.Sim;

public static class Compare
{
    public static string Format(RunSummary baseline, RunSummary current)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("COMPARISON vs BASELINE");
        sb.AppendLine();
        Append(sb, "SHIPPED", baseline.Shipped, current.Shipped, "N0");
        Append(sb, "THROUGHPUT", baseline.ThroughputPerHour, current.ThroughputPerHour, "0.0", "/hr");
        Append(sb, "AVG LEAD TIME", baseline.AverageLeadTimeMs, current.AverageLeadTimeMs, "dur");
        Append(sb, "MEDIAN LEAD TIME", baseline.MedianLeadTimeMs, current.MedianLeadTimeMs, "dur");
        Append(sb, "AVG WIP", baseline.AverageWip, current.AverageWip, "0.0");
        Append(sb, "PEAK WIP", baseline.PeakWip, current.PeakWip, "N0");

        foreach (var (b, c) in baseline.Workers.Zip(current.Workers))
        {
            sb.AppendLine();
            sb.AppendLine(Report.DisplayName(c.Id));
            Append(sb, "Distance", b.DistanceKm, c.DistanceKm, "0.00", " km");
            Append(sb, "Tasks", b.TasksCompleted, c.TasksCompleted, "N0");
            Append(sb, "Moving", b.Pct(b.Moving), c.Pct(c.Moving), "0.0", "%");
            Append(sb, "Idle", b.Pct(b.Idle), c.Pct(c.Idle), "0.0", "%");
        }

        foreach (var (b, c) in baseline.Machines.Zip(current.Machines))
        {
            sb.AppendLine();
            sb.AppendLine(Report.DisplayName(c.Id));
            Append(sb, "Processing", b.Pct(b.Processing), c.Pct(c.Processing), "0.0", "%");
            Append(sb, "Waiting Input", b.Pct(b.WaitingForInput), c.Pct(c.WaitingForInput), "0.0", "%");
            Append(sb, "Waiting Worker", b.Pct(b.WaitingForWorker), c.Pct(c.WaitingForWorker), "0.0", "%");
        }

        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string label, double before, double after, string fmt, string unit = "")
    {
        double delta = after - before;
        double pct = before != 0 ? delta / before * 100.0 : 0.0;
        sb.AppendLine($"{label,-16} {FormatValue(before, fmt)}{unit,-6} -> {FormatValue(after, fmt)}{unit,-6} {delta:+0.0;-0.0;0.0} ({pct:+0.0;-0.0;0.0}%)");
    }

    private static void Append(StringBuilder sb, string label, long before, long after, string fmt, string unit = "")
    {
        Append(sb, label, (double)before, after, fmt, unit);
    }

    private static string FormatValue(double value, string fmt) =>
        fmt == "dur" ? SimTime.FormatDurationMs((long)value) : value.ToString(fmt);
}
