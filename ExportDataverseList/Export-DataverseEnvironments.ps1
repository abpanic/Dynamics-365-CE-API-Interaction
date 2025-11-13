<#
.SYNOPSIS
  Lists all Dataverse environments (those with instanceUrl) using the
  PowerApps Admin module and exports them to CSV.

.OUTPUT
  C:\Temp\DataverseEnvironments_<timestamp>.csv

  Columns:
    - DisplayName
    - EnvironmentName
    - InstanceUrl
#>

$ErrorActionPreference = "Stop"

Write-Host "Importing admin module..." -ForegroundColor Cyan
Import-Module Microsoft.PowerApps.Administration.PowerShell -ErrorAction Stop

Write-Host "Signing in to Power Platform..." -ForegroundColor Cyan
Add-PowerAppsAccount | Out-Null

Write-Host "Retrieving environments..." -ForegroundColor Cyan
$envs = Get-AdminPowerAppEnvironment

# Filter to Dataverse environments (those with linkedEnvironmentMetadata.instanceUrl)
$dataverseEnvs = @()

foreach ($e in $envs) {
    $props = $e.Internal.properties
    if ($props -and $props.linkedEnvironmentMetadata -and $props.linkedEnvironmentMetadata.instanceUrl) {
        $lem = $props.linkedEnvironmentMetadata
        $dataverseEnvs += [pscustomobject]@{
            DisplayName      = $e.DisplayName
            EnvironmentName  = $e.EnvironmentName
            InstanceUrl      = $lem.instanceUrl
        }
    }
}

if (-not $dataverseEnvs -or $dataverseEnvs.Count -eq 0) {
    Write-Host "No Dataverse environments with instanceUrl were found." -ForegroundColor Red
    Write-Host "Tip: Run 'Get-AdminPowerAppEnvironment | Select-Object -First 1 | ConvertTo-Json -Depth 5' to inspect object shape." -ForegroundColor Yellow
    exit 1
}

Write-Host "`nDataverse environments (with DB):" -ForegroundColor Cyan
$dataverseEnvs | Format-Table -AutoSize

if (-not (Test-Path "C:\Temp")) {
    New-Item -Path "C:\Temp" -ItemType Directory | Out-Null
}

$stamp   = Get-Date -Format "yyyyMMdd_HHmmss"
$outPath = "C:\Temp\DataverseEnvironments_$stamp.csv"

$dataverseEnvs | Export-Csv -Path $outPath -NoTypeInformation

Write-Host "`nExported Dataverse environments to:`n$outPath" -ForegroundColor Green
