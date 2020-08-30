$repositoryLocation = "."
[bool]$global:clearCache = $FALSE
[bool]$global:warnAsError = $TRUE
$global:buildConfiguration = "Release"

function Check-Location()
{
    [string]$currentLocation = Get-Location

    if (-Not(Test-Path "./Update.ps1"))
    {
        Write-Host "This script is supposed to run in the /nuget folder of the main Silverback repository!" -ForegroundColor Red
        Exit
    }
}

function Check-Args([string[]]$argsArray)
{
    For ($i = 0; $i -le $argsArray.Length; $i++) {
        $arg = $argsArray[$i]

        if ($arg -eq "--configuration" -Or $arg -eq "-c")
        {
            $global:buildConfiguration = $argsArray[$i + 1]
        }
        elseif ($arg -eq "--clear-cache" -Or $arg -eq "-l")
        {
            $global:clearCache = $TRUE
        }
        elseif ($arg -eq "--no-warnaserror" -Or $arg -eq "-w")
        {
            $global:warnAsError = $FALSE
        }
    }
}

function Pack-All()
{
    foreach ($sourceProjectName in Get-SourceProjectNames)
    {
        Write-Host "Packing $sourceProjectName ($global:buildConfiguration)...`n" -ForegroundColor Yellow

        $projectFilePath = "../src/$sourceProjectName/$sourceProjectName.csproj"

        Backup-ProjectFile $projectFilePath
        Remove-ProjectReferences $projectFilePath

        Write-Host "Building..."

        if ($global:warnAsError -eq $TRUE)
        {
            dotnet build -c $global:buildConfiguration $projectFilePath --no-incremental --nologo -v q -warnaserror
        }
        else
        {
            dotnet build -c $global:buildConfiguration $projectFilePath --no-incremental --nologo -v q
        }

        Write-Host ""
        Write-Host "Packing..."

        dotnet pack -c $global:buildConfiguration $projectFilePath -o . --no-build --nologo -v q

        Write-Host ""

        Restore-ProjectFile $projectFilePath

        Write-Separator
    }
}

function Get-SourceProjectNames()
{
    return Get-ChildItem -Path ../src -Directory | Select-Object -ExpandProperty Name
}

function Remove-ProjectReferences([string]$projectFilePath)
{
    Remove-ProjectReference $projectFilePath "Silverback.Core" "Silverback.Core" '$(BaseVersion)'
    Remove-ProjectReference $projectFilePath "Silverback.Integration" "Silverback.Integration" '$(BaseVersion)'
    Remove-ProjectReference $projectFilePath "Silverback.Integration.Kafka" "Silverback.Integration.Kafka" '$(BaseVersion)'

    Test-ProjectReferenceReplaced $projectFilePath
}

function Remove-ProjectReference([string] $projectFilePath, [string]$projectToReplace, [string]$packageName, [string]$packageVersion)
{
    Write-Host "Replace $projectToReplace reference..." -NoNewline
    $find = "<ProjectReference Include=`"..\\$projectToReplace\\$projectToReplace.csproj`" />"
    $replace = "<PackageReference Include=`"$projectToReplace`" Version=`"$packageVersion`" />"
    ((Get-Content -path $projectFilePath -Raw) -replace $find, $replace) | Set-Content -Path $projectFilePath
    Write-Host "OK" -ForegroundColor Green
}

function Test-ProjectReferenceReplaced([string]$projectFilePath)
{
    $result = Select-String -Path $projectFilePath -Pattern ".csproj"

    if ($result -ne $null)
    {
        Restore-ProjectFile $projectFilePath
        throw "The file $projectFilePath still contains one or more references to another .csproj file."
    }
}

function Backup-ProjectFile([string] $projectFilePath)
{
    Write-Host "Backing up project file..." -NoNewline
    Copy-Item $projectFilePath -Destination "$projectFilePath.original"
    Write-Host "OK" -ForegroundColor Green
}

