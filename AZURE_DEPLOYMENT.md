# Azure Deployment

This guide publishes the current prototype without adding new business features. The repo is prepared for Azure App Service, Azure SQL Database, Azure Blob Storage, and a basic GitHub Actions deployment workflow.

## Required Azure resources

Create these resources in the same Azure region when possible:

1. Resource group
2. Azure App Service plan
3. Azure App Service web app for `ParlorPrediction.Mvc`
4. Azure SQL logical server
5. Azure SQL database
6. Azure Storage account with Blob Storage enabled

Recommended hosting path:

- Publish type: `Code`
- Runtime stack: `.NET 10`
- Operating system: `Windows`

Windows App Service keeps the publish-profile flow straightforward for this repo and matches the GitHub Actions workflow included here.

## App Service configuration

Set these App Settings in Azure App Service:

### Required

- `ConnectionStrings__ParlorPredictionDb`
- `ConnectionStrings__AzureStorage`
- `Jwt__Key`
- `Mail__Host`
- `Mail__Port`
- `Mail__Username`
- `Mail__Password`
- `Mail__From`
- `Frontend__BaseUrl`
- `Ai__Provider`

### Strongly recommended for this app

- `Mail__FromName`
- `Mail__BrandName`
- `Mail__ConfirmationSubject`
- `Mail__ResetPasswordSubject`
- `BootstrapAdmin__Password`

### Optional overrides

- `Frontend__ConfirmEmailPath`
- `Frontend__ResetPasswordPath`
- `TemplatePaths__EmailConfirmation`
- `TemplatePaths__PasswordReset`
- `BootstrapAdmin__FirstName`
- `BootstrapAdmin__LastName`
- `BootstrapAdmin__UserName`
- `BootstrapAdmin__Email`
- `BootstrapAdmin__PhoneNumber`
- `BootstrapAdmin__Role`

Notes:

- `Ai__Provider` should stay `Deterministic` for the current production prototype.
- `BootstrapAdmin__Password` is the only bootstrap secret required to create the first manager account automatically on startup.
- The default bootstrap manager profile already points to `dagemov@gmail.com` in the app configuration. Override it only if you need a different first manager.

## Azure SQL Database

Create the Azure SQL logical server and database first, then build the production connection string and store it in:

- App Service setting `ConnectionStrings__ParlorPredictionDb`
- GitHub Actions secret `AZURE_SQL_CONNECTIONSTRING` if you want migrations to run automatically in CI

Example shape:

```text
Server=tcp:<sql-server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<sql-admin-user>;Password=<sql-admin-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## EF Core migrations in production

This repo includes formal migrations. You have two safe ways to apply them.

### Option 1: Manual migration from a trusted machine

Use a secure terminal session and inject the production connection string only for that process:

```powershell
$env:ConnectionStrings__ParlorPredictionDb = "<azure-sql-connection-string>"
dotnet tool restore
dotnet dotnet-ef database update --project src/ParlorPrediction.Persistence --startup-project src/ParlorPrediction.Mvc --context ParlorPredictionDbContext
Remove-Item Env:\ConnectionStrings__ParlorPredictionDb
```

### Option 2: CI-managed migration

Set the GitHub secret:

- `AZURE_SQL_CONNECTIONSTRING`

The workflow will run the same `dotnet dotnet-ef database update` command before deployment when that secret exists. If the secret is missing, the workflow skips migrations and still deploys the web app package.

## Azure Blob Storage

Store the blob connection string in:

- `ConnectionStrings__AzureStorage`

Current container usage in the app:

- `images`
- `documents`

Before production smoke testing:

1. Confirm the storage account exists.
2. Confirm the connection string points to the correct account.
3. Upload one file through `api/files`.
4. Delete the same file through `api/files`.

The app creates missing containers on demand.

## GitHub Actions

This repo includes `.github/workflows/azure-deploy.yml`.

### Required GitHub secret

- `AZURE_WEBAPP_PUBLISH_PROFILE`

### Optional GitHub secret

- `AZURE_SQL_CONNECTIONSTRING`

### Required GitHub repository variable

- `AZURE_WEBAPP_NAME`

The workflow:

1. Restores tools and NuGet packages.
2. Builds the solution in `Release`.
3. Publishes `ParlorPrediction.Mvc`.
4. Optionally runs EF Core migrations when `AZURE_SQL_CONNECTIONSTRING` exists.
5. Deploys the published output to Azure App Service.

## Deployment order

1. Provision Azure SQL Database.
2. Provision the storage account.
3. Provision the App Service web app and set all required app settings.
4. Add the GitHub secret `AZURE_WEBAPP_PUBLISH_PROFILE`.
5. Add the GitHub variable `AZURE_WEBAPP_NAME`.
6. Optionally add `AZURE_SQL_CONNECTIONSTRING`.
7. Push to `main` or run the workflow manually with `workflow_dispatch`.
8. Confirm the first startup creates built-in roles and the bootstrap manager when `BootstrapAdmin__Password` exists.

## Rollback

Use the smallest rollback that fixes the incident:

1. Re-run the workflow from the last known good commit.
2. If needed, redeploy the previous artifact from GitHub Actions history.
3. If a schema issue is involved, restore the Azure SQL database from its latest backup or point-in-time restore.

Do not delete production data just to retry a web deployment.

## Logs and diagnostics

Use these checks first:

1. App Service `Log stream`
2. App Service `Diagnose and solve problems`
3. GitHub Actions run logs
4. Azure SQL connection diagnostics
5. Blob Storage metrics and failed requests

Keep HTTPS only enabled in App Service.

## Production smoke checklist

After deployment, verify:

1. App root loads over HTTPS.
2. Manager login works.
3. `/prep/dough` loads.
4. `/prep/dough/week` loads.
5. `/dashboard` loads.
6. Manager can generate and save a dough recommendation.
7. Manager can create a dough task.
8. PizzaMaker can complete a dough task if that user exists in production.
9. Expo is blocked from dough/admin screens if that user exists in production.
10. AI deterministic recommendation renders on the dashboard.
11. Blob upload works.
12. Blob delete works.
13. No HTTP 500 responses appear in the main flow.

## Notes about first production users

This repo bootstraps built-in roles in every environment and can create the first manager automatically when `BootstrapAdmin__Password` is set.

The extra local smoke users (`PizzaMaker` and `Expo`) are still development-only by default. If you want those exact accounts in production, create them intentionally after the manager account is available instead of committing shared production passwords.
