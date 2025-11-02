# Database Configuration Guide for Eidos

This guide explains the hybrid database setup: **SQLite for local development** and **SQL Server for Azure production**.

---

## Overview

Eidos uses a hybrid database approach:

- **Development (local)**: SQLite - simple, no setup required
- **Production (Azure)**: Azure SQL Database - scalable, managed cloud database

The application automatically switches based on the environment (configured in `Program.cs:94-109`).

---

## Changes Made

### 1. Fixed Decimal Precision Warning

Updated `Data/OntologyDbContext.cs:78-81` to specify precision for the `Relationship.Strength` property:

```csharp
modelBuilder.Entity<Relationship>()
    .Property(r => r.Strength)
    .HasPrecision(18, 2);
```

This prevents SQL Server from silently truncating decimal values in production.

### 2. Configured Hybrid Database Setup

Updated `Program.cs:94-109` to use different databases per environment:

```csharp
builder.Services.AddDbContextFactory<OntologyDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // SQLite for local development
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
    else
    {
        // SQL Server for production (Azure SQL Database)
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});
```

---

## Local Development Setup

### No Setup Required!

Just run the application - SQLite is used automatically in development:

```bash
dotnet run
```

The database file (`ontology.db`) will be created in your project directory on first run.

### Benefits of SQLite for Development

- **Zero configuration** - no database server to install
- **File-based** - easy to backup, delete, or reset
- **Fast** - perfect for development and testing
- **Cross-platform** - works on macOS, Windows, and Linux
- **Portable** - commit the .db file to git for seed data (optional)

### If You Want to Use SQL Server Locally (Optional)

You can test with SQL Server locally if needed:

#### macOS

SQL Server doesn't run natively on macOS. Use Docker:

#### Option 1: Docker (Recommended for macOS)

```bash
# Pull and run SQL Server 2022 container
docker run -e 'ACCEPT_EULA=Y' \
  -e 'SA_PASSWORD=YourStrong!Passw0rd' \
  -p 1433:1433 \
  --name eidos-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Verify it's running
docker ps

# View logs if needed
docker logs eidos-sqlserver
```

Then update your `appsettings.Development.json` ConnectionString to:
```json
"DefaultConnection": "Server=localhost,1433;Database=EidosDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

**Note**: The password must meet SQL Server requirements:
- At least 8 characters
- Contains uppercase, lowercase, numbers, and symbols

#### Managing the Docker Container

```bash
# Stop the container
docker stop eidos-sqlserver

# Start the container again
docker start eidos-sqlserver

# Remove the container (if you want to start fresh)
docker rm -f eidos-sqlserver

# Connect using SQL tools (optional)
docker exec -it eidos-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong!Passw0rd'
```

### Windows

#### Option 1: SQL Server LocalDB (Comes with Visual Studio)

Already configured in `appsettings.json`:
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EidosDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Just run the app - LocalDB will start automatically.

#### Option 2: SQL Server Express

Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads

Use this connection string:
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=EidosDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

---

## Running the Application

### Development (Default)

Just run the application - SQLite is used automatically:

```bash
dotnet run
```

The first time you run the app, Entity Framework will:
1. Create the `ontology.db` file in your project directory
2. Create all tables and relationships
3. Seed initial feature toggles

### Testing Production Configuration Locally

To test the production SQL Server configuration locally:

1. Set the environment to Production:
   ```bash
   export ASPNETCORE_ENVIRONMENT=Production  # macOS/Linux
   # or
   $env:ASPNETCORE_ENVIRONMENT="Production"  # Windows PowerShell
   ```

2. Ensure you have SQL Server running and a connection string configured

3. Run the application:
   ```bash
   dotnet run
   ```

The app will create the `EidosDb` database in SQL Server with all tables.

---

## Azure SQL Database Setup

When deploying to Azure, follow these steps:

### 1. Create Azure SQL Database

```bash
# Variables
RESOURCE_GROUP="eidos-rg"
SQL_SERVER_NAME="eidos-sql-server"  # Must be globally unique
SQL_DB_NAME="EidosDb"
ADMIN_USER="eidosadmin"
ADMIN_PASSWORD="YourStrong!Passw0rd123"  # Change this!

