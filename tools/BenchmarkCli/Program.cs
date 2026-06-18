using SmartEmergencyRoutePlanner.Benchmark;

string root = FindRepositoryRoot(Directory.GetCurrentDirectory());
string benchmarkCsv = Path.Combine(root, "bench", "benchmark_results.csv");
string scenarioCsv = Path.Combine(root, "bench", "scenario_results.csv");

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--output" && i + 1 < args.Length)
    {
        benchmarkCsv = ResolvePath(root, args[++i]);
    }
    else if (args[i] == "--scenario-output" && i + 1 < args.Length)
    {
        scenarioCsv = ResolvePath(root, args[++i]);
    }
    else if (args[i] is "-h" or "--help")
    {
        PrintHelp();
        return;
    }
}

Directory.CreateDirectory(Path.GetDirectoryName(benchmarkCsv)!);
Directory.CreateDirectory(Path.GetDirectoryName(scenarioCsv)!);

Console.WriteLine("Smart Emergency Route Planner benchmark");
Console.WriteLine($"Repository root : {root}");
Console.WriteLine($"Benchmark CSV   : {benchmarkCsv}");
Console.WriteLine($"Scenario CSV    : {scenarioCsv}");
Console.WriteLine("Seed            : 42");
Console.WriteLine("Sizes           : 100, 500, 1000, 5000, 10000");
Console.WriteLine("Graph families  : RandomSparse, GridCity");
Console.WriteLine();

BenchmarkRunner.RunAll(benchmarkCsv);
BenchmarkRunner.RunScenarioMatrix(scenarioCsv);

static string ResolvePath(string root, string path)
{
    return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(root, path));
}

static string FindRepositoryRoot(string start)
{
    var directory = new DirectoryInfo(start);
    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "SmartEmergencyRoutePlanner.csproj")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return start;
}

static void PrintHelp()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project tools/BenchmarkCli/BenchmarkCli.csproj --configuration Release");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --output <path>           Benchmark CSV path. Default: bench/benchmark_results.csv");
    Console.WriteLine("  --scenario-output <path>  Scenario CSV path. Default: bench/scenario_results.csv");
}
