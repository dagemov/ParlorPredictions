param(
    [ValidateSet("Funnel", "Serve", "None")]
    [string]$ShareMode = "Funnel",
    [string]$ApplicationUrl = "http://127.0.0.1:5212",
    [string]$LaunchProfile = "http",
    [string]$TailscaleExePath = "C:\Program Files\Tailscale\tailscale.exe"
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
}

function Wait-ForUrl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url,
        [int]$Attempts = 30,
        [int]$DelaySeconds = 2
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 10
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                return $true
            }
        }
        catch {
            if ($_.Exception.Response) {
                $statusCode = [int]$_.Exception.Response.StatusCode
                if ($statusCode -ge 200 -and $statusCode -lt 500) {
                    return $true
                }
            }
        }

        Start-Sleep -Seconds $DelaySeconds
    }

    return $false
}

function Test-ExistingProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PidFile
    )

    if (-not (Test-Path $PidFile)) {
        return $null
    }

    $existingPid = Get-Content $PidFile -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $existingPid) {
        return $null
    }

    return Get-Process -Id $existingPid -ErrorAction SilentlyContinue
}

function Get-ShareUrlFromStatus {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StatusText
    )

    $matches = [regex]::Matches($StatusText, 'https://[^\s]+')
    if ($matches.Count -eq 0) {
        return $null
    }

    return $matches[0].Value.TrimEnd('.')
}

$repoRoot = Get-RepoRoot
$mvcProjectPath = Join-Path $repoRoot "src\ParlorPrediction.Mvc\ParlorPrediction.Mvc.csproj"
$runtimeDirectory = Join-Path $repoRoot "artifacts\local-demo"
$pidFile = Join-Path $runtimeDirectory "parlorprediction-app.pid"
$stdoutLog = Join-Path $runtimeDirectory "parlorprediction-app.stdout.log"
$stderrLog = Join-Path $runtimeDirectory "parlorprediction-app.stderr.log"

New-Item -ItemType Directory -Force -Path $runtimeDirectory | Out-Null

if (-not (Test-Path $mvcProjectPath)) {
    throw "The MVC project was not found at '$mvcProjectPath'."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET SDK is not available in PATH."
}

$appAlreadyResponding = Wait-ForUrl -Url $ApplicationUrl -Attempts 1 -DelaySeconds 0
$existingProcess = Test-ExistingProcess -PidFile $pidFile

if ($appAlreadyResponding) {
    Write-Host "ParlorPrediction is already responding at $ApplicationUrl." -ForegroundColor Yellow
}
elseif ($existingProcess) {
    Write-Host "ParlorPrediction is already running with PID $($existingProcess.Id)." -ForegroundColor Yellow
}
else {
    Remove-Item $stdoutLog, $stderrLog -ErrorAction SilentlyContinue

    $startArguments = @(
        "run"
        "--project"
        $mvcProjectPath
        "--launch-profile"
        $LaunchProfile
    )

    if (-not [string]::IsNullOrWhiteSpace($ApplicationUrl)) {
        $startArguments += @("--urls", $ApplicationUrl)
    }

    $process = Start-Process -FilePath "dotnet" `
        -ArgumentList $startArguments `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $stdoutLog `
        -RedirectStandardError $stderrLog `
        -PassThru

    Set-Content -Path $pidFile -Value $process.Id -NoNewline
    Write-Host "Started ParlorPrediction with PID $($process.Id)." -ForegroundColor Green
}

if (-not (Wait-ForUrl -Url $ApplicationUrl)) {
    Write-Host "The app did not become ready at $ApplicationUrl." -ForegroundColor Red
    if (Test-Path $stderrLog) {
        Write-Host "`nLast stderr output:" -ForegroundColor Yellow
        Get-Content $stderrLog | Select-Object -Last 40
    }
    exit 1
}

Write-Host "ParlorPrediction is responding at $ApplicationUrl." -ForegroundColor Green

if ($ShareMode -eq "None") {
    Write-Host "Share mode is None. No Tailscale command was executed." -ForegroundColor Cyan
    exit 0
}

if (-not (Test-Path $TailscaleExePath)) {
    throw "Tailscale was not found at '$TailscaleExePath'."
}

$shareVerb = if ($ShareMode -eq "Funnel") { "funnel" } else { "serve" }
$shareArguments = @(
    $shareVerb
    "--bg"
    $ApplicationUrl
)

try {
    & $TailscaleExePath "serve" "--https=443" "off" | Out-Null
}
catch {
}

try {
    & $TailscaleExePath "funnel" "--https=443" "off" | Out-Null
}
catch {
}

& $TailscaleExePath @shareArguments | Out-Null

Write-Host ""
$shareStatus = (& $TailscaleExePath $shareVerb "status" | Out-String).Trim()
$shareUrl = Get-ShareUrlFromStatus -StatusText $shareStatus

Write-Host $shareStatus

if ($shareUrl) {
    Write-Host ""
    Write-Host "Share URL: $shareUrl" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "The app is shared, but the public URL could not be parsed automatically. Review the status output above." -ForegroundColor Yellow
}
