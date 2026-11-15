[CmdletBinding()]
param(
    # The release tag (e.g. v5.7.0 or v5.8.0-beta.1)
    [Parameter(Mandatory)]
    [string] $Tag
)

# Checks that the tag can be released and looks up the test runs on the tagged commit. Called by the release workflow, but
# can also run locally (with the GitHub CLI authenticated) to check a tag before pushing it.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryPath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot ".." "..")).Path

function Write-Step {
    param([string] $Message)

    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Invoke-Native {
    # A simple function, without a param block: an advanced one would bind arguments like -o or -v to its common parameters
    $command = $args[0]
    $arguments = @($args | Select-Object -Skip 1)
    & $command @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $($args -join ' ')"
    }
}

function Set-Output {
    param([string] $Name, [string] $Value)

    Write-Host "$Name=$Value"
    if ($env:GITHUB_OUTPUT) {
        Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "$Name=$Value"
    }
}

function Get-TestRun {
    param([string] $Repository, [string] $WorkflowFile, [string] $Sha)

    $runs = @(
        (Invoke-Native gh api "repos/$Repository/actions/workflows/$WorkflowFile/runs?head_sha=$Sha&per_page=100" |
            ConvertFrom-Json).workflow_runs |
            # A canceled run says nothing about the tests, and a filtered run of the extended tests doesn't run all of them
            Where-Object { $_.conclusion -notin @("cancelled", "skipped") -and -not $_.display_title.Contains("[filter:") } |
            Sort-Object -Property created_at -Descending
    )

    if ($runs.Count -eq 0) {
        Write-Host "'$WorkflowFile' didn't run on $Sha yet: it will run as part of the release."
        return [pscustomobject] @{ State = "missing"; RunId = "" }
    }

    $run = $runs[0]
    if ($run.status -ne "completed") {
        Write-Host "'$WorkflowFile' is running on $Sha ($($run.html_url)): the release will wait for it."
        return [pscustomobject] @{ State = "running"; RunId = [string] $run.id }
    }

    if ($run.conclusion -ne "success") {
        throw "The latest '$WorkflowFile' run on $Sha concluded with '$($run.conclusion)': $($run.html_url)"
    }

    Write-Host "'$WorkflowFile' succeeded on $Sha ($($run.html_url))."
    return [pscustomobject] @{ State = "success"; RunId = [string] $run.id }
}

