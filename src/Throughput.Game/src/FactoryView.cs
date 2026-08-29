using Godot;
using Throughput.Sim;

namespace Throughput.Game;

public partial class FactoryView : Node2D
{
    public Simulation? Sim { get; set; }
    public Scenario? Scenario { get; set; }
    public long DisplayTimeMs { get; set; }
    public Rect2 VisibleWorldRect { get; set; }
    public float ViewZoom { get; set; } = 1f;
    public (string Kind, string Id)? Selection { get; set; }

    private const int Tile = 80;
    private const float MachineSize = 132f;

    private const float LaneHalfW = 3f * Tile;
    private const float LaneSquare = 38f;
    private const float LaneStep = LaneSquare + 6f;
    private const int LanePerRow = 9;

    private float Ls => Mathf.Clamp(1f / ViewZoom, 0.72f, 1.8f);

    public (string Kind, string Id)? HitTest(Vector2 world)
    {
        if (Sim is null)
        {
            return null;
        }

        foreach (var m in Sim.Machines.Values)
        {
            var rect = MachineRect(m.Position);
            if (rect.HasPoint(world))
            {
                return ("MACHINE", m.Id);
            }
        }

        foreach (var q in QueuesWithLanes())
        {
            var itemIds = q.Items;
            for (int i = 0; i < itemIds.Count; i++)
            {
                if (LaneSquareRect(q, i).HasPoint(world))
                {
                    return ("ITEM", itemIds[i]);
                }
            }
        }

        foreach (var w in Sim.Workers.Values)
        {
            if (P(w.PositionAt(DisplayTimeMs)).DistanceTo(world) <= 18 * Ls)
            {
                return ("WORKER", w.Id);
            }
        }

        return null;
    }

    public override void _Draw()
    {
        if (Sim is null || Scenario is null)
        {
            return;
        }

        var scenario = Scenario;
        var font = ThemeDB.FallbackFont;
        float ls = Ls;

        DrawRect(VisibleWorldRect, Palette.Floor, true);

        int gx0 = Mathf.FloorToInt(VisibleWorldRect.Position.X / Tile);
        int gx1 = Mathf.CeilToInt((VisibleWorldRect.Position.X + VisibleWorldRect.Size.X) / Tile);
        int gy0 = Mathf.FloorToInt(VisibleWorldRect.Position.Y / Tile);
        int gy1 = Mathf.CeilToInt((VisibleWorldRect.Position.Y + VisibleWorldRect.Size.Y) / Tile);
        for (int x = gx0; x <= gx1; x++)
        {
            DrawLine(new Vector2(x * Tile, gy0 * Tile), new Vector2(x * Tile, gy1 * Tile), Palette.Grid, 1);
        }

        for (int y = gy0; y <= gy1; y++)
        {
            DrawLine(new Vector2(gx0 * Tile, y * Tile), new Vector2(gx1 * Tile, y * Tile), Palette.Grid, 1);
        }

        DrawStation(scenario.SourcePosition, Palette.Source, "RAW", "∞");
        DrawStation(scenario.DockPosition, Palette.Dock, "DOCK", null);

        foreach (var q in QueuesWithLanes())
        {
            DrawQueueLane(q);
        }

        foreach (var item in Sim.Items.Values)
        {
            if (item.ShippedAt >= 0 || item.Location.Kind == "QUEUE")
            {
                continue;
            }

            DrawCircle(ItemPos(item), 4.5f * ls, Palette.Item);
        }

        foreach (var w in Sim.Workers.Values.OrderBy(w => w.Id, StringComparer.Ordinal))
        {
            DrawWorker(w);
        }

        if (Selection is { Kind: "ITEM" } && Sim.Items.TryGetValue(Selection.Value.Id, out var selectedItem))
        {
            DrawItemTrace(selectedItem);
        }

        foreach (var m in Sim.Machines.Values.OrderBy(m => m.Id, StringComparer.Ordinal))
        {
            DrawMachine(m);
        }

        DrawSelectionHighlight();
    }

    private void DrawStation(GridPosition gp, Color color, string label, string? sub)
    {
        var font = ThemeDB.FallbackFont;
        float ls = Ls;
        var center = P(gp);
        var rect = new Rect2(center - new Vector2(MachineSize / 2f, MachineSize / 2f), new Vector2(MachineSize, MachineSize));
        DrawRect(rect, new Color(color, 0.30f), true);
        DrawRect(rect, color, false, 2.5f * ls);
        DrawCentered(font, center + new Vector2(0, -10 * ls), label, (int)(19 * ls), Palette.Text);
        if (sub is not null)
        {
            DrawCentered(font, center + new Vector2(0, 16 * ls), sub, (int)(13 * ls), Palette.DimText);
        }
    }

