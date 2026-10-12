#!/bin/bash
set -euo pipefail

# Compile and exercise the real workflow against deterministic agents, without Nitro or live model calls.
skill_dir="$(cd "$(dirname "$0")/.." && pwd)"
test_dir="$(mktemp -d "${TMPDIR:-/tmp}/nitro-wave-tests.XXXXXX")"
trap 'rm -rf "$test_dir"' EXIT
sdk_major="$(dotnet --version | cut -d. -f1)"
cat > "$test_dir/tests.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net${sdk_major}.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
EOF
cat "$skill_dir/assets/workflows/nitro-backlog-wave.workflow.cs" \
  "$skill_dir/scripts/venti-workflow-test-stubs.cs" > "$test_dir/Program.cs"
dotnet build "$test_dir/tests.csproj" --nologo >&2
for scenario in pass fix dismissed contradictory missing-ruling empty-plan cap blocked agent-failed resume five-tickets; do
  WAVE_TEST_SCENARIO="$scenario" dotnet "$test_dir/bin/Debug/net${sdk_major}.0/tests.dll"
done

# Invalid input must fail before any agent starts.
ulimit -c 0
for scenario in six-tickets four-cycles zero-cycles; do
  if WAVE_TEST_SCENARIO="$scenario" dotnet "$test_dir/bin/Debug/net${sdk_major}.0/tests.dll" > "$test_dir/rejection.log" 2>&1; then
    echo "FAIL $scenario: workflow accepted invalid input" >&2
    exit 1
  fi
  if [ "$scenario" = six-tickets ]; then
    expected="Split this wave into smaller runs to stay within 50 agents."
  else
    expected="maxCycles must be between 1 and 3."
  fi
  grep -F "System.ArgumentException: $expected" "$test_dir/rejection.log" > /dev/null
  echo "PASS $scenario"
done
