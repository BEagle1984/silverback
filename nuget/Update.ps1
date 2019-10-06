$repositoryLocation = "."
[bool]$global:clearCache = $FALSE
[bool]$global:build = $TRUE
$global:buildConfiguration = "Release"

# function Check-Location()
# {
#     [string]$currentLocation = Get-Location

#     if ($currentLocation -ne $repositoryLocation)
#     {
#         Write-Host "This script is supposed to run in $repositoryLocation!" -ForegroundColor Red
#         $choice = Read-Host "Wanna swith to $repositoryLocation ? [Y/n]"
#         if ($choice -ne "n")
#         {
#             cd $repositoryLocation
#         }
#     }
# }

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
    }
}

function Get-Sources() 
{
    $sources = 
        ("Silverback.Core", ("..\src\Silverback.Core\bin\$global:buildConfiguration")),
        ("Silverback.Core.EntityFrameworkCore", ("..\src\Silverback.Core.EFCore30\bin\$global:buildConfiguration", "..\src\Silverback.Core.EFCore22\bin\$global:buildConfiguration")),
        ("Silverback.Core.Rx", ("..\src\Silverback.Core.Rx\bin\$global:buildConfiguration")),
        ("Silverback.Core.Model", ("..\src\Silverback.Core.Model\bin\$global:buildConfiguration")),
        ("Silverback.Integration", ("..\src\Silverback.Integration\bin\$global:buildConfiguration")),
        ("Silverback.Integration.Kafka", ("..\src\Silverback.Integration.Kafka\bin\$global:buildConfiguration")),
        ("Silverback.Integration.InMemory", ("..\src\Silverback.Integration.InMemory\bin\$global:buildConfiguration")),
        ("Silverback.Integration.Configuration", ("..\src\Silverback.Integration.Configuration\bin\$global:buildConfiguration")),
        ("Silverback.EventSourcing", ("..\src\Silverback.EventSourcing\bin\$global:buildConfiguration"))

    return $sources
}

function Build()
{
    if ($global:build)
    {
        Write-Host "Building ($global:buildConfiguration)...`n" -ForegroundColor Yellow
        dotnet build -c $global:buildConfiguration -v q ../Silverback.sln
        Write-Host "" -ForegroundColor Yellow
    }
}

function Delete-All()
{
    Write-Host "Deleting everything in target folder..." -ForegroundColor Yellow -NoNewline

    Get-ChildItem -exclude Update.ps1 |
    Remove-Item -Force -Recurse |
    Out-Null

    Write-Host "OK" -ForegroundColor Green
}

function Copy-All()
{
    $destination = $repositoryLocation
    Ensure-Folder-Exists $destination

    Write-Host "Copying packages..." -ForegroundColor Yellow

    foreach ($source in Get-Sources)
    {
        $name = $source[0]

        Write-Host "`t$name..." -NoNewline

        foreach ($sourcePath in $source[1])
        {
            $sourcePath = Join-Path $sourcePath "*.nupkg"

            Copy-Item $sourcePath -Destination $destination -Recurse
        }

        Write-Host "OK" -ForegroundColor Green
    }

    if ($global:clearCache)
    {
        Delete-Cache $name
    }

    Write-Host "`nAvailable packages:" -ForegroundColor Yellow
    
    # Show-Files $destination
    Show-Summary $destination
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

function Show-Summary([string]$path)
{
    $hashtable = @{}

    $files = Get-ChildItem $path -Recurse -Filter *.nupkg

    foreach ($file in $files)
    {
        $file = Split-Path $file -leaf
        Add-Version $file $hashtable
    }

    foreach ($source in Get-Sources)
    {
        $key = $source[0]
        
        Write-Host "`t[" -NoNewline
        Write-Host "$($hashtable[$key].major).$($hashtable[$key].minor).$($hashtable[$key].patch)$($hashtable[$key].suffix)" -NoNewline -ForegroundColor Green
        Write-Host "] $($key)"
    }
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
            ($previousVersion.major -eq $major -And $previousVersion.minor -eq $minor -And $previousVersion.patch -eq $patch -And ($previousVersion.suffix -eq "" -Or $previousVersion.suffix -gt $suffix )))
        {
            return;
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
    Write-Host "`tClearing cache..."

    dotnet nuget locals all --clear
}

Check-Args $args
# Check-Location
Build
Delete-All
Copy-All
