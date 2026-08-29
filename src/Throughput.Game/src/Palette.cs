using Godot;

namespace Throughput.Game;

public static class Palette
{
    public static readonly Color Background = new("#090a0c");
    public static readonly Color Floor = new("#111419");
    public static readonly Color Grid = new("#1b2027");
    public static readonly Color PanelBg = new("#0d1117");
    public static readonly Color PanelBorder = new("#21262d");
    public static readonly Color Inactive = new("#555b66");
    public static readonly Color Text = new("#c9d1d9");
    public static readonly Color DimText = new("#8b949e");
    public static readonly Color Accent = new("#3fb950");

    public static readonly Color Source = new("#388bfd");
    public static readonly Color Dock = new("#8b949e");

    public static readonly Color Processing = new("#3fb950");
    public static readonly Color WaitingForJob = new("#d29922");
    public static readonly Color WaitingForWorker = new("#f0883e");
    public static readonly Color WaitingForInput = new("#555b66");

    public static readonly Color WorkerBusy = new("#58a6ff");
    public static readonly Color WorkerIdle = new("#555b66");
    public static readonly Color Item = new("#e6edf3");
    public static readonly Color QueueBar = new("#f85149");
}
