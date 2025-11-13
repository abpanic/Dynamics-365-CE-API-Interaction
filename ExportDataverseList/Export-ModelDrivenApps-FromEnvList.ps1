<#
.SYNOPSIS
  Reads the Dataverse environments CSV from Script 1, iterates through EACH environment,
  connects (locked to that environment URL), and exports model-driven apps (appmodule)
  to per-env CSV files. Also flags which environments contain the
  "Deployment Pipeline Configuration" app (AppDeploymentConfiguration).

.PARAMETER EnvListPath
  Path to the CSV produced by Export-DataverseEnvironments.ps1

.OUTPUT
  Per-env: C:\Temp\ModelDrivenApps\<EnvName>_ModelDrivenApps_<timestamp>.csv
  Summary: C:\Temp\ModelDrivenApps\DeploymentPipelineConfiguration_Envs_<timestamp>.csv
#>

param(
    [string]$EnvListPath = "C:\Temp\DataverseEnvironments_20251113_211849.csv"  # <-- change to your actual file
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $EnvListPath)) {
    Write-Host "CSV file not found: $EnvListPath" -ForegroundColor Red
    exit 1
}

Write-Host "Loading environments from CSV: $EnvListPath" -ForegroundColor Cyan
$dataverseEnvs = Import-Csv -Path $EnvListPath

if (-not $dataverseEnvs -or $dataverseEnvs.Count -eq 0) {
    Write-Host "No rows found in CSV." -ForegroundColor Red
    exit 1
}

# ==========================================
# 1) Ensure XRM modules for Dataverse
# ==========================================
Write-Host "`nChecking XRM modules..." -ForegroundColor Cyan

