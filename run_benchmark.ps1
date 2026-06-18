$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$benchmarkCsv = Join-Path $root 'bench\benchmark_results.csv'
$scenarioCsv = Join-Path $root 'bench\scenario_results.csv'

dotnet run --project (Join-Path $root 'tools\BenchmarkCli\BenchmarkCli.csproj') --configuration Release -- `
    --output $benchmarkCsv `
    --scenario-output $scenarioCsv

$python = Get-Command py -ErrorAction SilentlyContinue
if ($python) {
    py -3 (Join-Path $root 'scripts\plot_benchmarks.py') `
        --input $benchmarkCsv `
        --output-dir (Join-Path $root 'docs')
} else {
    Write-Warning 'Python launcher "py" was not found; benchmark CSV was generated, but plots were not regenerated.'
}