    private void DrawMachine(Machine m)
    {
        var font = ThemeDB.FallbackFont;
        float ls = Ls;
        var color = MachineColor(m);
        var rect = MachineRect(m.Position);

        DrawRect(rect, new Color(Palette.PanelBg), true);
        DrawRect(rect, color, false, 3f * ls);
        DrawRect(rect, new Color(color, 0.12f), true);

        DrawCentered(font, rect.GetCenter() + new Vector2(0, -22 * ls), ReportName(m.Id), (int)(17 * ls), Palette.Text);
        DrawCentered(font, rect.GetCenter() + new Vector2(0, 2 * ls), StateLabel(m.State), (int)(14 * ls), color);

        string job = m.CurrentItemId ?? m.OutputItemId ?? "";
        if (job.Length > 0)
        {
            DrawCentered(font, rect.GetCenter() + new Vector2(0, 30 * ls), job[^4..], (int)(11 * ls), Palette.DimText);
        }
    }

    private void DrawQueueLane(Queue q)
    {
        var font = ThemeDB.FallbackFont;
        float ls = Ls;
        var machine = Sim!.Machines[q.FeedsMachineId];
        var pos = P(machine.Position);

        int count = q.Items.Count;
        int rows = Math.Max(1, Mathf.CeilToInt(count / (float)LanePerRow));

        float y0 = pos.Y + Tile * 1.18f;
        float w = LaneHalfW * 2f;
        float h = rows * LaneStep + 8;
        var lane = new Rect2(pos.X - LaneHalfW, y0, w, h);
        DrawRect(lane, new Color(Palette.PanelBg, 0.6f), true);
        DrawRect(lane, count > 24 ? Palette.QueueBar : Palette.PanelBorder, false, 1.5f * ls);

        for (int i = 0; i < count; i++)
        {
            var itemId = q.Items[i];
            if (!Sim.Items.TryGetValue(itemId, out var item))
            {
                continue;
            }

            var r = LaneSquareRect(q, i);
            DrawRect(r, LaneItemColor(item, q.Id), true);
            DrawRect(r, new Color(Palette.Background, 0.6f), false, 1f);
        }

        string label = $"{ReportName(q.Id)}  {count}";
        float lw = font.GetStringSize(label, HorizontalAlignment.Left, -1, (int)(13 * ls)).X;
        DrawString(font, new Vector2(pos.X - LaneHalfW, y0 + h + 18 * ls), label, HorizontalAlignment.Left, -1, (int)(13 * ls), count > 24 ? Palette.QueueBar : Palette.DimText);
        DrawString(font, new Vector2(pos.X - LaneHalfW + lw + 10 * ls, y0 + h + 18 * ls), WaitSummary(q), HorizontalAlignment.Left, -1, (int)(11 * ls), Palette.DimText);
    }

    private void DrawWorker(Worker w)
    {
        var font = ThemeDB.FallbackFont;
        float ls = Ls;
        var pos = P(w.PositionAt(DisplayTimeMs));
        var busy = w.State != WorkerState.Idle;
        DrawCircle(pos, 6 * ls, busy ? Palette.WorkerBusy : Palette.WorkerIdle);
        DrawCircle(pos, 6 * ls, new Color(Palette.Background, 0.3f), false, 1.5f);
        DrawString(font, pos + new Vector2(10 * ls, -14 * ls), w.Id.Replace("WORKER_", "W"), HorizontalAlignment.Left, -1, (int)(11 * ls), Palette.DimText);
    }

    private void DrawItemTrace(Item item)
    {
        float ls = Ls;
        var pts = new List<Vector2>();
        string? last = null;
        foreach (var tr in item.Trace)
        {
            var p = TracePos(tr.Location);
            if (p is not Vector2 pos || tr.Location == last)
            {
                continue;
            }

            pts.Add(pos);
            last = tr.Location;
        }

        if (pts.Count >= 2)
        {
            DrawPolyline(pts.ToArray(), new Color(Palette.Accent, 0.7f), 3 * ls, true);
        }

        foreach (var pt in pts)
        {
            DrawCircle(pt, 5 * ls, Palette.Accent);
        }
    }

    private void DrawSelectionHighlight()
    {
        float ls = Ls;
        if (Selection is not { } sel)
        {
            return;
        }

        if (sel.Kind == "MACHINE" && Sim!.Machines.TryGetValue(sel.Id, out var m))
        {
            var rect = MachineRect(m.Position).Grow(5 * ls);
            DrawRect(rect, Palette.Text, false, 2 * ls);
        }
        else if (sel.Kind == "WORKER" && Sim!.Workers.TryGetValue(sel.Id, out var worker))
        {
            DrawArc(P(worker.PositionAt(DisplayTimeMs)), 13 * ls, 0, Mathf.Tau, 24, Palette.Text, 2 * ls);
        }
        else if (sel.Kind == "ITEM" && Sim!.Items.TryGetValue(sel.Id, out var item))
        {
            var pos = ItemPos(item);
            DrawArc(pos, 12 * ls, 0, Mathf.Tau, 24, Palette.Text, 2 * ls);
        }
    }

    private IEnumerable<Queue> QueuesWithLanes()
    {
        if (Sim is null || Scenario is null)
        {
            return [];
        }

        return Sim.Queues.Values
            .Where(q => q.Id != Scenario.DockId && q.Id != "SOURCE_BUFFER")
            .OrderBy(q => q.Id, StringComparer.Ordinal);
    }

