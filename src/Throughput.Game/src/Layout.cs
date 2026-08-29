using Godot;

namespace Throughput.Game;

public static class Layout
{
    public const float TopBarH = 46;
    public const float InspectorW = 330;
    public const float BottomH = 200;
    public const float Margin = 12;

    private const float SpeedW = 52;
    private const float SpeedGap = 8;
    private const float SpeedH = 30;

    public static Rect2 FloorRect(Vector2 size) =>
        new(0, TopBarH, size.X - InspectorW, size.Y - TopBarH - BottomH);

    public static Rect2 InspectorRect(Vector2 size) =>
        new(size.X - InspectorW, TopBarH, InspectorW, size.Y - TopBarH - BottomH);

    public static Rect2 BottomLeftRect(Vector2 size) =>
        new(0, size.Y - BottomH, (size.X - InspectorW) * 0.62f, BottomH);

    public static Rect2 BottomRightRect(Vector2 size) =>
        new((size.X - InspectorW) * 0.62f, size.Y - BottomH, size.X - (size.X - InspectorW) * 0.62f, BottomH);

    public static Rect2[] SpeedButtonRects(Vector2 size)
    {
        float y = (TopBarH - SpeedH) / 2f;
        float x0 = size.X - Margin - (4 * SpeedW + 3 * SpeedGap);
        return
        [
            new Rect2(x0, y, SpeedW, SpeedH),
            new Rect2(x0 + SpeedW + SpeedGap, y, SpeedW, SpeedH),
            new Rect2(x0 + 2 * (SpeedW + SpeedGap), y, SpeedW, SpeedH),
            new Rect2(x0 + 3 * (SpeedW + SpeedGap), y, SpeedW, SpeedH)
        ];
    }

    public static Rect2 PauseRect(Vector2 size)
    {
        var first = SpeedButtonRects(size)[0];
        return new Rect2(first.Position.X - 12 - 74, first.Position.Y, 74, SpeedH);
    }
}
