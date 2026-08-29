namespace Throughput.Headless;

using Throughput.Scenarios;
using Throughput.Sim;

public static class Report
{
    public static string Format(RunResult result)
    {
        var s = result.Summary;
        long start = result.Scenario.ShiftStartMs;
        long end = result.Scenario.ShiftEndMs;
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("THROUGHPUT");
        sb.AppendLine();
        sb.AppendLine("SCENARIO");
        sb.AppendLine(s.ScenarioId);
        sb.AppendLine();
        sb.AppendLine("SHIFT");
        sb.AppendLine($"{SimTime.FormatClock(start)} — {SimTime.FormatClock(end)}");
        sb.AppendLine();
        sb.AppendLine("EVENTS");
        sb.AppendLine(result.EventsProcessed.ToString("N0"));
        sb.AppendLine();
        sb.AppendLine("PRODUCTION");
        sb.AppendLine();
        sb.AppendLine("Shipped             " + s.Shipped.ToString("N0"));
        sb.AppendLine();
        sb.AppendLine("FLOW");
        sb.AppendLine();
        sb.AppendLine("Throughput          " + s.ThroughputPerHour.ToString("0.0") + "/hr");
        sb.AppendLine("Average Lead Time   " + SimTime.FormatDurationMs(s.AverageLeadTimeMs));
        sb.AppendLine("Median Lead Time    " + SimTime.FormatDurationMs(s.MedianLeadTimeMs));
        sb.AppendLine("Average WIP         " + s.AverageWip.ToString("0.0"));
        sb.AppendLine("Peak WIP            " + s.PeakWip.ToString("N0"));
        sb.AppendLine();
        sb.AppendLine("QUEUES");
        sb.AppendLine();
        foreach (var q in s.Queues)
        {
            sb.AppendLine($"{DisplayName(q.Id),-16} avg {q.AverageLength.ToString("0.0"),6}  now {q.CurrentLength.ToString("N0"),6}");
        }

        sb.AppendLine();
        sb.AppendLine("WORKERS");
        sb.AppendLine();
        foreach (var w in s.Workers)
        {
            sb.AppendLine(DisplayName(w.Id));
            sb.AppendLine($"Moving              {w.Pct(w.Moving).ToString("0.0")}%");
            sb.AppendLine($"Loading             {w.Pct(w.Loading).ToString("0.0")}%");
            sb.AppendLine($"Unloading           {w.Pct(w.Unloading).ToString("0.0")}%");
            sb.AppendLine($"Idle                {w.Pct(w.Idle).ToString("0.0")}%");
            sb.AppendLine($"Distance            {w.DistanceKm.ToString("0.00")} km");
            sb.AppendLine($"Tasks               {w.TasksCompleted.ToString("N0")}");
            sb.AppendLine();
        }

        sb.AppendLine("MACHINES");
        sb.AppendLine();
        foreach (var m in s.Machines)
        {
            sb.AppendLine(DisplayName(m.Id));
            sb.AppendLine($"Processing          {m.Pct(m.Processing).ToString("0.0")}%");
            sb.AppendLine($"Waiting Job         {m.Pct(m.WaitingForJob).ToString("0.0")}%");
            sb.AppendLine($"Waiting Input       {m.Pct(m.WaitingForInput).ToString("0.0")}%");
            sb.AppendLine($"Waiting Worker      {m.Pct(m.WaitingForWorker).ToString("0.0")}%");
            sb.AppendLine();
        }

        sb.AppendLine("FINAL STATE HASH");
        sb.AppendLine();
        sb.AppendLine(result.FinalStateHash);
        return sb.ToString();
    }

    public static string DisplayName(string id)
    {
        var parts = id.Split('_');
        if (parts.Length == 2)
        {
            var name = char.ToUpperInvariant(parts[0][0]) + parts[0][1..].ToLowerInvariant();
            return $"{name} {parts[1]}";
        }

        return id;
    }
}