[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

if (-not (Get-Module -ListAvailable -Name Microsoft.Xrm.Tooling.CrmConnector.PowerShell)) {
    Write-Host "Installing Microsoft.Xrm.Tooling.CrmConnector.PowerShell..." -ForegroundColor Yellow
    Install-Module Microsoft.Xrm.Tooling.CrmConnector.PowerShell -Scope CurrentUser -Force -AllowClobber
}
if (-not (Get-Module -ListAvailable -Name Microsoft.Xrm.Data.PowerShell)) {
    Write-Host "Installing Microsoft.Xrm.Data.PowerShell..." -ForegroundColor Yellow
    Install-Module Microsoft.Xrm.Data.PowerShell -Scope CurrentUser -Force -AllowClobber
}

Import-Module Microsoft.Xrm.Tooling.CrmConnector.PowerShell -ErrorAction Stop
Import-Module Microsoft.Xrm.Data.PowerShell -ErrorAction Stop

# ==========================================
# 2) Prepare output folder
# ==========================================
if (-not (Test-Path "C:\Temp")) {
    New-Item -Path "C:\Temp" -ItemType Directory | Out-Null
}

$rootOut = "C:\Temp\ModelDrivenApps"
if (-not (Test-Path $rootOut)) {
    New-Item -Path $rootOut -ItemType Directory | Out-Null
}

# Will hold environments that have the Deployment Pipeline app
$pipelineMatches = @()

# ==========================================
# 3) FetchXML for model-driven apps (AppModule)
# ==========================================
$fetch = @"
<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='appmodule'>
    <attribute name='appmoduleid' />
    <attribute name='name' />
    <attribute name='uniquename' />
    <attribute name='clienttype' />
  </entity>
</fetch>
"@

# Target app info
$targetName       = "Deployment Pipeline Configuration"
$targetUniqueName = "AppDeploymentConfiguration"

# ==========================================
# 4) Iterate each environment
# ==========================================
foreach ($env in $dataverseEnvs) {
    $displayName = $env.DisplayName
    $envName     = $env.EnvironmentName
    $instanceUrl = $env.InstanceUrl.TrimEnd('/')

    Write-Host "`n==================================================" -ForegroundColor DarkCyan
    Write-Host "Environment: $displayName" -ForegroundColor Yellow
    Write-Host "Env Name   : $envName" -ForegroundColor Yellow
    Write-Host "InstanceUrl: $instanceUrl" -ForegroundColor Yellow

    # ---- 4.1 Build connection string locked to this URL ----
    $connString = @"
AuthType=OAuth;
Url=$instanceUrl;
AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;
RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;
LoginPrompt=Auto
"@ -replace "`r?`n", ""

    Write-Host "Connecting to this org via connection string..." -ForegroundColor DarkYellow

    $crmConn = $null
    try {
        $crmConn = Get-CrmConnection -ConnectionString $connString
    } catch {
        Write-Host "Connection failed for '$displayName': $($_.Exception.Message)" -ForegroundColor Red
        continue
    }

    if (-not $crmConn) {
        Write-Host "No connection object returned; skipping '$displayName'." -ForegroundColor Red
        continue
    }

    Write-Host "Connected to: $($crmConn.ConnectedOrgFriendlyName) ($($crmConn.ConnectedOrgUniqueName))" -ForegroundColor Green
    Write-Host "Org URL     : $($crmConn.ConnectedOrgUriActual)" -ForegroundColor Green

    # ---- 4.2 Fetch appmodule rows ----
    $rawResult = $null
    $records   = $null
    try {
        $rawResult = Get-CrmRecordsByFetch -conn $crmConn -Fetch $fetch
        $records   = $rawResult.CrmRecords
    } catch {
        Write-Host "Fetch failed for '$displayName': $($_.Exception.Message)" -ForegroundColor Red
        continue
    }

    if (-not $records -or $records.Count -eq 0) {
        Write-Host "No appmodule rows found in this org." -ForegroundColor Yellow
        continue
    }

    # ---- 4.3 Does this env have Deployment Pipeline Configuration? ----
    # Force name & uniquename to string before comparison
    $dpApps = $records | Where-Object {
        ($_.name        -as [string]) -eq $targetName       -and
        ($_.uniquename  -as [string]) -eq $targetUniqueName
    }

    $hasDP = ($dpApps | Measure-Object).Count -gt 0

    if ($hasDP) {
        Write-Host ">>> This environment HAS the Deployment Pipeline Configuration app." -ForegroundColor Cyan

        # Add to summary list
        $pipelineMatches += [pscustomobject]@{
            EnvironmentDisplay = $displayName
            EnvironmentName    = $envName
            InstanceUrl        = $instanceUrl
            AppName            = $targetName
            UniqueName         = $targetUniqueName
        }
    } else {
        Write-Host "This environment does NOT have the Deployment Pipeline Configuration app." -ForegroundColor DarkGray
    }


    # ---- 4.4 Export per-env CSV, include a flag column ----
    $stamp   = Get-Date -Format "yyyyMMdd_HHmmss"
    $safeEnv = ($envName -replace '[^a-zA-Z0-9_-]', '_')
    $outPath = Join-Path $rootOut ("{0}_ModelDrivenApps_{1}.csv" -f $safeEnv, $stamp)

    $records |
        Select-Object `
            @{Name = 'EnvironmentDisplay'; Expression = { $displayName }}, `
            @{Name = 'EnvironmentName'   ; Expression = { $envName }}, `
            @{Name = 'InstanceUrl'       ; Expression = { $instanceUrl }}, `
            @{Name = 'HasDeploymentPipelineConfiguration'; Expression = { $hasDP }}, `
            name, uniquename, clienttype, lastmodifiedon |
        Sort-Object name |
        Export-Csv -Path $outPath -NoTypeInformation

    Write-Host "Exported $($records.Count) model-driven apps for env '$displayName' to:" -ForegroundColor Green
    Write-Host "  $outPath" -ForegroundColor Cyan
}

# ==========================================
# 5) Summary of environments that have the app
# ==========================================
if ($pipelineMatches.Count -gt 0) {
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $summaryPath = Join-Path $rootOut ("DeploymentPipelineConfiguration_Envs_{0}.csv" -f $stamp)

    $pipelineMatches |
        Export-Csv -Path $summaryPath -NoTypeInformation

    Write-Host "`nEnvironments *with* 'Deployment Pipeline Configuration' app:" -ForegroundColor Green
    $pipelineMatches |
        Select-Object EnvironmentDisplay, InstanceUrl |
        Format-Table -AutoSize

    Write-Host "`nSummary written to:" -ForegroundColor Green
    Write-Host "  $summaryPath" -ForegroundColor Cyan
} else {
    Write-Host "`nNo environments in this CSV contain the 'Deployment Pipeline Configuration' app." -ForegroundColor Yellow
}

Write-Host "`nDone processing all environments from CSV." -ForegroundColor Cyan
