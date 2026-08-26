[CmdletBinding()]
param(
    [switch] $NoPush,
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

foreach ($argument in $RemainingArguments) {
    if ($argument -eq "--no-push") {
        $NoPush = $true
    }
    else {
        throw "Unknown argument '$argument'."
    }
}

function Write-Step {
    param([string] $Message)

    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Invoke-Git {
    param([Parameter(ValueFromRemainingArguments)][string[]] $Arguments)

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed: git $($Arguments -join ' ')"
    }
}

function Get-GitOriginUrl {
    param([string] $Path)

    $urls = @(Invoke-Git -C $Path remote get-url origin)
    if ($urls.Count -ne 1 -or [string]::IsNullOrWhiteSpace($urls[0])) {
        throw "Could not determine the origin URL of '$Path'."
    }

    return ([string]$urls[0]).Trim()
}

$repositoryPath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$repositoryParentPath = Split-Path -Parent $repositoryPath
$publishRepositoryPath = Join-Path $repositoryParentPath "silverback-ghpages"
$sitePath = Join-Path $repositoryPath "docs\_site"
$buildPropsPath = Join-Path $repositoryPath "Directory.Build.props"
$publishBranch = "gh-pages"
$originUrl = Get-GitOriginUrl $repositoryPath

Write-Step "Checking the generated site"
if (-not (Test-Path -LiteralPath $sitePath -PathType Container)) {
    throw "The generated site was not found at '$sitePath'. Run the documentation build first."
}

$siteItems = @(Get-ChildItem -LiteralPath $sitePath -Force)
if ($siteItems.Count -eq 0) {
    throw "The generated site at '$sitePath' is empty."
}

Write-Step "Checking the documentation publishing repository"
if (Test-Path -LiteralPath $publishRepositoryPath) {
    Write-Host "The publishing repository already exists at '$publishRepositoryPath'."

    if (-not (Test-Path -LiteralPath (Join-Path $publishRepositoryPath ".git"))) {
        throw "The existing folder is not a Git clone. Refusing to delete its contents: '$publishRepositoryPath'."
    }

    $publishOriginUrl = Get-GitOriginUrl $publishRepositoryPath
    if (-not [string]::Equals($publishOriginUrl, $originUrl, [StringComparison]::Ordinal)) {
        throw "The publishing repository does not use the source repository's origin URL."
    }
}
else {
    Write-Host "Cloning '$originUrl' into '$publishRepositoryPath'."
    Invoke-Git clone $originUrl $publishRepositoryPath
}

Write-Step "Switching the publishing repository to branch '$publishBranch'"
Invoke-Git -C $publishRepositoryPath switch $publishBranch

Write-Step "Updating the publishing repository"
Invoke-Git -C $publishRepositoryPath pull --ff-only origin $publishBranch

Write-Step "Removing the previously published files"
$itemsToRemove = Get-ChildItem -LiteralPath $publishRepositoryPath -Force |
    Where-Object { $_.Name -notin @(".git", "CNAME") }

if ($itemsToRemove) {
    $itemsToRemove | ForEach-Object {
        Write-Host "Removing '$($_.Name)'."
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
    }
}
else {
    Write-Host "There are no previously published files to remove."
}

Write-Step "Copying the generated site"
$siteItems | Copy-Item -Destination $publishRepositoryPath -Recurse -Force
Write-Host "Copied the contents of '$sitePath' to '$publishRepositoryPath'."

Write-Step "Reading the Silverback version"
[xml] $buildProps = Get-Content -LiteralPath $buildPropsPath -Raw
$baseVersion = [string](
    $buildProps.Project.PropertyGroup |
        Where-Object { $null -ne $_.BaseVersion } |
        Select-Object -First 1 -ExpandProperty BaseVersion
)
$baseVersionSuffix = [string](
    $buildProps.Project.PropertyGroup |
        Where-Object { $null -ne $_.BaseVersionSuffix } |
        Select-Object -First 1 -ExpandProperty BaseVersionSuffix
)

if ([string]::IsNullOrWhiteSpace($baseVersion)) {
    throw "BaseVersion was not found in '$buildPropsPath'."
}

$version = if ($baseVersion.Contains('$(BaseVersionSuffix)')) {
    $baseVersion.Replace('$(BaseVersionSuffix)', $baseVersionSuffix)
}
else {
    "$baseVersion$baseVersionSuffix"
}
if ($version.Contains('$(')) {
    throw "The version '$version' contains an unresolved MSBuild property."
}

Write-Host "Publishing documentation for version '$version'."

Write-Step "Staging and committing the published site"
Invoke-Git -C $publishRepositoryPath add --all

$pendingChanges = (& git -C $publishRepositoryPath status --porcelain)
if ($LASTEXITCODE -ne 0) {
    throw "Could not inspect the publishing repository status."
}

if ($pendingChanges) {
    $commitMessage = "docs: update docs for v$version"
    Invoke-Git -C $publishRepositoryPath commit -m $commitMessage
    Write-Host "Created commit '$commitMessage'."
}
else {
    Write-Host "There are no documentation changes to commit."
}

if ($NoPush) {
    Write-Step "Skipping push because --no-push was specified"
}
else {
    Write-Step "Pushing branch '$publishBranch' to origin"
    Invoke-Git -C $publishRepositoryPath push origin $publishBranch
    Write-Host "Documentation published successfully."
}
