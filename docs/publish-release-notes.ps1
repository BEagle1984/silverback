[CmdletBinding(SupportsShouldProcess)]
param(
    # The version to publish (e.g. 5.7.0 or v5.7.0); defaults to the version in Directory.Build.props
    [string] $Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string] $Message)

    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Invoke-Gh {
    param([Parameter(ValueFromRemainingArguments)][string[]] $Arguments)

    & gh @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub CLI command failed: gh $($Arguments -join ' ')"
    }
}

function Get-BuildPropsVersion {
    param([string] $Path)

    [xml] $buildProps = Get-Content -LiteralPath $Path -Raw
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
        throw "BaseVersion was not found in '$Path'."
    }

    return $baseVersion.Replace('$(BaseVersionSuffix)', $baseVersionSuffix)
}

function Get-ReleaseSection {
    param([string] $Path, [string] $Version)

    $lines = (Get-Content -LiteralPath $Path -Raw) -split "\r?\n"
    $headingRegex = [regex] "^## \[$([regex]::Escape($Version))\]\("
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($headingRegex.IsMatch($lines[$i])) {
            $start = $i + 1
            break
        }
    }

    if ($start -lt 0) {
        throw "The release notes for version '$Version' were not found in '$Path'."
    }

    $end = $lines.Count
    for ($i = $start; $i -lt $lines.Count; $i++) {
        if ($lines[$i].StartsWith("## ")) {
            $end = $i
            break
        }
    }

    return $lines[$start..($end - 1)]
}

function Test-ApiUid {
    param([string] $Uid)

    # The API uids are the fully qualified type or member names, while the conceptual pages use lowercase uids
    return $Uid -cmatch '^[A-Z]'
}

function Get-ApiManifest {
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Warning "The API metadata was not found at '$Path'. Run the documentation build to qualify the members with their type name."
        return @{}
    }

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable
}

function Get-ContentPageLinks {
    param([string] $Path)

    $links = @{}
    if (-not (Test-Path -LiteralPath $Path)) {
        return $links
    }

    $uid = $null
    $name = $null
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match '^- uid: (.+)$') {
            $uid = $Matches[1]
            $name = $uid
        }
        elseif ($line -match '^  name: (.+)$') {
            $name = $Matches[1]
        }
        elseif ($line -match '^  href: (.+)$' -and $uid -and -not (Test-ApiUid $uid)) {
            $links[$uid] = "[$name](https://silverback-messaging.net/$($Matches[1]))"
        }
    }

    return $links
}

function Get-ShortName {
    param([string] $Name)

    $name = ($Name -split '\(')[0].TrimEnd('*')
    $name = $name.Substring($name.LastIndexOf('.') + 1)

    return $name -replace '`+\d+$', ''
}

function ConvertTo-ApiName {
    param([string] $Uid, [hashtable] $ApiManifest)

    # Mirror the docfx short name (dropping the namespace, the parameters and the generic arity), but qualify the
    # members with their type name, since the API metadata stores them in their type's file
    $uid = ($Uid -split '\?')[0].TrimEnd('*')
    $typeFile = $ApiManifest[$uid]
    if ($typeFile) {
        $typeUidLength = [IO.Path]::GetFileNameWithoutExtension($typeFile).Length
        if ($typeUidLength -lt $uid.Length -and $uid[$typeUidLength] -eq '.') {
            $typeName = Get-ShortName $uid.Substring(0, $typeUidLength)
            $memberName = Get-ShortName $uid.Substring($typeUidLength + 1)
            return "``$typeName.$memberName``"
        }
    }

    return "``$(Get-ShortName $uid)``"
}

