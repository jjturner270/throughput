namespace Throughput.Sim;

using System.Text;
using System.Text.Json;

public sealed class ReplayRecord
{
    public required string ScenarioId { get; init; }
    public required int ScenarioVersion { get; init; }
    public required string RulesetVersion { get; init; }
    public required IReadOnlyList<SimCommand> Commands { get; init; }

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"scenario\": ").Append(JsonSerializer.Serialize(ScenarioId)).Append(",\n");
        sb.Append("  \"scenarioVersion\": ").Append(ScenarioVersion).Append(",\n");
        sb.Append("  \"ruleset\": ").Append(JsonSerializer.Serialize(RulesetVersion)).Append(",\n");
        sb.Append("  \"commands\": [\n");
        for (int i = 0; i < Commands.Count; i++)
        {
            var c = Commands[i];
            sb.Append("    { \"time\": ").Append(c.Time);
            sb.Append(", \"type\": ").Append(JsonSerializer.Serialize(ToSnakeUpper(c.Type.ToString())));
            if (c.MachineId is not null)
            {
                sb.Append(", \"machine\": ").Append(JsonSerializer.Serialize(c.MachineId));
            }

            if (c.WorkerId is not null)
            {
                sb.Append(", \"worker\": ").Append(JsonSerializer.Serialize(c.WorkerId));
            }

            if (c.Position is { } pos)
            {
                sb.Append(", \"x\": ").Append(pos.X).Append(", \"y\": ").Append(pos.Y);
            }

            if (c.Priorities is { } prios)
            {
                sb.Append(", \"priorities\": [");
                sb.Append(string.Join(", ", prios.Select(p => JsonSerializer.Serialize(p))));
                sb.Append(']');
            }

            sb.Append(" }");
            if (i < Commands.Count - 1)
            {
                sb.Append(',');
            }

            sb.Append('\n');
        }

        sb.Append("  ]\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    public static ReplayRecord FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);        var root = doc.RootElement;
        var commands = new List<SimCommand>();
        foreach (var el in root.GetProperty("commands").EnumerateArray())
        {
            long time = el.GetProperty("time").GetInt64();
            string typeText = el.GetProperty("type").GetString()!.Replace("_", "");
            var type = Enum.Parse<CommandType>(typeText, ignoreCase: true);
            string? machine = el.TryGetProperty("machine", out var m) ? m.GetString() : null;
            string? worker = el.TryGetProperty("worker", out var w) ? w.GetString() : null;
            GridPosition? pos = null;
            if (el.TryGetProperty("x", out var x) && el.TryGetProperty("y", out var y))
            {
                pos = new GridPosition(x.GetInt32(), y.GetInt32());
            }

            IReadOnlyList<string>? priorities = null;
            if (el.TryGetProperty("priorities", out var p))
            {
                priorities = p.EnumerateArray().Select(e => e.GetString()!).ToList();
            }

            commands.Add(new SimCommand(time, type, machine, pos, worker, priorities));
        }

        return new ReplayRecord
        {
            ScenarioId = root.GetProperty("scenario").GetString()!,
            ScenarioVersion = root.GetProperty("scenarioVersion").GetInt32(),
            RulesetVersion = root.GetProperty("ruleset").GetString()!,
            Commands = commands
        };
    }

    private static string ToSnakeUpper(string value)
    {
        var sb = new StringBuilder();
        foreach (char ch in value)
        {
            if (char.IsUpper(ch) && sb.Length > 0)
            {
                sb.Append('_');
            }

            sb.Append(char.ToUpperInvariant(ch));
        }

        return sb.ToString();
    }
}