    private Rect2 MachineRect(GridPosition gp) =>
        new(P(gp) - new Vector2(MachineSize / 2f, MachineSize / 2f), new Vector2(MachineSize, MachineSize));

    private Rect2 LaneSquareRect(Queue q, int index)
    {
        var machine = Sim!.Machines[q.FeedsMachineId];
        var pos = P(machine.Position);
        int row = index / LanePerRow;
        int col = index % LanePerRow;
        float x = pos.X - LaneHalfW + 6 + col * LaneStep;
        float y = pos.Y + Tile * 1.18f + 4 + row * LaneStep;
        return new Rect2(x, y, LaneSquare, LaneSquare);
    }

    private Color LaneItemColor(Item item, string queueId)
    {
        long age = AgeInLocation(item, queueId);
        if (age > 300_000)
        {
            return Palette.QueueBar;
        }

        if (age > 60_000)
        {
            return Palette.WaitingForJob;
        }

        return Palette.Item;
    }

    private long AgeInLocation(Item item, string locationId)
    {
        for (int i = item.Trace.Count - 1; i >= 0; i--)
        {
            if (item.Trace[i].Location == locationId)
            {
                return DisplayTimeMs - item.Trace[i].Time;
            }
        }

        return 0;
    }

    private string WaitSummary(Queue q)
    {
        if (q.Items.Count == 0)
        {
            return "";
        }

        long oldest = 0;
        foreach (var id in q.Items)
        {
            if (Sim!.Items.TryGetValue(id, out var item))
            {
                oldest = Math.Max(oldest, AgeInLocation(item, q.Id));
            }
        }

        return $"oldest {SimTime.FormatDuration(oldest)}";
    }

    private Vector2? TracePos(string location)
    {
        if (Sim is null || Scenario is null)
        {
            return null;
        }

        if (Sim.Machines.TryGetValue(location, out var machine))
        {
            return P(machine.Position);
        }

        if (Sim.Queues.TryGetValue(location, out var queue))
        {
            if (queue.Id == Scenario.DockId)
            {
                return P(Scenario.DockPosition);
            }

            if (Sim.Machines.TryGetValue(queue.FeedsMachineId, out var feeder))
            {
                return P(feeder.Position);
            }

            return null;
        }

        if (location == Scenario.SourceId)
        {
            return P(Scenario.SourcePosition);
        }

        if (location == Scenario.DockId)
        {
            return P(Scenario.DockPosition);
        }

        return null;
    }

    private Vector2 ItemPos(Item item)
    {
        switch (item.Location.Kind)
        {
            case "SOURCE":
                return P(Scenario!.SourcePosition) + StackOffset(QueueIndex("SOURCE_BUFFER", item.Id));
            case "QUEUE":
                var q = Sim!.Queues[item.Location.Id];
                var m = Sim.Machines[q.FeedsMachineId];
                return P(m.Position) + StackOffset(q.Items.IndexOf(item.Id));
            case "MACHINE":
                return P(Sim!.Machines[item.Location.Id].Position);
            case "WORKER":
                var w = Sim!.Workers[item.Location.Id];
                return P(w.PositionAt(DisplayTimeMs));
            case "DOCK":
                return P(Scenario!.DockPosition);
            default:
                return Vector2.Zero;
        }
    }

    private int QueueIndex(string queueId, string itemId)
    {
        if (queueId == "SOURCE_BUFFER")
        {
            int i = Sim!.Queues[queueId].Items.IndexOf(itemId);
            return i < 0 ? 0 : i;
        }

        return Sim!.Queues[queueId].Items.IndexOf(itemId);
    }

    private static Vector2 StackOffset(int index)
    {
        if (index < 0)
        {
            index = 0;
        }

        return new Vector2((index % 7) * 8 - 24, (index / 7) * 8 - 4);
    }

    private static Vector2 P(GridPosition gp) => new(gp.X * Tile, gp.Y * Tile);

    private void DrawCentered(Font font, Vector2 center, string text, int size, Color color)
    {
        float width = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        DrawString(font, center - new Vector2(width / 2f, -size * 0.35f), text, HorizontalAlignment.Left, -1, size, color);
    }

    private static Color MachineColor(Machine m) => m.State switch
    {
        MachineState.Processing => Palette.Processing,
        MachineState.WaitingForJob => Palette.WaitingForJob,
        MachineState.WaitingForWorker => Palette.WaitingForWorker,
        _ => Palette.WaitingForInput
    };

    private static string StateLabel(MachineState state) => state switch
    {
        MachineState.Processing => "RUN",
        MachineState.WaitingForJob => "HAS JOB",
        MachineState.WaitingForWorker => "NEEDS WORKER",
        _ => "NO INPUT"
    };

    public static string ReportName(string id)
    {
        var parts = id.Split('_');
        return parts.Length == 2 ? $"{char.ToUpperInvariant(parts[0][0])}{parts[0][1..].ToLowerInvariant()} {parts[1]}" : id;
    }
}
