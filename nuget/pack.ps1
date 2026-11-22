[CmdletBinding()]
param(
    # The build configuration
    [Alias("c")]
    [string] $Configuration = "Release",

    # Don't treat the build warnings as errors
    [Alias("w")]
    [switch] $NoWarnAsError,

    # Only list the IDs of the packages that would be built, in build order
    [switch] $List
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$outputPath = $PSScriptRoot
$sourcePath = Join-Path $PSScriptRoot ".." "src"

function Write-Step {
    param([string] $Message)

    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Invoke-Dotnet {
    # A simple function, without a param block: an advanced one would bind arguments like -o or -v to its common parameters
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed: dotnet $($args -join ' ')"
    }
}

function Get-ProjectReferenceRegex {
    param([string] $ProjectName)

    # The references are written with backslashes, as Visual Studio does, but are accepted with forward slashes too
    $escapedName = [regex]::Escape($ProjectName)
    return [regex] "<ProjectReference Include=`"\.\.[\\/]$escapedName[\\/]$escapedName\.csproj`""
}

function Get-PackageProjects {
    # Sort the projects so that each one is packed after the projects it references, since these references are replaced
    # with references to the freshly built packages
    $projects = @{}
    foreach ($directory in Get-ChildItem -LiteralPath $sourcePath -Directory) {
        $projectPath = Join-Path $directory.FullName "$($directory.Name).csproj"
        if (Test-Path -LiteralPath $projectPath) {
            $projects[$directory.Name] = $projectPath
        }
    }

    $dependencies = @{}
    foreach ($name in $projects.Keys) {
        $content = Get-Content -LiteralPath $projects[$name] -Raw
        $dependencies[$name] = @($projects.Keys | Where-Object { $_ -ne $name -and (Get-ProjectReferenceRegex $_).IsMatch($content) })
    }

    $sorted = [Collections.Generic.List[string]]::new()
    $remaining = [Collections.Generic.SortedSet[string]]::new([string[]] $projects.Keys, [StringComparer]::Ordinal)
    while ($remaining.Count -gt 0) {
        $ready = @($remaining | Where-Object { @($dependencies[$_] | Where-Object { -not $sorted.Contains($_) }).Count -eq 0 })
        if ($ready.Count -eq 0) {
            throw "The project references contain a cycle: $($remaining -join ', ')."
        }

        foreach ($name in $ready) {
            $sorted.Add($name)
            $remaining.Remove($name) | Out-Null
        }
    }

    return $sorted | ForEach-Object { [pscustomobject] @{ Name = $_; Path = $projects[$_] } }
}

function Get-PackageVersion {
    param([string] $ProjectPath)

    # Quoted, otherwise PowerShell splits the argument at the colon when forwarding it
    $version = (Invoke-Dotnet msbuild $ProjectPath "-getProperty:Version" -nologo | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($version) -or $version.Contains('$(')) {
        throw "Could not determine the package version from '$ProjectPath'."
    }

    return $version
}

function Clear-Output {
    Get-ChildItem -LiteralPath $outputPath -Force |
        Where-Object { $_.Name -ne (Split-Path -Leaf $PSCommandPath) } |
        Remove-Item -Recurse -Force
}

function Clear-CachedPackages {
    param([object[]] $Projects, [string] $Version)

    # A package rebuilt with the same version would otherwise be restored from the global packages folder instead of the
    # output folder
    $globalPackagesPath = ((Invoke-Dotnet nuget locals global-packages --list | Out-String) -replace '^\s*global-packages:\s*', '').Trim()
    foreach ($project in $Projects) {
        $cachedPath = Join-Path $globalPackagesPath $project.Name.ToLowerInvariant() $Version.ToLowerInvariant()
        if (Test-Path -LiteralPath $cachedPath) {
            Remove-Item -LiteralPath $cachedPath -Recurse -Force
            Write-Host "Removed the cached '$($project.Name)' $Version package."
        }
    }
}

function Invoke-Pack {
    param([pscustomobject] $Project, [object[]] $Projects)

    $backupPath = "$($Project.Path).original"
    Copy-Item -LiteralPath $Project.Path -Destination $backupPath -Force

    try {
        $content = Get-Content -LiteralPath $Project.Path -Raw
        foreach ($referencedProject in $Projects) {
            $content = (Get-ProjectReferenceRegex $referencedProject.Name).Replace(
                $content,
                "<PackageReference Include=`"$($referencedProject.Name)`"")
        }

        # The analyzer references are build-time only and don't become package dependencies
        $remainingReferences = @(
            $content -split "\r?\n" |
                Where-Object { $_ -match '<ProjectReference ' -and $_ -notmatch 'OutputItemType="Analyzer".*PrivateAssets="all"' }
        )
        if ($remainingReferences.Count -gt 0) {
            throw "'$($Project.Path)' still references another project after the replacement: $($remainingReferences[0].Trim())"
        }

        [IO.File]::WriteAllText($Project.Path, $content, [Text.UTF8Encoding]::new($true))

        # Restore into a separate folder (see Directory.Build.props): an IDE with the solution open reacts to the modified
        # project file by restoring it from its own, unmodified project model, and would otherwise overwrite
        # obj\project.assets.json in the middle of the build
        $restorePathArgument = "/p:SilverbackPackRestore=true"

        $buildArguments = @(
            "build", $Project.Path, "-c", $Configuration, "--no-incremental", "--nologo", "-v", "q",
            "/p:ContinuousIntegrationBuild=true", "/p:EmbedUntrackedSources=true", $restorePathArgument)
        if (-not $NoWarnAsError) {
            $buildArguments += "-warnaserror"
        }

        Invoke-Dotnet @buildArguments
        Invoke-Dotnet pack $Project.Path -c $Configuration -o $outputPath --no-build --nologo -v q $restorePathArgument
    }
    finally {
        Move-Item -LiteralPath $backupPath -Destination $Project.Path -Force
    }
}

$projects = @(Get-PackageProjects)

if ($List) {
    $projects | ForEach-Object { $_.Name }
    return
}

$stopwatch = [Diagnostics.Stopwatch]::StartNew()

$version = Get-PackageVersion $projects[0].Path
Write-Host "Packing version $version ($Configuration) of: $($projects.Name -join ', ')."

Write-Step "Clearing the output folder and the cached packages"
Clear-Output
Clear-CachedPackages $projects $version

foreach ($project in $projects) {
    Write-Step "Packing $($project.Name)"
    Invoke-Pack $project $projects
}

Write-Step "Checking the packages"
$missingFiles = @(
    foreach ($project in $projects) {
        foreach ($extension in @("nupkg", "snupkg")) {
            $fileName = "$($project.Name).$version.$extension"
            if (-not (Test-Path -LiteralPath (Join-Path $outputPath $fileName))) {
                $fileName
            }
        }
    }
)
if ($missingFiles.Count -gt 0) {
    throw "The following packages are missing: $($missingFiles -join ', ')."
}

$expectedCount = $projects.Count * 2
$actualCount = @(Get-ChildItem -LiteralPath $outputPath -File | Where-Object { $_.Extension -in @(".nupkg", ".snupkg") }).Count
if ($actualCount -ne $expectedCount) {
    throw "Expected $expectedCount package files, but found $actualCount in '$outputPath'."
}

$projects | ForEach-Object { Write-Host "[$version] $($_.Name)" -ForegroundColor Green }
Write-Host "`nPacked $($projects.Count) packages in $($stopwatch.Elapsed.ToString('mm\:ss'))."
