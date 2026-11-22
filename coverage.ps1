# Builds and tests the main solution with coverage, then generates and opens an HTML report.
# The tests that need Docker (PostgreSQL) start their own containers, so Docker must be running.
# -UnitOnly leaves out the E2E tests, to show what the unit tests alone cover.
param(
    [switch] $UnitOnly
)

# Keep the coverlet options in sync with .github/workflows/sonar.yml
$coverletOptions = @(
    "--coverlet",
    "--coverlet-output-format", "opencover",
    "--coverlet-exclude", "[xunit.*]*",
    "--coverlet-exclude", "[Silverback.Tests.*]*",
    "--coverlet-exclude", "[*]*.Migrations.*",
    "--coverlet-exclude-by-attribute", "GeneratedCodeAttribute",
    "--coverlet-skip-auto-props",
    "--coverlet-exclude-assemblies-without-sources", "MissingAll"
)

# The E2E project has no test left by the filter and returns exit code 8
$filterOptions = @()
if ($UnitOnly)
{
    $filterOptions = @("--filter", "FullyQualifiedName!~Silverback.Tests.Integration.E2E", "--ignore-exit-code", "8")
}

dotnet build Silverback.slnx --configuration Release
# The coverage reports have a timestamp in their name, so remove those of the previous runs
Remove-Item -Recurse -Force TestResults/coverage -ErrorAction SilentlyContinue
dotnet test --solution Silverback.slnx --no-build --no-restore --configuration Release --report-trx @filterOptions @coverletOptions --results-directory TestResults/coverage --results-directory-layout per-module
dotnet tool restore
dotnet tool run reportgenerator -reports:"TestResults/coverage/**/*.opencover.*.xml" -targetdir:coveragereport -filefilters:"-*LoggerMessage.g.cs" -reporttypes:HTMLInline
. ./coveragereport/index.htm
