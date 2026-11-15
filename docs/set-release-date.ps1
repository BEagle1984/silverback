[CmdletBinding()]
param(
    # The released version (e.g. 5.7.0, 5.8.0-beta.1 or v5.7.0)
    [Parameter(Mandatory)]
    [string] $Version,

    # The release date, in the yyyy-MM-dd format; defaults to today (UTC)
    [string] $Date = [DateTime]::UtcNow.ToString("yyyy-MM-dd", [Globalization.CultureInfo]::InvariantCulture),

    # Only check that the release notes contain the section of the version, without changing them
    [switch] $Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Version = $Version.TrimStart('v')
if ($Version -notmatch '^(\d+)\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
    throw "'$Version' is not a valid version."
}

$majorVersion = $Matches[1]

if ($Date -notmatch '^\d{4}-\d{2}-\d{2}$') {
    throw "'$Date' is not a valid date (expected yyyy-MM-dd)."
}

$changelogPath = Join-Path $PSScriptRoot "changelog" "$($majorVersion)00.md"
if (-not (Test-Path -LiteralPath $changelogPath)) {
    throw "The release notes file '$changelogPath' doesn't exist."
}

$content = [IO.File]::ReadAllText($changelogPath)
$newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }
$lines = [Collections.Generic.List[string]]::new([string[]] ($content -split "\r?\n"))

$expectedHeading = "## [$Version](https://github.com/BEagle1984/silverback/releases/tag/v$Version)"
$headingIndex = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i].StartsWith("## [$Version]", [StringComparison]::Ordinal)) {
        $headingIndex = $i
        break
    }
}

if ($headingIndex -lt 0) {
    throw "The release notes for version '$Version' were not found in '$changelogPath'."
}

if ($lines[$headingIndex] -cne $expectedHeading) {
    throw "The release notes heading '$($lines[$headingIndex])' doesn't match the expected '$expectedHeading'."
}

if ($Check) {
    Write-Host "The release notes for version '$Version' were found in '$changelogPath'."
    return
}

# The heading is followed by a blank line, the optional release date paragraph and another blank line
$dateLine = "<p class=`"release-date`"><i class=`"bi bi-calendar3`"></i> Released on $Date</p>"
$firstContentIndex = $headingIndex + 1
while ($firstContentIndex -lt $lines.Count -and [string]::IsNullOrWhiteSpace($lines[$firstContentIndex])) {
    $firstContentIndex++
}

if ($firstContentIndex -lt $lines.Count -and $lines[$firstContentIndex].StartsWith('<p class="release-date">', [StringComparison]::Ordinal)) {
    $lines[$firstContentIndex] = $dateLine
    Write-Host "Updated the release date of version '$Version' to $Date."
}
else {
    $lines.InsertRange($headingIndex + 1, [string[]] @("", $dateLine))
    Write-Host "Added the release date $Date to version '$Version'."
}

[IO.File]::WriteAllText($changelogPath, ($lines -join $newLine), [Text.UTF8Encoding]::new($false))
