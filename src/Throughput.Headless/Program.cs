namespace Throughput.Headless;

using System.Text.Json;
using Throughput.Scenarios;
using Throughput.Sim;

public static class Program
{
    public static int Main(string[] args)
    {
        string? scenarioId = null;
        string? commandsFile = null;
        string? saveReplay = null;
        string? saveSummary = null;
        string? compareFile = null;
        bool checkDeterminism = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--commands":
                    commandsFile = args[++i];
                    break;
                case "--save-replay":
                    saveReplay = args[++i];
                    break;
                case "--save-summary":
                    saveSummary = args[++i];
                    break;
                case "--compare":
                    compareFile = args[++i];
                    break;
                case "--check-determinism":
                    checkDeterminism = true;
                    break;
                default:
                    scenarioId = args[i];
                    break;
            }
        }

        if (scenarioId is null)
        {
            Console.Error.WriteLine("usage: Throughput.Headless <scenario> [--commands file.json] [--save-replay out.json] [--save-summary out.json] [--compare baseline.json] [--check-determinism]");
            Console.Error.WriteLine("scenarios: " + string.Join(", ", ScenarioCatalog.KnownScenarios));
            return 2;
        }

        try
        {
            var scenario = ScenarioCatalog.Get(scenarioId);
            var commands = commandsFile is not null ? ReplayRecord.FromJson(File.ReadAllText(commandsFile)).Commands : null;

            if (checkDeterminism)
            {
                var r1 = new Simulation(scenario, commands).Run();
                var r2 = new Simulation(scenario, commands).Run();
                Console.Write(Report.Format(r1));
                Console.WriteLine();
                Console.WriteLine("DETERMINISM CHECK");
                Console.WriteLine($"Run 1: {r1.FinalStateHash}");
                Console.WriteLine($"Run 2: {r2.FinalStateHash}");
                Console.WriteLine(r1.FinalStateHash == r2.FinalStateHash ? "MATCH" : "MISMATCH");
                return r1.FinalStateHash == r2.FinalStateHash ? 0 : 1;
            }

            var result = new Simulation(scenario, commands).Run();
            Console.Write(Report.Format(result));

            if (compareFile is not null)
            {
                var baseline = JsonSerializer.Deserialize<RunSummary>(File.ReadAllText(compareFile))!;
                Console.WriteLine();
                Console.WriteLine(Compare.Format(baseline, result.Summary));
            }

            if (saveReplay is not null)
            {
                var replay = new ReplayRecord
                {
                    ScenarioId = result.Scenario.Id,
                    ScenarioVersion = result.Scenario.Version,
                    RulesetVersion = result.Scenario.Ruleset.Version,
                    Commands = result.Commands
                };
                File.WriteAllText(saveReplay, replay.ToJson());
            }

            if (saveSummary is not null)
            {
                File.WriteAllText(saveSummary, JsonSerializer.Serialize(result.Summary, new JsonSerializerOptions { WriteIndented = true }));
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex);
            return 1;
        }
    }
}
