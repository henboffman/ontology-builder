# Eidos Ontology Builder - Deployment Guide

## Overview

This guide covers deployment for:
1. **Local Mac Development** (SQLite database)
2. **IIS Production** (Remote SQL Server)

**Note**: Azure-specific features (Key Vault, Application Insights, Redis) are now **optional** and disabled by default.

---

## 1. Local Mac Development Setup

### Prerequisites
- .NET 9.0 SDK
- macOS with terminal access

### Configuration

The default configuration in `appsettings.json` uses SQLite, which works out-of-the-box:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=ontology.db"
  }
}
```

### Running Locally

```bash
cd /Users/benjaminhoffman/documents/code/ontology-builder/onto-editor/eidos

# Restore packages
dotnet restore

# Build
dotnet build

# Run (will create SQLite database automatically)
dotnet run
```

The application will:
- ✅ Use SQLite database (`ontology.db` in the project directory)
- ✅ Run automatic migrations in development
- ✅ Use in-memory caching and presence tracking
- ✅ Run on `https://localhost:7216` and `http://localhost:5000`

### Optional: Configure OAuth Providers

To enable GitHub/Google/Microsoft login:

```bash
# Set secrets for OAuth
dotnet user-secrets set "Authentication:GitHub:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:GitHub:ClientSecret" "your-client-secret"

dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret"

dotnet user-secrets set "Authentication:Microsoft:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "your-client-secret"
```

---

## 2. IIS Production Deployment

### Prerequisites
- Windows Server with IIS 10+ installed
- .NET 9.0 Hosting Bundle installed
- SQL Server (local or remote)
- IIS configured with an Application Pool

### Step 1: Prepare SQL Server Database

**Option A: Create database and apply migrations manually (RECOMMENDED)**

```bash
# On your development machine, create a migration script
cd /Users/benjaminhoffman/documents/code/ontology-builder/onto-editor/eidos

# Generate SQL script for all migrations
dotnet ef migrations script --output migrations.sql --idempotent --project Eidos.csproj

# Copy migrations.sql to your SQL Server and execute it
# Or use command line:
sqlcmd -S YOUR_SQL_SERVER -d EidosDb -U YOUR_USERNAME -P YOUR_PASSWORD -i migrations.sql
```

**Option B: Use Entity Framework migrations on the server**

```bash
# On the deployment server
dotnet ef database update --project Eidos.csproj --configuration Release
```

### Step 2: Configure Connection String

**Edit `appsettings.Production.json`:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SQL_SERVER;Database=EidosDb;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;"
  }
}
```

**OR use web.config** (more secure for IIS):

```xml
<configuration>
  <appSettings>
    <add key="ConnectionStrings:DefaultConnection" value="Server=YOUR_SQL_SERVER;Database=EidosDb;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True;" />
  </appSettings>
</configuration>
```

### Step 3: Build for Production

```bash
# On your Mac, publish the application
cd /Users/benjaminhoffman/documents/code/ontology-builder/onto-editor/eidos