# Create SQL Server
az sql server create \
  --name $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --location eastus \
  --admin-user $ADMIN_USER \
  --admin-password "$ADMIN_PASSWORD"

# Create Database
az sql db create \
  --name $SQL_DB_NAME \
  --server $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --service-objective S0 \
  --backup-storage-redundancy Local

# Allow Azure services to access
az sql server firewall-rule create \
  --server $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### 2. Get Connection String

```bash
az sql db show-connection-string \
  --name $SQL_DB_NAME \
  --server $SQL_SERVER_NAME \
  --client ado.net
```

Example output:
```
Server=tcp:eidos-sql-server.database.windows.net,1433;Initial Catalog=EidosDb;Persist Security Info=False;User ID=<username>;Password=<password>;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### 3. Store in Azure Key Vault

```bash
# Get the actual connection string
CONNECTION_STRING="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Initial Catalog=${SQL_DB_NAME};Persist Security Info=False;User ID=${ADMIN_USER};Password=${ADMIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Store in Key Vault
az keyvault secret set \
  --vault-name your-keyvault-name \
  --name "ConnectionStrings--DefaultConnection" \
  --value "$CONNECTION_STRING"
```

### 4. Configure App Service

Your `Program.cs` is already configured to load secrets from Azure Key Vault in production. Just ensure:

1. Key Vault URI is set in App Service Configuration:
   ```
   KeyVault__Uri = https://your-keyvault.vault.azure.net/
   ```

2. Managed Identity is enabled and has access to Key Vault

3. The connection string secret is in Key Vault with the name `ConnectionStrings--DefaultConnection`

---

## Troubleshooting

### "Cannot connect to SQL Server"

**On macOS:**
- Verify Docker container is running: `docker ps`
- Check container logs: `docker logs eidos-sqlserver`
- Ensure password meets requirements
- Verify connection string port is `1433`

**On Windows:**
- For LocalDB: Ensure Visual Studio is installed
- For SQL Server Express: Verify service is running
- Check firewall isn't blocking port 1433

### "Login failed for user"

- Verify username and password in connection string
- For Azure SQL: Check firewall rules allow your IP
- For Docker: Ensure password matches the one used when creating container

### Database doesn't exist

Entity Framework should create it automatically. If not:
```bash
# Clean and rebuild
dotnet clean
dotnet build
dotnet run
```

### Migration issues

If you get migration errors:
```bash
# Install EF tools if not already installed
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration
dotnet ef database update
```

---

## Connection String Reference

### Local Development (macOS - Docker)
```
Server=localhost,1433;Database=EidosDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true
```

### Local Development (Windows - LocalDB)
```
Server=(localdb)\\mssqllocaldb;Database=EidosDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

### Local Development (Windows - SQL Server Express)
```
Server=localhost\\SQLEXPRESS;Database=EidosDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

### Azure SQL Database
```
Server=tcp:your-server.database.windows.net,1433;Initial Catalog=EidosDb;Persist Security Info=False;User ID=your-admin;Password=your-password;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

---

## Database Tools

### Azure Data Studio (Cross-platform, Recommended)

Download: https://aka.ms/azuredatastudio

Works on macOS, Windows, and Linux. Great for managing both local and Azure SQL databases.

### SQL Server Management Studio (Windows only)

Download: https://aka.ms/ssmsfullsetup

The classic tool for SQL Server management.

### Command Line (sqlcmd)

Included in the Docker container:
```bash
docker exec -it eidos-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong!Passw0rd'
```

---

## Cost Estimates (Azure SQL Database)

- **Basic Tier**: ~$5/month (5 DTUs, 2GB)
- **S0 Standard**: ~$15/month (10 DTUs, 250GB)
- **S1 Standard**: ~$30/month (20 DTUs, 250GB)

For development/testing, start with S0. Scale up as needed.

---

## Next Steps

1. Choose your local development option (Docker for macOS)
2. Update `appsettings.Development.json` with appropriate connection string
3. Run the application with `dotnet run`
4. For Azure deployment, follow the Azure SQL Database setup above

See `AZURE-DEPLOYMENT.md` for complete Azure deployment guide.
