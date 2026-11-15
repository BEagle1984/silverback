[CmdletBinding()]
param()

# Tags the current commit of main with the version in Directory.Build.props and pushes the tag, which starts the release
# workflow (.github/workflows/release.yml). See the Releasing section of the README.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-Git {
    # A simple function, without a param block: an advanced one would bind arguments like -C or -q to its common parameters
    $output = & git @args
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed: git $($args -join ' ')"
    }

    return $output
}

Push-Location $PSScriptRoot
try {
    $branch = (Invoke-Git branch --show-current | Out-String).Trim()
    if ($branch -ne "main") {
        throw "The release must be tagged on main, but the current branch is '$branch'."
    }

    if (Invoke-Git status --porcelain) {
        throw "The working tree has uncommitted changes."
    }

    Invoke-Git fetch --quiet --tags origin main | Out-Null
    $head = (Invoke-Git rev-parse HEAD | Out-String).Trim()
    $remoteHead = (Invoke-Git rev-parse origin/main | Out-String).Trim()
    if ($head -ne $remoteHead) {
        throw "The local main ($($head.Substring(0, 9))) isn't in sync with origin/main ($($remoteHead.Substring(0, 9)))."
    }

    # Quoted, otherwise PowerShell splits the argument at the colon
    $version = (& dotnet msbuild src/Silverback.Core/Silverback.Core.csproj "-getProperty:Version" -nologo | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
        throw "Could not determine the version from Directory.Build.props (got '$version')."
    }

    $tag = "v$version"
    & git rev-parse --quiet --verify "refs/tags/$tag" | Out-Null
    if ($LASTEXITCODE -eq 0) {
        throw "The tag '$tag' already exists."
    }

    $global:LASTEXITCODE = 0

    # Throws if the release notes don't contain the section of the version, with the heading linking to the tag
    & (Join-Path $PSScriptRoot "docs" "set-release-date.ps1") -Version $version -Check

    $commit = (Invoke-Git log -1 --format="%h %s" | Out-String).Trim()

    Write-Host "Release " -NoNewline
    Write-Host $tag -ForegroundColor Green -NoNewline
    Write-Host " from $commit"
    # Redirected (or empty) input would answer the prompt with nothing, which would count as a yes
    if ([Console]::IsInputRedirected) {
        throw "The confirmation needs an interactive console."
    }

    $answer = Read-Host "Create and push the tag? [Y/n]"
    if ($null -eq $answer -or (-not [string]::IsNullOrWhiteSpace($answer) -and $answer.Trim() -notmatch '^(y|yes)$')) {
        Write-Host "Canceled."
        return
    }

    Invoke-Git tag $tag | Out-Null
    try {
        Invoke-Git push --quiet origin "refs/tags/$tag" | Out-Null
    }
    catch {
        # Don't leave a local tag behind that was never pushed
        & git tag --delete $tag | Out-Null
        throw
    }

    Write-Host "Pushed the tag '$tag': https://github.com/BEagle1984/silverback/actions/workflows/release.yml" -ForegroundColor Green
}
finally {
    Pop-Location
}