function Restore-ProjectFile([string] $projectFilePath)
{
    Write-Host "Restoring project file..." -NoNewline
    Copy-Item $projectFilePath -Destination "$projectFilePath.modified"
    Copy-Item "$projectFilePath.original" -Destination $projectFilePath
    Write-Host "OK" -ForegroundColor Green
}

function Delete-All()
{
    Write-Host "Deleting everything in target folder..." -ForegroundColor Yellow -NoNewline

    Get-ChildItem -exclude Update.ps1 |
            Remove-Item -Force -Recurse |
            Out-Null

    Write-Host "OK" -ForegroundColor Green

    Write-Separator
}

function Show-Summary()
{
    Write-Host "Available packages:`n" -ForegroundColor Yellow

    $hashtable = @{ }

    $files = Get-ChildItem $repositoryLocation -Recurse -Filter *.nupkg

    foreach ($file in $files)
    {
        $file = Split-Path $file -leaf
        Add-Version $file $hashtable
    }

    foreach ($key in Get-SourceProjectNames)
    {
        if ($key -eq "Silverback.Core.EFCore22")
        {
            continue
        }
        elseif ($key -eq "Silverback.Core.EFCore30")
        {
            $key = "Silverback.Core.EntityFrameworkCore"
        }

        Write-Host "[" -NoNewline
        Write-Host "$( $hashtable[$key].major ).$( $hashtable[$key].minor ).$( $hashtable[$key].patch )$( $hashtable[$key].suffix )" -NoNewline -ForegroundColor Green
        Write-Host "] $( $key )"
    }

    Write-Separator
}

function Add-Version([string]$path, [hashtable]$hashtable)
{
    $name = ""
    $major = 0
    $minor = 0
    $patch = 0
    $suffix = ""
    $versionTokenIndex = 0

    foreach ($token in $path.Replace("-", ".").Split("."))
    {
        if ($token -match "^\d+$" -And $versionTokenIndex -lt 3)
        {
            if ($versionTokenIndex -eq 0)
            {
                $major = [int]$token
            }
            elseif ($versionTokenIndex -eq 1)
            {
                $minor = [int]$token
            }
            elseif ($versionTokenIndex -eq 2)
            {
                $patch = [int]$token
            }

            $versionTokenIndex++
        }
        else
        {
            if ($versionTokenIndex -gt 0)
            {
                if ($token -eq "nupkg")
                {
                    break
                }
                elseif ($suffix.Length -eq 0)
                {
                    $suffix = "-" + $token
                }
                else
                {
                    $suffix = $suffix + "." + $token
                }
            }
            else
            {
                if ($name.Length -gt 0)
                {
                    $name += "."
                }

                $name += $token
            }
        }
    }

    if ( $hashtable.ContainsKey($name))
    {
        $previousVersion = $hashtable[$name]

        if ($previousVersion.major -gt $major -Or
                ($previousVersion.major -eq $major -And $previousVersion.minor -gt $minor) -Or
                ($previousVersion.major -eq $major -And $previousVersion.minor -eq $minor -And $previousVersion.patch -gt $patch) -Or
                ($previousVersion.major -eq $major -And $previousVersion.minor -eq $minor -And $previousVersion.patch -eq $patch -And $suffix -ne "" -And ($previousVersion.suffix -eq "" -Or $previousVersion.suffix -gt $suffix)))
        {
            return
        }
    }

    $hashtable[$name] = @{
        major = $major
        minor = $minor
        patch = $patch
        suffix = $suffix
    }
}

function Delete-Cache([string]$name)
{
    if ($global:clearCache -eq $false)
    {
        return
    }

    Write-Host "Clearing cache..." -ForegroundColor Yellow

    dotnet nuget locals all --clear

    Write-Separator
}

function Write-Separator()
{
    Write-Host "`n##################################################################`n" -ForegroundColor Yellow
}

$stopwatch = [system.diagnostics.stopwatch]::StartNew()

Check-Args $args
Check-Location
Delete-All
Delete-Cache
Pack-All
Show-Summary

$stopwatch.Stop()

Write-Host "Elapsed time $( $stopwatch.Elapsed ), finished at $((get-date).ToString("T") )"
Write-Host ""
