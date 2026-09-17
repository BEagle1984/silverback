param(
    [string[]] $Versions = @("4.6.2")
)

$ErrorActionPreference = "Stop"
$benchmarkRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$sourceName = "Silverback.Tests.Extended.Benchmarks.VersionComparison.Current"
$sourceFolder = Join-Path $benchmarkRoot $sourceName

foreach ($version in $Versions) {
    if ($version -notmatch '^\d+\.\d+\.\d+$') {
        throw "Expected a stable package version such as 4.6.2."
    }

    $targetName = "Silverback.Tests.Extended.Benchmarks.VersionComparison.V" + $version.Replace(".", "_")
    $targetFolder = [IO.Path]::GetFullPath((Join-Path $benchmarkRoot $targetName))
    if (-not $targetFolder.StartsWith($benchmarkRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "The target must remain within the extended tests directory."
    }
    if (Test-Path -LiteralPath $targetFolder) {
        throw "$targetFolder already exists. Historical API adaptations must be preserved; choose a new version."
    }

    New-Item -ItemType Directory -Path $targetFolder | Out-Null
    Get-ChildItem -LiteralPath $sourceFolder | Where-Object Name -NotIn @("bin", "obj") |
        Copy-Item -Destination $targetFolder -Recurse

    $projectPath = Join-Path $targetFolder "$sourceName.csproj"
    [xml] $project = Get-Content -LiteralPath $projectPath
    $project.Project.PropertyGroup.RootNamespace = $targetName
    $pinning = $project.CreateElement("CentralPackageTransitivePinningEnabled")
    $pinning.InnerText = "false"
    $project.Project.PropertyGroup.AppendChild($pinning) | Out-Null

    foreach ($reference in @($project.SelectNodes("//ProjectReference"))) {
        $package = $project.CreateElement("PackageReference")
        $package.SetAttribute("Include", [IO.Path]::GetFileNameWithoutExtension($reference.Include))
        $package.SetAttribute("VersionOverride", $version)
        $reference.ParentNode.ReplaceChild($package, $reference) | Out-Null
    }
    $project.Save((Join-Path $targetFolder "$targetName.csproj"))
    Remove-Item -LiteralPath $projectPath

    Get-ChildItem -LiteralPath $targetFolder -Filter "*.cs" -Recurse | ForEach-Object {
        $content = [IO.File]::ReadAllText($_.FullName).Replace($sourceName, $targetName)
        [IO.File]::WriteAllText($_.FullName, $content, [Text.UTF8Encoding]::new($false))
    }
    [IO.File]::WriteAllText(
        (Join-Path $targetFolder ".editorconfig"),
        "[*.cs]`r`n# Historical semantic version in the namespace and assembly name.`r`ndotnet_diagnostic.CA1707.severity = none`r`n")

    Write-Host "Created $targetName. Adapt the workloads to this Silverback API before adding it to Silverback.Tests.Extended.sln."
}
