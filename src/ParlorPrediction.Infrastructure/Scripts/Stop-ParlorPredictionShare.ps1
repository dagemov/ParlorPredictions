param(
    [string]$TailscaleExePath = "C:\Program Files\Tailscale\tailscale.exe",
    [int]$HttpsPort = 443
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $TailscaleExePath)) {
    throw "Tailscale was not found at '$TailscaleExePath'."
}

& $TailscaleExePath "funnel" "--https=$HttpsPort" "off" | Out-Null
& $TailscaleExePath "serve" "--https=$HttpsPort" "off" | Out-Null

Write-Host "Tailscale Funnel and Serve were turned off for HTTPS port $HttpsPort." -ForegroundColor Green
Write-Host ""
& $TailscaleExePath "funnel" "status"
Write-Host ""
& $TailscaleExePath "serve" "status"