Push-Location $repositoryPath
try {
    if (-not $Tag.StartsWith("v", [StringComparison]::Ordinal)) {
        throw "The tag '$Tag' doesn't start with 'v'."
    }

    $version = $Tag.Substring(1)
    $sha = (Invoke-Native git rev-parse "$Tag^{commit}" | Out-String).Trim()

    # The version, the release notes and the packages are read from the working tree
    $headSha = (Invoke-Native git rev-parse HEAD | Out-String).Trim()
    if ($headSha -ne $sha) {
        throw "The tag '$Tag' ($sha) isn't checked out (HEAD is $headSha)."
    }

    $repository = if ($env:GITHUB_REPOSITORY) {
        $env:GITHUB_REPOSITORY
    }
    else {
        (Invoke-Native gh repo view --json nameWithOwner --jq .nameWithOwner | Out-String).Trim()
    }

    Write-Step "Checking the version"
    $projectVersion = (Invoke-Native dotnet msbuild src/Silverback.Core/Silverback.Core.csproj "-getProperty:Version" -nologo | Out-String).Trim()
    if ($projectVersion -cne $version) {
        throw "The tag '$Tag' doesn't match the version '$projectVersion' in Directory.Build.props."
    }

    Write-Host "The tag matches the version '$version'."

    Write-Step "Checking that $sha is on main"
    Invoke-Native git fetch --quiet origin main
    & git merge-base --is-ancestor $sha origin/main
    if ($LASTEXITCODE -ne 0) {
        throw "The tagged commit $sha isn't on main."
    }

    Write-Host "The tagged commit is on main."

    Write-Step "Checking the release notes"
    & (Join-Path $repositoryPath "docs" "set-release-date.ps1") -Version $version -Check

    Write-Step "Checking that the GitHub release doesn't exist yet"
    & gh release view $Tag --repo $repository --json tagName 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        throw "The GitHub release '$Tag' already exists."
    }

    $global:LASTEXITCODE = 0
    Write-Host "The GitHub release doesn't exist yet."

    # These would otherwise only fail after pushing the packages (or, for the nuget environment, let them be pushed without
    # approval, since a missing environment is created on the fly)
    Write-Step "Checking the release setup"
    $pagesBuildType = (Invoke-Native gh api "repos/$repository/pages" --jq .build_type | Out-String).Trim()
    if ($pagesBuildType -ne "workflow") {
        throw "GitHub Pages must be deployed by GitHub Actions (build type 'workflow'), but its build type is '$pagesBuildType'."
    }

    $nugetEnvironment = Invoke-Native gh api "repos/$repository/environments/nuget" | ConvertFrom-Json
    $reviewersRules = @($nugetEnvironment.protection_rules | Where-Object { $_.type -eq "required_reviewers" })
    if ($reviewersRules.Count -eq 0) {
        throw "The 'nuget' environment must require a reviewer."
    }

    # The maintainer pushes the tag and approves the deployment
    if (@($reviewersRules | Where-Object { $_.prevent_self_review }).Count -gt 0) {
        throw "The 'nuget' environment must not prevent self-review."
    }

    $pagesEnvironment = Invoke-Native gh api "repos/$repository/environments/github-pages" | ConvertFrom-Json
    $pagesPolicy = $pagesEnvironment.deployment_branch_policy
    if ($null -ne $pagesPolicy) {
        $tagPolicies = @()
        if ($pagesPolicy.custom_branch_policies) {
            $tagPolicies = @(
                (Invoke-Native gh api "repos/$repository/environments/github-pages/deployment-branch-policies" | ConvertFrom-Json).branch_policies |
                    Where-Object { $_.type -eq "tag" -and $Tag -like $_.name }
            )
        }

        if ($tagPolicies.Count -eq 0) {
            throw "The 'github-pages' environment doesn't allow deployments from the tag '$Tag'."
        }
    }

    if ($env:GITHUB_ACTIONS -eq "true" -and [string]::IsNullOrWhiteSpace($env:NUGET_USER)) {
        throw "The NUGET_USER repository variable (the nuget.org user of the trusted publishing policy) isn't set."
    }

    Write-Host "GitHub Pages, the environments and the variables are set up."

    Write-Step "Checking that the packages don't exist on nuget.org yet"
    $packageIds = @(& (Join-Path $repositoryPath "nuget" "pack.ps1") -List)
    foreach ($packageId in $packageIds) {
        $response = Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/$($packageId.ToLowerInvariant())/index.json" -SkipHttpErrorCheck
        if ($response.StatusCode -eq 404) {
            Write-Host "$packageId is a new package."
            continue
        }

        if ($response.StatusCode -ne 200) {
            throw "Could not check the $packageId versions on nuget.org (HTTP $($response.StatusCode))."
        }

        $versions = @(($response.Content | ConvertFrom-Json).versions)
        if ($versions -contains $version.ToLowerInvariant()) {
            throw "$packageId $version already exists on nuget.org."
        }
    }

    Write-Host "None of the $($packageIds.Count) packages exists in version $version."

    Write-Step "Looking up the test runs"
    $continuous = Get-TestRun $repository "continuous.yml" $sha
    $extended = Get-TestRun $repository "extended-integration-tests.yml" $sha

    Write-Step "Outputs"
    Set-Output "version" $version
    Set-Output "continuous_state" $continuous.State
    Set-Output "continuous_run_id" $continuous.RunId
    Set-Output "extended_state" $extended.State
    Set-Output "extended_run_id" $extended.RunId
}
finally {
    Pop-Location
}
