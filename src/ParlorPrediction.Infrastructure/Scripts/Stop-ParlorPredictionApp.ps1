param(
    [string]$ApplicationHint = "ParlorPrediction.Mvc"
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
}

$repoRoot = Get-RepoRoot
$runtimeDirectory = Join-Path $repoRoot "artifacts\local-demo"
$pidFile = Join-Path $runtimeDirectory "parlorprediction-app.pid"

$stopped = $false

if (Test-Path $pidFile) {
    $pidValue = Get-Content $pidFile -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($pidValue) {
        $process = Get-Process -Id $pidValue -ErrorAction SilentlyContinue
        if ($process) {
            Stop-Process -Id $process.Id -Force
            Write-Host "Stopped ParlorPrediction process PID $($process.Id)." -ForegroundColor Green
            $stopped = $true
        }
    }

    Remove-Item $pidFile -ErrorAction SilentlyContinue
}

if (-not $stopped) {
    $candidateProcesses = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
        Where-Object { $_.CommandLine -like "*$ApplicationHint*" }

    foreach ($candidate in $candidateProcesses) {
        Stop-Process -Id $candidate.ProcessId -Force -ErrorAction SilentlyContinue
        Write-Host "Stopped fallback process PID $($candidate.ProcessId)." -ForegroundColor Yellow
        $stopped = $true
    }
}

if (-not $stopped) {
    Write-Host "No running ParlorPrediction app process was found." -ForegroundColor Yellow
}
