using Godot;
using Throughput.Sim;

namespace Throughput.Game;

public partial class Hud : Control
{
    public Simulation? Sim { get; set; }
    public Scenario? Scenario { get; set; }
    public long DisplayTimeMs { get; set; }
    public int SpeedIndex { get; set; } = 1;
    public RunResult? FinalResult { get; set; }
    public (string Kind, string Id)? Selection { get; set; }

    private static readonly string[] SpeedLabels = ["1x", "4x", "16x", "MAX"];

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        Size = GetViewport().GetVisibleRect().Size;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Sim is null || Scenario is null || Sim.Queues.Count == 0)
        {
            return;
        }

        var font = ThemeDB.FallbackFont;
        var size = Size;

        DrawTopBar(font, size);
        DrawInspector(font, size);
        DrawBottomLeft(font, size);
        DrawBottomRight(font, size);

        if (FinalResult is { } result)
        {
            DrawSummary(font, size, result);
        }
    }

    private void DrawTopBar(Font font, Vector2 size)
    {
        DrawRect(new Rect2(0, 0, size.X, Layout.TopBarH), Palette.PanelBg, true);
        DrawLine(new Vector2(0, Layout.TopBarH), new Vector2(size.X, Layout.TopBarH), Palette.PanelBorder, 1);

        DrawString(font, new Vector2(16, 25), "THROUGHPUT", HorizontalAlignment.Left, -1, 18, Palette.Accent);
        float titleW = font.GetStringSize("THROUGHPUT", HorizontalAlignment.Left, -1, 18).X;
        DrawString(font, new Vector2(20 + titleW, 25), "/ " + Scenario!.Id, HorizontalAlignment.Left, -1, 13, Palette.DimText);

        long absolute = Scenario!.ShiftStartMs + DisplayTimeMs;
        string clock = $"{absolute / 3600_000:00}:{(absolute / 60_000) % 60:00}:{(absolute / 1000) % 60:00}";
        DrawString(font, new Vector2(240, 27), clock, HorizontalAlignment.Left, -1, 22, Palette.Text);

        var stats = $"SHIPPED {Sim!.ShippedCount}   WIP {Sim.Items.Values.Count(i => i.ShippedAt < 0)}";
        float statsW = font.GetStringSize(stats, HorizontalAlignment.Left, -1, 14).X;
        var pause = Layout.PauseRect(size);
        float statsX = pause.Position.X - 12 - statsW;
        DrawString(font, new Vector2(statsX, 25), stats, HorizontalAlignment.Left, -1, 14, Palette.Text);

        if (SpeedIndex == 0)
        {
            DrawRect(pause, Palette.Accent, false, 2);
            DrawCenteredIn(font, pause, "PAUSED", 14, Palette.Accent);
        }

        var buttons = Layout.SpeedButtonRects(size);
        for (int i = 0; i < buttons.Length; i++)
        {
            bool active = SpeedIndex == i + 1;
            DrawRect(buttons[i], active ? new Color(Palette.Accent, 0.18f) : new Color(Palette.PanelBorder), true);
            DrawRect(buttons[i], active ? Palette.Accent : Palette.PanelBorder, false, 1);
            DrawCenteredIn(font, buttons[i], SpeedLabels[i], 13, active ? Palette.Accent : Palette.DimText);
        }
    }

    private void DrawInspector(Font font, Vector2 size)
    {
        var rect = Layout.InspectorRect(size);
        DrawRect(rect, Palette.PanelBg, true);
        DrawLine(new Vector2(rect.Position.X, rect.Position.Y), new Vector2(rect.Position.X, rect.End.Y), Palette.PanelBorder, 1);
        DrawString(font, new Vector2(rect.Position.X + 14, rect.Position.Y + 22), "INSPECTOR", HorizontalAlignment.Left, -1, 13, Palette.DimText);
        DrawLine(new Vector2(rect.Position.X, rect.Position.Y + 30), new Vector2(rect.End.X, rect.Position.Y + 30), Palette.PanelBorder, 1);

        if (Selection is { } sel)
        {
            if (sel.Kind == "MACHINE" && Sim!.Machines.TryGetValue(sel.Id, out var m))
            {
                DrawMachineDetails(font, rect, m);
            }
            else if (sel.Kind == "WORKER" && Sim!.Workers.TryGetValue(sel.Id, out var worker))
            {
                DrawWorkerDetails(font, rect, worker);
            }
            else if (sel.Kind == "ITEM" && Sim!.Items.TryGetValue(sel.Id, out var item))
            {
                DrawItemDetails(font, rect, item);
            }
            else
            {
                DrawDefaultInspector(font, rect);
            }
        }
        else
        {
            DrawDefaultInspector(font, rect);
        }
    }

    private void DrawItemDetails(Font font, Rect2 rect, Item item)
    {
        float x = rect.Position.X + 14;
        float y = rect.Position.Y + 30;
        var profile = Sim!.ProfileItem(item.Id);

        DrawString(font, new Vector2(x, y + 30), item.Id, HorizontalAlignment.Left, -1, 18, Palette.Text);
        DrawString(font, new Vector2(x, y + 52), $"PRODUCT   {item.ProductId}  ({item.Type})", HorizontalAlignment.Left, -1, 12, Palette.DimText);
        DrawString(font, new Vector2(x, y + 74), $"NOW       {ItemActivityLabel(item.Activity)}", HorizontalAlignment.Left, -1, 12, Palette.Text);

        if (profile.ShippedAt >= 0)
        {
            DrawString(font, new Vector2(x, y + 98), $"CREATED   {SimTime.FormatClockMs(profile.CreatedAt)}", HorizontalAlignment.Left, -1, 12, Palette.DimText);
            DrawString(font, new Vector2(x, y + 118), $"SHIPPED   {SimTime.FormatClockMs(profile.ShippedAt)}", HorizontalAlignment.Left, -1, 12, Palette.DimText);
            DrawString(font, new Vector2(x, y + 138), $"LEAD      {SimTime.FormatDurationMs(profile.LeadTimeMs)}", HorizontalAlignment.Left, -1, 13, Palette.Accent);
            y += 162;
        }
        else
        {
            DrawString(font, new Vector2(x, y + 98), $"CREATED   {SimTime.FormatClockMs(profile.CreatedAt)}", HorizontalAlignment.Left, -1, 12, Palette.DimText);
            DrawString(font, new Vector2(x, y + 118), $"IN SYSTEM {SimTime.FormatDuration(DisplayTimeMs - item.CreatedAt)}", HorizontalAlignment.Left, -1, 12, Palette.DimText);
            y += 142;
        }

        DrawLine(new Vector2(x, y), new Vector2(rect.End.X - 14, y), Palette.PanelBorder, 1);
        y += 22;

        foreach (var e in profile.Timeline)
        {
            DrawString(font, new Vector2(x, y), $"{e.Label,-12} {SimTime.FormatDuration(e.DurationMs),8}", HorizontalAlignment.Left, -1, 12, Palette.Text);
            y += 19;
        }

        if (profile.BiggestDelay is { } delay)
        {
            y += 4;
            DrawLine(new Vector2(x, y), new Vector2(rect.End.X - 14, y), Palette.PanelBorder, 1);
            y += 22;
            DrawString(font, new Vector2(x, y), "BIGGEST DELAY", HorizontalAlignment.Left, -1, 12, Palette.DimText);
            y += 20;
            DrawString(font, new Vector2(x, y), $"{delay.Label,-12} {SimTime.FormatDuration(delay.DurationMs),8}", HorizontalAlignment.Left, -1, 13, Palette.QueueBar);
        }
    }

    private static string ItemActivityLabel(ItemActivity activity) => activity switch
    {
        ItemActivity.Queueing => "IN QUEUE",
        ItemActivity.Transporting => "TRANSPORTING",
        ItemActivity.Processing => "PROCESSING",
        ItemActivity.Waiting => "WAITING",
        ItemActivity.Shipped => "SHIPPED",
        _ => activity.ToString()
    };

    private void DrawMachineDetails(Font font, Rect2 rect, Machine m)
    {
        float x = rect.Position.X + 14;
        float y = rect.Position.Y + 30;
        long elapsed = DisplayTimeMs;
        var live = (long[])m.TimeInState.Clone();
        live[(int)m.State] += DisplayTimeMs - m.StateStartTime;

        DrawString(font, new Vector2(x, y + 30), FactoryView.ReportName(m.Id), HorizontalAlignment.Left, -1, 19, Palette.Text);
        DrawString(font, new Vector2(x, y + 56), $"STATE   {StateLabel(m.State)}", HorizontalAlignment.Left, -1, 14, MachineColor(m.State));
        DrawString(font, new Vector2(x, y + 80), $"JOB     {m.CurrentItemId ?? "-"}", HorizontalAlignment.Left, -1, 12, Palette.DimText);
        DrawString(font, new Vector2(x, y + 100), $"OUTPUT  {m.OutputItemId ?? "-"}", HorizontalAlignment.Left, -1, 12, Palette.DimText);

        DrawString(font, new Vector2(x, y + 132), "PROCESSING      " + Pct(live[(int)MachineState.Processing], elapsed), HorizontalAlignment.Left, -1, 13, Palette.Processing);
        DrawString(font, new Vector2(x, y + 156), "WAITING JOB     " + Pct(live[(int)MachineState.WaitingForJob], elapsed), HorizontalAlignment.Left, -1, 13, Palette.WaitingForJob);
        DrawString(font, new Vector2(x, y + 180), "WAITING INPUT   " + Pct(live[(int)MachineState.WaitingForInput], elapsed), HorizontalAlignment.Left, -1, 13, Palette.WaitingForInput);
        DrawString(font, new Vector2(x, y + 204), "WAITING WORKER  " + Pct(live[(int)MachineState.WaitingForWorker], elapsed), HorizontalAlignment.Left, -1, 13, Palette.WaitingForWorker);

        DrawLine(new Vector2(x, y + 228), new Vector2(rect.End.X - 14, y + 228), Palette.PanelBorder, 1);
        DrawString(font, new Vector2(x, y + 250), "DETAIL", HorizontalAlignment.Left, -1, 12, Palette.DimText);
        DrawString(font, new Vector2(x, y + 274), $"CYCLE   {SimTime.FormatDuration(m.CycleTimeMs)}", HorizontalAlignment.Left, -1, 12, Palette.Text);
        DrawString(font, new Vector2(x, y + 296), $"FEEDS   {FactoryView.ReportName(m.FeedsQueueId)}", HorizontalAlignment.Left, -1, 12, Palette.Text);
    }

    private void DrawWorkerDetails(Font font, Rect2 rect, Worker w)
    {
        float x = rect.Position.X + 14;
        float y = rect.Position.Y + 30;
        long elapsed = DisplayTimeMs;
        var live = (long[])w.TimeInState.Clone();
        live[(int)w.State] += DisplayTimeMs - w.StateStartTime;

        DrawString(font, new Vector2(x, y + 30), FactoryView.ReportName(w.Id), HorizontalAlignment.Left, -1, 19, Palette.Text);
        DrawString(font, new Vector2(x, y + 56), $"STATE    {WorkerStateLabel(w.State)}", HorizontalAlignment.Left, -1, 14, w.State == WorkerState.Idle ? Palette.WorkerIdle : Palette.WorkerBusy);
        DrawString(font, new Vector2(x, y + 80), $"DISTANCE {w.TotalDistanceMm / 1_000_000.0:0.00} km", HorizontalAlignment.Left, -1, 13, Palette.Text);
        DrawString(font, new Vector2(x, y + 102), $"TASKS    {w.TasksCompleted}", HorizontalAlignment.Left, -1, 13, Palette.Text);

        DrawString(font, new Vector2(x, y + 134), "MOVING      " + Pct(live[(int)WorkerState.Moving], elapsed), HorizontalAlignment.Left, -1, 13, Palette.WorkerBusy);
        DrawString(font, new Vector2(x, y + 158), "LOADING     " + Pct(live[(int)WorkerState.Loading], elapsed), HorizontalAlignment.Left, -1, 13, Palette.Text);
        DrawString(font, new Vector2(x, y + 182), "UNLOADING   " + Pct(live[(int)WorkerState.Unloading], elapsed), HorizontalAlignment.Left, -1, 13, Palette.Text);
        DrawString(font, new Vector2(x, y + 206), "IDLE        " + Pct(live[(int)WorkerState.Idle], elapsed), HorizontalAlignment.Left, -1, 13, Palette.DimText);

        DrawLine(new Vector2(x, y + 228), new Vector2(rect.End.X - 14, y + 228), Palette.PanelBorder, 1);
        DrawString(font, new Vector2(x, y + 250), "DETAIL", HorizontalAlignment.Left, -1, 12, Palette.DimText);
        DrawString(font, new Vector2(x, y + 274), $"AT  {((w.CurrentTask is { } t) ? FactoryView.ReportName(t.PickupEntityId) : "-")}", HorizontalAlignment.Left, -1, 12, Palette.Text);
        int row = 0;
        foreach (var kv in w.TimePerStation.OrderByDescending(kv => kv.Value).Take(4))
        {
            DrawString(font, new Vector2(x, y + 296 + row * 20), $"  {FactoryView.ReportName(kv.Key),-10} {SimTime.FormatDuration(kv.Value),8}", HorizontalAlignment.Left, -1, 12, Palette.DimText);
            row++;
        }
    }

    private void DrawDefaultInspector(Font font, Rect2 rect)
    {
        float x = rect.Position.X + 14;
        float y = rect.Position.Y + 30;
        DrawString(font, new Vector2(x, y + 30), "Click a machine, worker", HorizontalAlignment.Left, -1, 14, Palette.Text);
        DrawString(font, new Vector2(x, y + 52), "or an item to inspect.", HorizontalAlignment.Left, -1, 13, Palette.DimText);

        DrawLine(new Vector2(x, y + 76), new Vector2(rect.End.X - 14, y + 76), Palette.PanelBorder, 1);
        DrawString(font, new Vector2(x, y + 98), "QUEUES", HorizontalAlignment.Left, -1, 12, Palette.DimText);
        int row = 0;
        foreach (var q in Sim!.Queues.Values
                     .Where(q => q.Id != Scenario!.DockId && q.Id != "SOURCE_BUFFER")
                     .OrderBy(q => q.Id, StringComparer.Ordinal))
        {
            DrawString(font, new Vector2(x, y + 122 + row * 22), $"{FactoryView.ReportName(q.Id),-10} {q.Items.Count,6}", HorizontalAlignment.Left, -1, 13, Palette.Text);
            row++;
        }

        DrawLine(new Vector2(x, y + 122 + row * 22 + 8), new Vector2(rect.End.X - 14, y + 122 + row * 22 + 8), Palette.PanelBorder, 1);
        DrawString(font, new Vector2(x, y + 122 + row * 22 + 32), "F1..F4 / wheel", HorizontalAlignment.Left, -1, 12, Palette.DimText);
        DrawString(font, new Vector2(x, y + 122 + row * 22 + 54), "zoom  ·  arrows pan", HorizontalAlignment.Left, -1, 12, Palette.DimText);
        DrawString(font, new Vector2(x, y + 122 + row * 22 + 76), "F  re-fit  ·  R  restart", HorizontalAlignment.Left, -1, 12, Palette.DimText);
    }

    private void DrawBottomLeft(Font font, Vector2 size)
    {
        var rect = Layout.BottomLeftRect(size);
        DrawRect(rect, Palette.PanelBg, true);
        DrawLine(new Vector2(rect.Position.X, rect.Position.Y), new Vector2(rect.End.X, rect.Position.Y), Palette.PanelBorder, 1);
        DrawString(font, new Vector2(rect.Position.X + 14, rect.Position.Y + 22), "MACHINE TIMELINE", HorizontalAlignment.Left, -1, 13, Palette.DimText);

        long elapsed = DisplayTimeMs;
        var machines = Sim!.Machines.Values.OrderBy(m => m.Id, StringComparer.Ordinal).ToList();
        float barX = rect.Position.X + 14;
        float barW = rect.Size.X - 28 - 74;
        float rowY = rect.Position.Y + 40;
        float rowH = (rect.Size.Y - 48) / machines.Count;

        foreach (var m in machines)
        {
            var live = (long[])m.TimeInState.Clone();
            live[(int)m.State] += DisplayTimeMs - m.StateStartTime;

            DrawString(font, new Vector2(barX, rowY + rowH * 0.68f), FactoryView.ReportName(m.Id), HorizontalAlignment.Left, -1, 12, Palette.Text);

            float x = barX + 68;
            float w = barW;
            DrawRect(new Rect2(x, rowY + 3, w, 10), Palette.PanelBorder, true);
            float acc = 0;
            void Segment(Color color, long ms)
            {
                if (ms <= 0 || elapsed <= 0)
                {
                    return;
                }

                float f = (float)(ms / (double)elapsed);
                DrawRect(new Rect2(x + acc * w, rowY + 3, f * w, 10), color, true);
                acc += f;
            }

            Segment(Palette.Processing, live[(int)MachineState.Processing]);
            Segment(Palette.WaitingForJob, live[(int)MachineState.WaitingForJob]);
            Segment(Palette.WaitingForWorker, live[(int)MachineState.WaitingForWorker]);
            Segment(Palette.WaitingForInput, live[(int)MachineState.WaitingForInput]);

            DrawString(font, new Vector2(x + w + 8, rowY + rowH * 0.68f), $"{live[(int)MachineState.Processing] * 100.0 / Math.Max(1, elapsed):0}%", HorizontalAlignment.Left, -1, 12, Palette.Text);
            rowY += rowH;
        }
    }

    private void DrawBottomRight(Font font, Vector2 size)
    {
        var rect = Layout.BottomRightRect(size);
        DrawRect(rect, Palette.PanelBg, true);
        DrawLine(new Vector2(rect.Position.X, rect.Position.Y), new Vector2(rect.End.X, rect.Position.Y), Palette.PanelBorder, 1);
        DrawString(font, new Vector2(rect.Position.X + 14, rect.Position.Y + 22), "LIVE", HorizontalAlignment.Left, -1, 13, Palette.DimText);

        long elapsed = DisplayTimeMs;
        double hours = elapsed / 3_600_000.0;
        double through = hours > 0 ? Sim!.ShippedCount / hours : 0;

        float x = rect.Position.X + 14;
        float y = rect.Position.Y + 42;
        DrawString(font, new Vector2(x, y), $"SHIPPED     {Sim!.ShippedCount}", HorizontalAlignment.Left, -1, 14, Palette.Text);
        DrawString(font, new Vector2(x, y + 24), $"WIP         {Sim.Items.Values.Count(i => i.ShippedAt < 0)}", HorizontalAlignment.Left, -1, 14, Palette.Text);
        DrawString(font, new Vector2(x, y + 48), $"THROUGHPUT  {through:0.0}/hr", HorizontalAlignment.Left, -1, 14, Palette.Text);
        DrawString(font, new Vector2(x, y + 72), $"EVENTS      {Sim.EventsProcessed:N0}", HorizontalAlignment.Left, -1, 14, Palette.Text);

        float x2 = x + 200;
        DrawString(font, new Vector2(x2, y), $"Q A  {QueueLen("QUEUE_A")}", HorizontalAlignment.Left, -1, 14, Palette.QueueBar);
        DrawString(font, new Vector2(x2, y + 24), $"Q B  {QueueLen("QUEUE_B")}", HorizontalAlignment.Left, -1, 14, Palette.Text);
        DrawString(font, new Vector2(x2, y + 48), $"Q C  {QueueLen("QUEUE_C")}", HorizontalAlignment.Left, -1, 14, Palette.Text);
        DrawString(font, new Vector2(x2, y + 72), $"LEAD  {SimTime.FormatDuration(AvgLead())}", HorizontalAlignment.Left, -1, 14, Palette.Text);
    }

    private int QueueLen(string id) => Sim!.Queues.TryGetValue(id, out var q) ? q.Items.Count : 0;

    private long AvgLead()
    {
        if (Sim!.ShippedLeadTimesMs.Count == 0)
        {
            return 0;
        }

        return (long)Sim.ShippedLeadTimesMs.Average();
    }

    private void DrawSummary(Font font, Vector2 size, RunResult result)
    {
        var s = result.Summary;
        var rect = Layout.FloorRect(size);
        float w = 430;
        float h = 300;
        var box = new Rect2(rect.GetCenter() - new Vector2(w / 2f, h / 2f), new Vector2(w, h));
        DrawRect(box, new Color(Palette.PanelBg, 0.95f), true);
        DrawRect(box, Palette.PanelBorder, false, 1);

        float x = box.Position.X + 24;
        float y = box.Position.Y + 28;
        DrawString(font, new Vector2(x, y), "SHIFT COMPLETE", HorizontalAlignment.Left, -1, 18, Palette.Accent);
        DrawString(font, new Vector2(x, y + 30), $"Shipped        {s.Shipped}", HorizontalAlignment.Left, -1, 15, Palette.Text);
        DrawString(font, new Vector2(x, y + 56), $"Throughput     {s.ThroughputPerHour:0.0}/hr", HorizontalAlignment.Left, -1, 15, Palette.Text);
        DrawString(font, new Vector2(x, y + 82), $"Avg Lead Time  {SimTime.FormatDurationMs(s.AverageLeadTimeMs)}", HorizontalAlignment.Left, -1, 15, Palette.Text);
        DrawString(font, new Vector2(x, y + 108), $"Peak WIP       {s.PeakWip}", HorizontalAlignment.Left, -1, 15, Palette.Text);
        DrawString(font, new Vector2(x, y + 134), $"Events         {result.EventsProcessed:N0}", HorizontalAlignment.Left, -1, 15, Palette.Text);
        DrawString(font, new Vector2(x, y + 168), "HASH", HorizontalAlignment.Left, -1, 11, Palette.DimText);
        DrawString(font, new Vector2(x, y + 186), result.FinalStateHash[..24], HorizontalAlignment.Left, -1, 11, Palette.DimText);
        DrawString(font, new Vector2(x, y + 232), "R  restart", HorizontalAlignment.Left, -1, 13, Palette.DimText);
    }

    private static string Pct(long ms, long elapsed) =>
        elapsed > 0 ? $"{ms * 100.0 / elapsed:0.0}%   ({SimTime.FormatDuration(ms)})" : "-";

    private static string StateLabel(MachineState state) => state switch
    {
        MachineState.Processing => "RUN",
        MachineState.WaitingForJob => "HAS JOB",
        MachineState.WaitingForWorker => "NEEDS WORKER",
        _ => "NO INPUT"
    };

    private static string WorkerStateLabel(WorkerState state) => state switch
    {
        WorkerState.Moving => "MOVING",
        WorkerState.Loading => "LOADING",
        WorkerState.Unloading => "UNLOADING",
        _ => "IDLE"
    };

    private static Color MachineColor(MachineState state) => state switch
    {
        MachineState.Processing => Palette.Processing,
        MachineState.WaitingForJob => Palette.WaitingForJob,
        MachineState.WaitingForWorker => Palette.WaitingForWorker,
        _ => Palette.WaitingForInput
    };

    private void DrawCenteredIn(Font font, Rect2 rect, string text, int size, Color color)
    {
        float tw = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        float th = font.GetHeight(size);
        DrawString(font, new Vector2(rect.GetCenter().X - tw / 2f, rect.GetCenter().Y + th * 0.32f), text, HorizontalAlignment.Left, -1, size, color);
    }
}
