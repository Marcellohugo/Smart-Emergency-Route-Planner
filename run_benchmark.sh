#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BENCHMARK_CSV="$ROOT/bench/benchmark_results.csv"
SCENARIO_CSV="$ROOT/bench/scenario_results.csv"

dotnet run --project "$ROOT/tools/BenchmarkCli/BenchmarkCli.csproj" --configuration Release -- \
  --output "$BENCHMARK_CSV" \
  --scenario-output "$SCENARIO_CSV"

if command -v python3 >/dev/null 2>&1; then
  python3 "$ROOT/scripts/plot_benchmarks.py" \
    --input "$BENCHMARK_CSV" \
    --output-dir "$ROOT/docs"
else
  echo 'python3 was not found; benchmark CSV was generated, but plots were not regenerated.' >&2
fi
