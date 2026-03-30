# Remove all except .git, CNAME, and this script
$CurrentScript = $MyInvocation.MyCommand.Name
Get-ChildItem -Force | Where-Object {
    $_.Name -ne '.git' -and $_.Name -ne 'CNAME' -and $_.Name -ne $CurrentScript
} | ForEach-Object {
    if ($_.PSIsContainer) {
        Remove-Item $_.FullName -Recurse -Force
    } else {
        Remove-Item $_.FullName -Force
    }
}

# Copy new site contents from ../silverback/docs/_site/
$Source = Join-Path $PSScriptRoot '..\silverback\docs\_site'
Copy-Item -Path $Source\* -Destination $PSScriptRoot -Recurse -Force

