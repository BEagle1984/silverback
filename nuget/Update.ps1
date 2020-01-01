$repositoryLocation = "."
[bool]$global:clearCache = $FALSE
[bool]$global:build = $TRUE
[bool]$global:buildSolution = $FALSE
[bool]$global:restorePackagesAfterwards = $TRUE
$global:buildConfiguration = "Release"

function Check-Location()
{
    [string]$currentLocation = Get-Location

    if (-Not (Test-Path "./Update.ps1"))
    {
        Write-Host "This script is supposed to run in the /nuget folder of the main Silverback repository!" -ForegroundColor Red
        Exit
    }
}

function Check-Args([string[]]$argsArray)
{
    For ($i=0; $i -le $argsArray.Length; $i++) {
        $arg = $argsArray[$i]

        if ($arg -eq "--configuration" -Or $arg -eq "-c")
        {
           $global:buildConfiguration = $argsArray[$i + 1]
        }
        elseif ($arg -eq "--clear-cache" -Or $arg -eq "-l")
        {
           $global:clearCache = $TRUE
        }
        elseif ($arg -eq "--no-build")
        {
           $global:build = $FALSE
        }
        elseif ($arg -eq "--build-solution" -Or $arg -eq "-s")
        {
           $global:buildSolution = $TRUE
        }
        elseif ($arg -eq "--no-restore")
        {
            $global:restorePackagesAfterwards = $FALSE
        }
    }
}

function Get-Sources() 
{
    $sources = 
        ("Silverback.Core", ("..\src\Silverback.Core\")),
        ("Silverback.Core.EntityFrameworkCore", ("..\src\Silverback.Core.EFCore30\", "..\src\Silverback.Core.EFCore22\")),
        ("Silverback.Core.Model", ("..\src\Silverback.Core.Model\")),
        ("Silverback.Core.Rx", ("..\src\Silverback.Core.Rx\")),
        ("Silverback.EventSourcing", ("..\src\Silverback.EventSourcing\")),
        ("Silverback.Integration", ("..\src\Silverback.Integration\")),
        ("Silverback.Integration.Configuration", ("..\src\Silverback.Integration.Configuration\")),
        ("Silverback.Integration.HealthChecks", ("..\src\Silverback.Integration.HealthChecks\")),
        ("Silverback.Integration.InMemory", ("..\src\Silverback.Integration.InMemory\")),
        ("Silverback.Integration.Kafka", ("..\src\Silverback.Integration.Kafka\")),
        ("Silverback.Integration.RabbitMQ", ("..\src\Silverback.Integration.RabbitMQ\"))

    return $sources
}

function Build()
{
    if ($global:build)
    {
        if ($global:buildSolution)
        {
            Write-Host "Building ($global:buildConfiguration)...`n" -ForegroundColor Yellow
            dotnet build -c $global:buildConfiguration -v q ../Silverback.sln
            Write-Host "" -ForegroundColor Yellow
        }
        else
        {
            foreach ($source in Get-Sources)
            {
                $name = $source[0]

                Write-Host "Building $name ($global:buildConfiguration)...`n" -ForegroundColor Yellow

                foreach ($sourcePath in $source[1])
                {
                    dotnet build -c $global:buildConfiguration -v q $sourcePath/.
                }

                Write-Host ""

                Copy-Package $source

                Write-Separator
            }
        }
    }
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

function Copy-Package($source) 
{
    $name = $source[0]

    Write-Host "Copying $name..." -NoNewline

    foreach ($sourcePath in $source[1])
    {
        $sourcePath = Join-Path $sourcePath "bin\$global:buildConfiguration\*.nupkg"

        Copy-Item $sourcePath -Destination $repositoryLocation -Recurse
    }

    Write-Host "OK" -ForegroundColor Green    
}

function Copy-All()
{
    if ($global:buildSolution -eq $false)
    {   
       return
    }

    foreach ($source in Get-Sources)
    {
        Copy-Package($source)
    }
}

function Ensure-Folder-Exists([string]$path)
{
    if (!(Test-Path $path))
    {
        New-Item -ItemType Directory -Force -Path $path | Out-Null
    }
}

function Show-Files([string]$path)
{
    Get-ChildItem $path -Recurse -Filter *.nupkg | 
    Foreach-Object {
        Write-Host "`t" -NoNewline
        Write-Host $_.Name.Substring(0, $_.Name.Length - ".nupkg".Length)
    }
}

function Show-Summary()
{
    Write-Host "Available packages:`n" -ForegroundColor Yellow
    
    $hashtable = @{}

    $files = Get-ChildItem $repositoryLocation -Recurse -Filter *.nupkg

    foreach ($file in $files)
    {
        $file = Split-Path $file -leaf
        Add-Version $file $hashtable
    }

    foreach ($source in Get-Sources)
    {
        $key = $source[0]
        
        Write-Host "[" -NoNewline
        Write-Host "$($hashtable[$key].major).$($hashtable[$key].minor).$($hashtable[$key].patch)$($hashtable[$key].suffix)" -NoNewline -ForegroundColor Green
        Write-Host "] $($key)"
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
        if ($token -match "^\d+$")
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
            if ($versionTokenIndex -gt 0 -And $token -ne "nupkg")
            {
                $suffix = "-" + $token
                break
            }

            if ($token -eq "nupkg")
            {
                break
            }

            if ($name.Length -gt 0)
            {
                $name += "."
            }

            $name += $token
        }
    }

    if ($hashtable.ContainsKey($name))
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

function Restore()
{
    if ($global:restorePackagesAfterwards -eq $false)
    {
        return
    }

    Write-Host "Restoring nuget packages in Silverback.sln..." -ForegroundColor Yellow

    dotnet restore ../Silverback.sln

    Write-Separator
}

function Write-Separator()
{
    Write-Host "`n---------------------------------------------------------------`n" -ForegroundColor Yellow
}

Write-Separator

Check-Args $args
Check-Location
Delete-All
Delete-Cache
Build
Copy-All
Restore
Show-Summary
