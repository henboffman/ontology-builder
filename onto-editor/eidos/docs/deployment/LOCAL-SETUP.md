# Local Development Setup

This document explains how to run Eidos locally without Azure Key Vault.

## Current Configuration

The application is now configured for **local-only development** with the following setup:

### Database
- **Type**: SQL Server (Docker container)
- **Server**: localhost,1433
- **Database Name**: EidosDb
- **Connection String**: `Server=localhost,1433;Database=EidosDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=True`
- **Status**: ✓ SQL Server container running and database ready

### Authentication Providers

Currently configured OAuth providers in User Secrets:
- **GitHub**: ClientId configured (secret needed separately)

### How It Works

1. **Key Vault Disabled**: `appsettings.Development.json` has an empty `KeyVault:Uri`, which tells the application to skip Azure Key Vault entirely.

2. **Configuration Priority** (when Key Vault is disabled):
   - User Secrets (stored locally, not in source control)
   - appsettings.Development.json (gitignored)
   - appsettings.json (committed to source control)

3. **The app will print on startup**:
   ```
   ℹ️  Azure Key Vault not configured. Using User Secrets and appsettings.
   ```

## Running the Application

### Starting the Application

Simply run:
```bash
dotnet run
```

On first run, the application will:
1. Connect to SQL Server in Docker
2. Create the EidosDb database if it doesn't exist
3. Create all necessary tables from the current schema
4. Seed admin roles and users
5. Start the web server

### Startup Messages

You should see these confirmations:
```
ℹ️  Azure Key Vault not configured. Using User Secrets and appsettings.
ℹ️  GitHub OAuth not configured (credentials not found in User Secrets or Key Vault)
ℹ️  Using SQL Server database: Server=localhost,1433;Database=EidosDb;...
ℹ️  SQL Server database schema ensured
```

The application will be available at:
- HTTPS: https://localhost:7216
- HTTP: http://localhost:5000

## Adding OAuth Providers (Optional)

If you want to enable OAuth login providers, add the secrets using `dotnet user-secrets`:

### GitHub OAuth
```bash
dotnet user-secrets set "Authentication:GitHub:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:GitHub:ClientSecret" "your-client-secret"
```

### Google OAuth
```bash
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret"
```

### Microsoft OAuth
```bash
dotnet user-secrets set "Authentication:Microsoft:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "your-client-secret"
```

### View Current Secrets
```bash
dotnet user-secrets list
```

## What's Safe for Source Control

✅ **Safe to commit**:
- appsettings.json (no secrets)
- This LOCAL-SETUP.md file
- Code changes

❌ **NOT in source control** (already gitignored):
- appsettings.Development.json
- User Secrets (stored in `~/.microsoft/usersecrets/`)
- SQL Server data (stored in Docker volume)

## Switching Back to Azure

To re-enable Azure Key Vault later, simply update `appsettings.Development.json`:

```json
{
  "KeyVault": {
    "Uri": "https://eidos.vault.azure.net/"
  }
}
```

Then run `az login` before starting the application.

## Changes Made for Local Development

The following modifications enable fully local development without Azure dependencies:

### 1. appsettings.Development.json (gitignored)
- Set `KeyVault:Uri` to empty string → Disables Azure Key Vault
- Changed `ConnectionStrings:DefaultConnection` to SQL Server connection string
- Removed OAuth credentials (now handled by User Secrets)

### 2. Program.cs
- Configured to use SQL Server for all environments
- Made GitHub OAuth optional (was required, now only enables if credentials present)
- Added helpful console messages for configuration status

### 3. Benefits
- **No Azure dependencies** - Works completely offline
- **Source control safe** - All secrets in gitignored files or User Secrets
- **Easy switching** - Just change KeyVault:Uri to re-enable Azure
- **Flexible** - OAuth providers are all optional for local dev

## Current Status

- ✓ Build succeeds
- ✓ SQL Server connection working (Docker container)
- ✓ Key Vault disabled for local development
- ✓ All OAuth providers optional
- ✓ All secrets excluded from source control
- ✓ Informative startup messages showing configuration
