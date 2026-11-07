if (Test-Path build) { Remove-Item build -Recurse -Force }

# Build the src projects and the docs generator tools (solution filters don't support wildcards, so the filter is generated)
$solutionFilter = Join-Path ([System.IO.Path]::GetTempPath()) "Silverback.Docs.slnf"
$projects = Get-ChildItem ../src/*/*.csproj | ForEach-Object { "src/$($_.Directory.Name)/$($_.Name)" }
$projects += Get-ChildItem ../tools/Silverback.Tools.Generators.Docs.*/*.csproj | ForEach-Object { "tools/$($_.Directory.Name)/$($_.Name)" }

@{
    solution = @{
        path = (Resolve-Path ../Silverback.slnx).Path
        projects = @($projects)
    }
} | ConvertTo-Json -Depth 3 | Set-Content $solutionFilter

# Single node: the tools share the output directory and would otherwise race copying the same dependencies
dotnet build -c Release $solutionFilter --no-incremental -m:1 -o build
$buildExitCode = $LASTEXITCODE

Remove-Item $solutionFilter

if ($buildExitCode -ne 0) { exit $buildExitCode }

# Generate the reference tables included by the guides
dotnet build/Silverback.Tools.Generators.Docs.Headers.dll build/generated
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build/Silverback.Tools.Generators.Docs.LogEvents.dll build/generated
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet tool restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet docfx --serve --port 4242
exit $LASTEXITCODE