function ConvertTo-GitHubMarkdown {
    param([string[]] $Lines, [hashtable] $ApiManifest, [hashtable] $ContentPageLinks)

    $xrefRegex = [regex] '<xref:([^>]+)>'
    $apiLinkRegex = [regex] '\[([^\]]+)\]\(xref:[A-Z][^)]*\)'
    $contentLinkRegex = [regex] '\[([^\]]+)\]\((?!https?://)([^)]+)\)'

    # Remove the references to the documentation pages, e.g. "(see <xref:producing>)"
    $seeAlsoRegex = [regex] ' ?\(see (also )?[^()]*\)'

    $result = foreach ($line in $Lines) {
        if ($line -match '^<p class="release-date">') {
            continue
        }

        $line = $seeAlsoRegex.Replace(
            $line,
            {
                param($match)

                $hasContentReferences =
                    $contentLinkRegex.IsMatch($match.Value) -or
                    @($xrefRegex.Matches($match.Value) | Where-Object { -not (Test-ApiUid $_.Groups[1].Value) }).Count -gt 0

                if ($hasContentReferences) { "" } else { $match.Value }
            })

        $line = $apiLinkRegex.Replace($line, '`$1`')
        $line = $contentLinkRegex.Replace($line, '$1')

        $line = $xrefRegex.Replace(
            $line,
            {
                param($match)

                $uid = $match.Groups[1].Value
                if (Test-ApiUid $uid) {
                    return ConvertTo-ApiName $uid $ApiManifest
                }

                # A documentation page referenced outside of a "see" remark is part of the sentence, so it is linked
                $link = $ContentPageLinks[($uid -split '#')[0]]
                if (-not $link) {
                    throw "Cannot resolve the documentation page '$uid'. Run the documentation build first."
                }

                $link
            })

        # GitHub expects the alert types in uppercase
        $line = [regex]::Replace($line, '^> \[!(\w+)\]', { param($match) "> [!$($match.Groups[1].Value.ToUpperInvariant())]" })

        $line
    }

    # Trim the leading and trailing blank lines and collapse the consecutive ones
    $text = ($result -join "`n") -replace "\n{3,}", "`n`n"

    return $text.Trim()
}

$repositoryPath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$changelogPath = Join-Path $PSScriptRoot "changelog"

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-BuildPropsVersion (Join-Path $repositoryPath "Directory.Build.props")
}

$Version = $Version.TrimStart('v')
$tag = "v$Version"

if ($Version -notmatch '^(\d+)\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
    throw "'$Version' is not a valid version."
}

$majorVersion = [int] $Matches[1]
$isPrerelease = $Version.Contains('-')
$changelogFileName = "$($majorVersion)00"

Write-Step "Checking tag '$tag'"
& gh api "repos/{owner}/{repo}/git/ref/tags/$tag" --silent 2>$null
if ($LASTEXITCODE -ne 0) {
    throw "The tag '$tag' does not exist on GitHub. Push the tag first."
}

Write-Host "The tag '$tag' exists."

Write-Step "Reading the release notes"
$section = Get-ReleaseSection (Join-Path $changelogPath "$changelogFileName.md") $Version
$apiManifest = Get-ApiManifest (Join-Path $PSScriptRoot "api\.manifest")
$contentPageLinks = Get-ContentPageLinks (Join-Path $PSScriptRoot "_site\xrefmap.yml")
$anchor = $Version.ToLowerInvariant() -replace '[^0-9a-z-]', ''
$notes = (ConvertTo-GitHubMarkdown $section $apiManifest $contentPageLinks) +
    "`n`nSee [docs](https://silverback-messaging.net/changelog/$changelogFileName.html#$anchor) for details.`n"

Write-Host $notes

Write-Step "Publishing the GitHub release '$tag'"
& gh release view $tag --json tagName 2>$null | Out-Null
$releaseExists = $LASTEXITCODE -eq 0
$global:LASTEXITCODE = 0

$notesFilePath = [IO.Path]::GetTempFileName()
try {
    [IO.File]::WriteAllText($notesFilePath, $notes, [Text.UTF8Encoding]::new($false))

    if ($releaseExists) {
        if ($PSCmdlet.ShouldProcess($tag, "Replace the GitHub release notes")) {
            Invoke-Gh release edit $tag --notes-file $notesFilePath
            Write-Host "Replaced the notes of the existing release '$tag'."
        }
    }
    else {
        $createArguments = @("release", "create", $tag, "--verify-tag", "--title", $tag, "--notes-file", $notesFilePath)
        if ($isPrerelease) {
            $createArguments += "--prerelease"
        }

        if ($PSCmdlet.ShouldProcess($tag, "Create the GitHub release")) {
            Invoke-Gh @createArguments
            Write-Host "Created the release '$tag'."
        }
    }
}
finally {
    Remove-Item -LiteralPath $notesFilePath -Force -WhatIf:$false
}
