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
	("Silverback.Integration", "..\silverback-integration\src\Silverback.Integration\bin\Debug"),
	("Silverback.Integration.FileSystem", "..\silverback-testing\src\Silverback.Integration.FileSystem\bin\Debug")

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
        $sourcePath = $source[1]

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

    copy $sourcePath -Destination $destination -Recurse

    Write-Host "OK" -ForegroundColor Green
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
