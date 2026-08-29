using Godot;
using Throughput.Scenarios;
using Throughput.Sim;

namespace Throughput.Game;

public partial class GameRoot : Node2D
{
    private enum Speed
    {
        Paused,
        X1,
        X4,
        X16,
        Max
    }

    private const long MaxStepMs = 500_000;
    private const long MaxBudgetMs = 6;
    private const float MinZoom = 0.35f;
    private const float MaxZoom = 2.6f;

    private Simulation? _sim;
    private Scenario? _scenario;
    private FactoryView? _view;
    private Hud? _hud;
    private Camera2D? _camera;
    private long _displayTime;
    private bool _finished;
    private bool _manualZoom;
    private Vector2 _panOffset;
    private Speed _speed = Speed.X1;
    private string? _shotPath;
    private long _shotTimeMs;
    private bool _shotTaken;
    private int _shotWaitFrames;

    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--shot")
            {
                _shotPath = args[i + 1];
                i++;
            }
            else if (args[i] == "--shot-time")
            {
                _shotTimeMs = long.Parse(args[i + 1]);
                i++;
            }
        }

        StartScenario();

        if (_shotPath is not null)
        {
            _speed = Speed.Max;
        }
    }

    public override void _Process(double delta)
    {
        if (_sim is null || _view is null || _hud is null || _camera is null || _scenario is null || _finished)
        {
            return;
        }

        var sim = _sim;
        long duration = sim.ShiftDurationMs;

        switch (_speed)
        {
            case Speed.Paused:
                break;
            case Speed.X1:
            case Speed.X4:
            case Speed.X16:
            {
                long step = (long)(delta * 1000 * SpeedMultiplier());
                long target = Math.Min(_displayTime + step, duration);
                sim.AdvanceTo(target);
                _displayTime = target;
                break;
            }

            case Speed.Max:
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                while (watch.ElapsedMilliseconds < MaxBudgetMs && _displayTime < duration)
                {
                    long target = Math.Min(_displayTime + MaxStepMs, duration);
                    sim.AdvanceTo(target);
                    _displayTime = target;
                }

                break;
            }
        }

        if (_displayTime >= duration)
        {
            Finish();
        }

        UpdateCamera();

        _view.DisplayTimeMs = _displayTime;
        _view.VisibleWorldRect = CurrentWorldRect();
        _view.ViewZoom = _camera.Zoom.X;
        _view.QueueRedraw();
        _hud.DisplayTimeMs = _displayTime;
        _hud.SpeedIndex = SpeedIndex();
        _hud.Selection = _view.Selection;
        _hud.QueueRedraw();

        if (_shotPath is not null && !_shotTaken)
        {
            if (_shotWaitFrames++ < 10)
            {
                return;
            }

            _shotTaken = true;
            _ = CaptureShot();
        }
    }

    private async System.Threading.Tasks.Task CaptureShot()
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var img = GetViewport().GetTexture().GetImage();
        img.SavePng(_shotPath);
        GetTree().Quit();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            HandleMouse(mb);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            HandleKey(key);
            GetViewport().SetInputAsHandled();
        }
    }

    private void HandleMouse(InputEventMouseButton mb)
    {
        if (_camera is null)
        {
            return;
        }

        if (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown)
        {
            _manualZoom = true;
            float factor = mb.ButtonIndex == MouseButton.WheelUp ? 1.12f : 1f / 1.12f;
            var zoom = _camera.Zoom.X * factor;
            zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            _camera.Zoom = new Vector2(zoom, zoom);
            return;
        }

        if (mb.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        var size = GetViewport().GetVisibleRect().Size;
        var pos = mb.Position;

        var buttons = Layout.SpeedButtonRects(size);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].HasPoint(pos))
            {
                _speed = (Speed)(i + 1);
                return;
            }
        }

        if (Layout.PauseRect(size).HasPoint(pos))
        {
            _speed = _speed == Speed.Paused ? Speed.X1 : Speed.Paused;
            return;
        }

        if (Layout.FloorRect(size).HasPoint(pos))
        {
            _view!.Selection = _view.HitTest(_view.GetGlobalMousePosition());
        }
        else
        {
            _view!.Selection = null;
        }

        if (_view.Selection is null && _hud is not null)
        {
            _hud.Selection = null;
        }
    }

    private void HandleKey(InputEventKey key)
    {
        switch (key.Keycode)
        {
            case Key.Space:
                _speed = _speed == Speed.Paused ? Speed.X1 : Speed.Paused;
                break;
            case Key.Key1:
                _speed = Speed.X1;
                break;
            case Key.Key2:
                _speed = Speed.X4;
                break;
            case Key.Key3:
                _speed = Speed.X16;
                break;
            case Key.Key4:
                _speed = Speed.Max;
                break;
            case Key.R:
                StartScenario();
                break;
            case Key.F:
                _manualZoom = false;
                _panOffset = Vector2.Zero;
                break;
            case Key.Left:
                Pan(new Vector2(-1, 0));
                break;
            case Key.Right:
                Pan(new Vector2(1, 0));
                break;
            case Key.Up:
                Pan(new Vector2(0, -1));
                break;
            case Key.Down:
                Pan(new Vector2(0, 1));
                break;
        }
    }

    private void Pan(Vector2 dir)
    {
        _manualZoom = true;
        float zoom = _camera?.Zoom.X ?? 1f;
        _panOffset += dir * (60f / zoom);
    }

    private void UpdateCamera()
    {
        if (_camera is null || _scenario is null)
        {
            return;
        }

        var world = FactoryWorldBounds(_scenario);
        _camera.Position = world.GetCenter() + _panOffset;

        if (_manualZoom)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var floor = Layout.FloorRect(viewportSize);
        float fit = Mathf.Min(floor.Size.X / world.Size.X, floor.Size.Y / world.Size.Y);
        fit = Mathf.Clamp(fit, MinZoom, MaxZoom);
        _camera.Zoom = new Vector2(fit, fit);
    }

    private Rect2 CurrentWorldRect()
    {
        if (_camera is null)
        {
            return new Rect2();
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        float zoom = _camera.Zoom.X;
        return new Rect2(_camera.Position - viewportSize / (2f * zoom), viewportSize / zoom);
    }

    private void StartScenario()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        var scenario = ScenarioCatalog.Get("garage-01");
        _scenario = scenario;
        _sim = new Simulation(scenario);
        _displayTime = 0;
        _finished = false;
        _speed = Speed.X1;
        _manualZoom = false;
        _panOffset = Vector2.Zero;

        _view = new FactoryView
        {
            Sim = _sim,
            Scenario = scenario,
            DisplayTimeMs = 0,
            ViewZoom = 1f
        };
        AddChild(_view);

        _hud = new Hud
        {
            Sim = _sim,
            Scenario = scenario,
            DisplayTimeMs = 0
        };
        var layer = new CanvasLayer();
        AddChild(layer);
        layer.AddChild(_hud);

        _camera = new Camera2D();
        AddChild(_camera);
    }

    private void Finish()
    {
        _finished = true;
        _displayTime = _sim!.ShiftDurationMs;
        var result = _sim.CompleteShift();
        GD.Print($"SHIFT COMPLETE: {result.Summary.Shipped} shipped, hash {result.FinalStateHash}");
        if (_hud is not null)
        {
            _hud.FinalResult = result;
            _hud.QueueRedraw();
        }
    }

    private static Rect2 FactoryWorldBounds(Scenario scenario)
    {
        const float pad = 1.5f;
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        void Include(GridPosition p)
        {
            minX = Math.Min(minX, p.X);
            minY = Math.Min(minY, p.Y);
            maxX = Math.Max(maxX, p.X);
            maxY = Math.Max(maxY, p.Y);
        }

        foreach (var m in scenario.Machines)
        {
            Include(m.Position);
        }

        Include(scenario.SourcePosition);
        Include(scenario.DockPosition);

        const int tile = 80;
        float spanX = maxX - minX + 1 + 2 * pad;
        float spanY = maxY - minY + 1 + 2 * pad;
        return new Rect2(
            new Vector2((minX - pad) * tile, (minY - pad) * tile),
            new Vector2(spanX * tile, spanY * tile));
    }

    private int SpeedMultiplier() => _speed switch
    {
        Speed.X1 => 1,
        Speed.X4 => 4,
        Speed.X16 => 16,
        _ => 1
    };

    private int SpeedIndex() => _speed switch
    {
        Speed.Paused => 0,
        Speed.X1 => 1,
        Speed.X4 => 2,
        Speed.X16 => 3,
        Speed.Max => 4,
        _ => 1
    };
}
