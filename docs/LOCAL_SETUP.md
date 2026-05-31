# Local Setup

## SQL Server with Docker

The local Docker SQL Server setup is aligned to the Docker connection that was previously used for Parlor development:

- Host: `localhost`
- Port: `1433`
- Database: `ParlorPredictionDb`
- User: `sa`

The database password must stay local and should not be committed.

1. Keep a local `.env` file at the repository root with:

```env
PARLOR_SQL_PORT=1433
PARLOR_SQL_DB=ParlorPredictionDb
PARLOR_SQL_SA_PASSWORD=<local-sa-password>
```

2. Start SQL Server:

```powershell
dotnet tool restore
docker compose up -d
docker ps
```

3. Point the app to Docker SQL Server with user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:ParlorPredictionDb" "Server=localhost,1433;Database=ParlorPredictionDb;User Id=sa;Password=<local-sa-password>;Encrypt=True;TrustServerCertificate=True" --project src/ParlorPrediction.Mvc
```

4. Keep Azure Blob Storage, JWT and SMTP secrets in user secrets as well:

```powershell
dotnet user-secrets set "ConnectionStrings:AzureStorage" "<azure-storage-connection-string>" --project src/ParlorPrediction.Mvc
dotnet user-secrets set "Jwt:Key" "<jwt-signing-key>" --project src/ParlorPrediction.Mvc
dotnet user-secrets set "Mail:Password" "<smtp-app-password>" --project src/ParlorPrediction.Mvc
dotnet user-secrets set "Mail:Username" "<smtp-username-if-different-from-from-address>" --project src/ParlorPrediction.Mvc
```

5. If you want the known local prep users, add their passwords through user secrets instead of committing them:

```powershell
dotnet user-secrets set "DevelopmentSeedUsers:Users:0:Password" "Polondrolo3*" --project src/ParlorPrediction.Mvc
dotnet user-secrets set "DevelopmentSeedUsers:Users:1:Password" "Polondrolo3*" --project src/ParlorPrediction.Mvc
dotnet user-secrets set "DevelopmentSeedUsers:Users:2:Password" "Polondrolo3*" --project src/ParlorPrediction.Mvc
```

6. Start the app:

```powershell
dotnet run --project src/ParlorPrediction.Mvc
```

## EF Core migrations

Use the local tool manifest already committed in the repo:

```powershell
dotnet tool restore
dotnet dotnet-ef database update --project src/ParlorPrediction.Persistence --startup-project src/ParlorPrediction.Mvc --context ParlorPredictionDbContext
```

In development, run the migration first. After that, the auth bootstrap will only seed roles and the initial manager when `BootstrapAdmin:Password` exists in user secrets.
