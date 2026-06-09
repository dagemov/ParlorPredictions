# ParlorPrediction Local Demo Scripts

These scripts help you run the real ParlorPrediction app locally and share it through Tailscale without touching Azure App Service or Azure SQL.

## Script location

```text
src/ParlorPrediction.Infrastructure/Scripts/
```

## Scripts included

### Start-ParlorPredictionDemo.ps1

Starts the ASP.NET Core MVC app locally and then enables Tailscale sharing.

Default behavior:

- App URL: `http://127.0.0.1:5212`
- Launch profile: `http`
- Share mode: `Funnel`
- App logs: `artifacts/local-demo/`

Order of operations:

1. Start the local MVC app.
2. Wait until the local app responds.
3. Enable Tailscale `Funnel` or `Serve`.
4. Print the share URL when Tailscale exposes one.

Example:

```powershell
powershell -ExecutionPolicy Bypass -File .\src\ParlorPrediction.Infrastructure\Scripts\Start-ParlorPredictionDemo.ps1
```

Tailnet-only access:

```powershell
powershell -ExecutionPolicy Bypass -File .\src\ParlorPrediction.Infrastructure\Scripts\Start-ParlorPredictionDemo.ps1 -ShareMode Serve
```

Local app only:

```powershell
powershell -ExecutionPolicy Bypass -File .\src\ParlorPrediction.Infrastructure\Scripts\Start-ParlorPredictionDemo.ps1 -ShareMode None
```

### Stop-ParlorPredictionApp.ps1

Stops the local ASP.NET Core app process that was started by the start script.

```powershell
powershell -ExecutionPolicy Bypass -File .\src\ParlorPrediction.Infrastructure\Scripts\Stop-ParlorPredictionApp.ps1
```

### Stop-ParlorPredictionShare.ps1

Turns off Tailscale Funnel and Serve for the default HTTPS endpoint.

```powershell
powershell -ExecutionPolicy Bypass -File .\src\ParlorPrediction.Infrastructure\Scripts\Stop-ParlorPredictionShare.ps1
```

## Important commands to learn

### User secrets

List secrets:

```powershell
dotnet user-secrets list --project src/ParlorPrediction.Mvc
```

Set a secret:

```powershell
dotnet user-secrets set "Jwt:Key" "<local-jwt-key>" --project src/ParlorPrediction.Mvc
```

Remove a secret:

```powershell
dotnet user-secrets remove "Jwt:Key" --project src/ParlorPrediction.Mvc
```

Clear all secrets:

```powershell
dotnet user-secrets clear --project src/ParlorPrediction.Mvc
```

### EF Core

Restore local tools:

```powershell
dotnet tool restore
```

Apply the current migrations locally:

```powershell
dotnet dotnet-ef database update --project src/ParlorPrediction.Persistence --startup-project src/ParlorPrediction.Mvc --context ParlorPredictionDbContext
```

### Docker SQL Server

Start SQL Server:

```powershell
docker compose up -d
docker ps
```

Stop SQL Server:

```powershell
docker compose down
```

### Tailscale

Check status:

```powershell
& "C:\Program Files\Tailscale\tailscale.exe" status
```

Share inside your tailnet:

```powershell
& "C:\Program Files\Tailscale\tailscale.exe" serve --bg http://127.0.0.1:5212
```

Share publicly:

```powershell
& "C:\Program Files\Tailscale\tailscale.exe" funnel --bg http://127.0.0.1:5212
```

Check Funnel:

```powershell
& "C:\Program Files\Tailscale\tailscale.exe" funnel status
```

Turn off sharing:

```powershell
& "C:\Program Files\Tailscale\tailscale.exe" funnel --https=443 off
& "C:\Program Files\Tailscale\tailscale.exe" serve --https=443 off
```

## Notes

- Keep the local app running while your demo link is in use.
- Funnel can only proxy a working local app.
- These scripts do not replace Azure deployment. They are a low-cost development and demo workflow.
