$repositoryLocation = "c:\source\silverback\nuget"
$cacheLocation = Join-Path $env:USERPROFILE ".nuget/packages"
[bool]$clearCache = $FALSE

foreach ($arg in $args)
{
	if ($arg -eq "-c")
	{
		$clearCache = $TRUE
	}
}

$sources =
    ("Silverback.Core", "..\silverback-core\src\Silverback.Core\bin\Debug"),
    ("Silverback.Core.EntityFrameworkCore", "..\silverback-core\src\Silverback.Core.EntityFrameworkCore\bin\Debug"),
    ("Silverback.Core.Rx", "..\silverback-core\src\Silverback.Core.Rx\bin\Debug"),
	("Silverback.Integration", "..\silverback-integration\src\Silverback.Integration\bin\Debug"),
    ("Silverback.Integration.EntityFrameworkCore", "..\silverback-integration\src\Silverback.Integration.EntityFrameworkCore\bin\Debug"),
    ("Silverback.Integration.FileSystem", "..\silverback-testing\src\Silverback.Integration.FileSystem\bin\Debug"),
    ("Silverback.Integration.Kafka", "..\silverback-integration-kafka\src\Silverback.Integration.Kafka\bin\Debug")

function Check-Location()
{
    [string]$currentLocation = Get-Location

    if ($currentLocation -ne $repositoryLocation)
    {
        Write-Host "This script is supposed to run in $repositoryLocation!" -ForegroundColor Red
        $choice = Read-Host "Wanna swith to $repositoryLocation ? [Y/n]"
        if ($choice -ne "n")
        {
            cd $repositoryLocation
        }
    }
}

function Delete-All()
{
    Write-Host "Deleting everything in target folder..." -NoNewline

    Get-ChildItem -exclude Update.ps1 |
    Remove-Item -Force -Recurse |
    Out-Null

    Write-Host "OK" -ForegroundColor Green
}

function Copy-All()
{
    foreach ($source in $sources)
    {
        $name = $source[0]
        $sourcePath = Join-Path $source[1] "\*"

        Write-Host "$name" -ForegroundColor Yellow

       Copy-Package $name $sourcePath

		if ($clearCache)
		{
        	Delete-Cache $name
		}
    }
}

function Copy-Package([string]$name, [string]$sourcePath)
{
    Write-Host "`tCopying..." -NoNewline

    $destination = Join-Path $repositoryLocation $name

    Ensure-Folder-Exists $destination

    Copy-Item $sourcePath -Destination $destination -Recurse

    Write-Host "OK" -ForegroundColor Green

    Show-Versions $destination $name
}

function Ensure-Folder-Exists([string]$path)
{
    if(!(Test-Path $path))
    {
        New-Item -ItemType Directory -Force -Path $path | Out-Null
    }
}

function Show-Versions([string]$path, [string]$packageName)
{
    Get-ChildItem $path -Recurse -Filter *.nupkg | 
    Foreach-Object {
        Write-Host `t $_.Name.Substring($packageName.Length + 1, $_.Name.Length - $packageName.Length - 7) -ForegroundColor DarkGray
    }
}

function Delete-Cache([string]$name)
{
    Write-Host "`tClearing cache..." -NoNewline

    $cache = Join-Path $cacheLocation $name

    Get-ChildItem $cache -Recurse |
    Remove-Item -Force -Recurse |
    Out-Null

    Write-Host "OK" -ForegroundColor Green
}

Check-Location
Delete-All
Copy-All