dotnet publish -c Release -o ./publish
```

### Step 4: Deploy to IIS

1. **Copy published files** to IIS server (e.g., `C:\inetpub\wwwroot\eidos`)

2. **Create Application Pool** in IIS Manager:
   - Name: `EidosAppPool`
   - .NET CLR Version: **No Managed Code**
   - Managed Pipeline Mode: Integrated
   - Identity: ApplicationPoolIdentity (or specific service account)

3. **Create Website/Application**:
   - Physical path: `C:\inetpub\wwwroot\eidos`
   - Application Pool: `EidosAppPool`
   - Bindings: Configure HTTP/HTTPS as needed

4. **Set Permissions**:
   ```powershell
   # Grant IIS AppPool user access to application folder
   icacls "C:\inetpub\wwwroot\eidos" /grant "IIS AppPool\EidosAppPool:(OI)(CI)F" /T
   ```

5. **Configure Environment**:
   - In IIS Manager → Application → Configuration Editor
   - Set `ASPNETCORE_ENVIRONMENT` to `Production`

### Step 5: Verify Deployment

1. Browse to your IIS website
2. Check IIS logs: `C:\inetpub\wwwroot\eidos\logs`
3. Verify database connectivity
4. Test login and basic functionality

---

## 3. Production Configuration Options

### Disable Automatic Migrations (Default - SECURE)

By default, the app will **NOT** run migrations in production. It will:
- ✅ Verify database connection
- ✅ Check for pending migrations
- ✅ Block startup if migrations are pending (safe)

To allow startup with pending migrations (**NOT RECOMMENDED**):

```json
{
  "Database": {
    "AllowPendingMigrations": true
  }
}
```

### Configure OAuth for Production

Add OAuth secrets to `appsettings.Production.json` or environment variables:

```json
{
  "Authentication": {
    "GitHub": {
      "ClientId": "your-production-client-id",
      "ClientSecret": "your-production-client-secret"
    }
  }
}
```

**OR** use environment variables in IIS:

```
Authentication__GitHub__ClientId=your-client-id
Authentication__GitHub__ClientSecret=your-client-secret
```

---

## 4. Optional: Multi-Server Scaling with Redis

If you need to scale horizontally (multiple IIS servers behind load balancer):

### Install Redis

```bash
# Windows: Install Redis for Windows or use Azure Redis Cache
# Linux: apt-get install redis-server
```

### Configure Redis

```json
{
  "Redis": {
    "ConnectionString": "your-redis-server:6379,password=your-redis-password"
  }
}
```

The application will automatically:
- ✅ Use Redis for SignalR backplane (cross-server messaging)
- ✅ Use Redis for distributed caching
- ✅ Use Redis for presence tracking

---

## 5. Troubleshooting

### Issue: Database connection fails on startup

**Check:**
1. SQL Server is accessible from IIS server
2. Firewall allows connections to SQL Server port (default: 1433)
3. SQL Server allows remote connections
4. Connection string credentials are correct
5. Database exists and migrations are applied

**Test connection:**
```bash
sqlcmd -S YOUR_SQL_SERVER -U YOUR_USERNAME -P YOUR_PASSWORD -Q "SELECT @@VERSION"
```

### Issue: "Pending migrations detected" error

**Solution:** Apply migrations manually before deployment

```bash
dotnet ef database update --project Eidos.csproj --connection "YOUR_CONNECTION_STRING"
```

### Issue: OAuth providers not working

**Check:**
1. OAuth secrets are configured in production appsettings
2. Redirect URIs are configured in OAuth provider settings
3. Production domain is whitelisted in OAuth app settings

### Issue: Application pool crashes

**Check:**
1. .NET 9.0 Hosting Bundle is installed
2. Application pool is set to "No Managed Code"
3. IIS AppPool has permissions to application folder
4. Check Windows Event Viewer for errors

---

## 6. Security Checklist

Before deploying to production:

- [ ] Connection strings use secure passwords
- [ ] OAuth secrets are not in source control
- [ ] `AllowedHosts` is configured (not "*")
- [ ] HTTPS is enforced
- [ ] Rate limiting is enabled (default: yes)
- [ ] SQL command text logging is disabled (default: yes)
- [ ] Development auto-login is disabled in production (default: yes)
- [ ] Migrations are applied manually (default: required)
- [ ] IIS AppPool runs with least privilege

---

## 7. Performance Tuning

### IIS Settings

```xml
<!-- web.config -->
<configuration>
  <system.webServer>
    <aspNetCore processPath="dotnet"
                arguments=".\Eidos.dll"
                stdoutLogEnabled="false"
                stdoutLogFile=".\logs\stdout"
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

### SQL Server Connection Pooling

Already configured in application:
- ✅ Connection resiliency (5 retries)
- ✅ Command timeout: 60 seconds
- ✅ Max batch size: 100 queries

### Application Settings

For high-traffic sites, adjust in `appsettings.Production.json`:

```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 200
      }
    ]
  }
}
```

---

## 8. Monitoring and Logging

### IIS Logs

Default location: `C:\inetpub\logs\LogFiles`

### Application Logs

ASP.NET Core logs: `C:\inetpub\wwwroot\eidos\logs`

### Optional: Application Insights

Only if you want to use Azure Application Insights:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;..."
  }
}
```

---

## Quick Reference

| Environment | Database | Migrations | Caching | Backplane |
|-------------|----------|------------|---------|-----------|
| **Mac Dev** | SQLite | Automatic | In-Memory | None |
| **IIS Prod** | SQL Server | Manual | In-Memory | None |
| **Multi-Server** | SQL Server | Manual | Redis | Redis |

---

## Support

For issues or questions:
1. Check application logs
2. Review this deployment guide
3. Test locally first
4. Verify all prerequisites are installed
