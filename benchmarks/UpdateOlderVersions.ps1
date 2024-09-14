$versions = @("4.5.1")
$rootFolder = Get-Location
$sourceFolder = Join-Path -Path $rootFolder -ChildPath "LatestVersionBenchmarks"
$sourceCsprojName = "LatestVersionBenchmarks.csproj"

foreach ($version in $versions)
{
    $versionFormatted = "V" + ($version -replace "\.", "")
    $targetFolder = Join-Path -Path $rootFolder -ChildPath "$versionFormatted`Benchmark"
    
    if (Test-Path -Path $targetFolder)
    {
        Write-Host "Cleaning up existing folder: $targetFolder"
        Remove-Item -Path $targetFolder -Recurse -Force
    }

    # Copy the project to the new folder
    Write-Host "Copying project to $targetFolder"
    Copy-Item -Path $sourceFolder -Destination $targetFolder -Recurse -Force

    $targetCsprojPath = Join-Path -Path $targetFolder -ChildPath $sourceCsprojName

    if (-Not (Test-Path -Path $targetCsprojPath)) {
        Write-Host "No project file named $sourceCsprojName found in $targetFolder"
        continue
    }

    # Rename the .csproj file to match the folder name
    $newCsprojName = "$versionFormatted" + "Benchmark.csproj"
    $newCsprojPath = Join-Path -Path $targetFolder -ChildPath $newCsprojName
    Rename-Item -Path $targetCsprojPath -NewName $newCsprojName
    Write-Host "Renamed .csproj file to: $newCsprojName"

    # Update the Silverback package references in the new project file
    $csprojContent = Get-Content $newCsprojPath -Raw
    $updatedContent = $csprojContent -replace "\$\(BaseVersion\)", $version
    Set-Content -Path $newCsprojPath -Value $updatedContent

    Write-Host "Project copied and Silverback packages updated to version $version."
}
