if (Test-Path build) { Remove-Item build -Recurse -Force }

# Build only the src projects (solution filters don't support wildcards, so the filter is generated)
$solutionFilter = Join-Path ([System.IO.Path]::GetTempPath()) "Silverback.Docs.slnf"
$projects = Get-ChildItem ../src/*/*.csproj | ForEach-Object { "src/$($_.Directory.Name)/$($_.Name)" }

@{
    solution = @{
        path = (Resolve-Path ../Silverback.slnx).Path
        projects = @($projects)
    }
} | ConvertTo-Json -Depth 3 | Set-Content $solutionFilter

dotnet build -c Release $solutionFilter --no-incremental -o build

Remove-Item $solutionFilter

docfx --serve --port 4242
