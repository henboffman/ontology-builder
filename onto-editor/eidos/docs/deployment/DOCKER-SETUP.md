# Docker SQL Server Setup for Local Development

This guide will help you set up SQL Server in Docker for local development, matching your production Azure SQL Database environment.

---

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)

---

## Quick Start

### 1. Start SQL Server

From the project root directory:

```bash
docker-compose up -d
```

This will:
- Download SQL Server 2022 Developer Edition image (first time only)
- Start SQL Server container on port 1433
- Create a persistent volume for your data

### 2. Verify SQL Server is Running

```bash
docker-compose ps
```

You should see:
```
NAME                IMAGE                                      STATUS
eidos-sqlserver     mcr.microsoft.com/mssql/server:2022-latest Up
```

### 3. Run Your Application

```bash
dotnet run
```

The application will automatically:
- Connect to SQL Server at `localhost:1433`
- Create the `EidosDb` database if it doesn't exist
- Run Entity Framework migrations

---

## Managing SQL Server

### Stop SQL Server

```bash
docker-compose down
```

This stops the container but **preserves your data**.

### Restart SQL Server

```bash
docker-compose restart
```

### View SQL Server Logs

```bash
docker-compose logs -f sqlserver
```

### Reset Database (Delete All Data)

```bash
# Stop and remove container + volume
docker-compose down -v

# Start fresh
docker-compose up -d
```

⚠️ **Warning**: This will delete all your data!

---

## Connection Details

The application uses these connection settings (from `appsettings.Development.json`):

```
Server: localhost,1433
Database: EidosDb
User: sa
Password: YourStrong!Passw0rd
```

### Changing the Password

1. Edit `docker-compose.yml`:
   ```yaml
   environment:
     - SA_PASSWORD=YourNewPassword123!
   ```

2. Update `appsettings.Development.json`:
   ```json
   "DefaultConnection": "Server=localhost,1433;Database=EidosDb;User Id=sa;Password=YourNewPassword123!;TrustServerCertificate=True;"
   ```

3. Restart:
   ```bash
   docker-compose down
   docker-compose up -d
   ```

---

## Connecting with Database Tools

### Azure Data Studio (Recommended)

1. Download from: https://aka.ms/azuredatastudio
2. Connect with:
   - Server: `localhost,1433`
   - Authentication: SQL Login
   - User: `sa`
   - Password: `YourStrong!Passw0rd`

### SQL Server Management Studio (SSMS)

1. Download from: https://aka.ms/ssmsfullsetup
2. Connect with:
   - Server name: `localhost,1433`
   - Authentication: SQL Server Authentication
   - Login: `sa`
   - Password: `YourStrong!Passw0rd`

### VS Code

Install the "SQL Server (mssql)" extension and connect using the same credentials.

---

## Troubleshooting

### Port 1433 Already in Use

If you have SQL Server installed locally:

**Option 1**: Use a different port
```yaml
# In docker-compose.yml
ports:
  - "1434:1433"  # Use port 1434 instead
```

Then update your connection string:
```
Server=localhost,1434;Database=EidosDb;...
```

**Option 2**: Stop local SQL Server
```bash
# Windows
net stop MSSQLSERVER

# macOS
brew services stop mssql-server
```

### Container Won't Start

Check Docker Desktop is running:
```bash
docker info
```

View container logs:
```bash
docker-compose logs sqlserver
```

### Connection Refused

Wait for SQL Server to fully start (can take 10-20 seconds):
```bash
docker-compose logs -f sqlserver
```

Look for: `SQL Server is now ready for client connections`

### Out of Disk Space

SQL Server uses a persistent volume. To check disk usage:
```bash
docker system df
```

To clean up unused Docker resources:
```bash
docker system prune
```

---

## Data Persistence

Your database data is stored in a Docker volume named `sqlserver-data`. This volume persists even when you:
- Stop the container
- Remove the container
- Rebuild the container

To view volumes:
```bash
docker volume ls
```

To delete the volume (⚠️ **deletes all data**):
```bash
docker volume rm onto-editor_sqlserver-data
```

---

## Development Workflow

### Typical Daily Workflow

```bash
# Morning: Start SQL Server
docker-compose up -d

# Run your app
dotnet run

# ... develop throughout the day ...

# Evening: Stop SQL Server (optional)
docker-compose down
```

### After Git Pull (Database Changes)

If someone added migrations:

```bash
# Make sure SQL Server is running
docker-compose up -d

# Apply migrations
dotnet ef database update

# Or just run the app (migrations apply automatically)
dotnet run
```

### Creating a New Migration

```bash
# Make changes to your models

# Create migration
dotnet ef migrations add YourMigrationName

# Apply migration
dotnet ef database update
```

---

## Production Deployment

This Docker setup is **for local development only**. Production uses Azure SQL Database.

Key differences:
- **Development**: Docker SQL Server on localhost
- **Production**: Azure SQL Database with Managed Identity

Both use the same SQL Server provider, so your code works identically.

---

## Health Check

The docker-compose.yml includes a health check that runs every 10 seconds:

```bash
# View health status
docker-compose ps

# Should show "healthy" status
```

If the container is unhealthy, check logs:
```bash
docker-compose logs sqlserver
```

---

## Performance Tips

### Increase Memory

SQL Server performs better with more memory. In docker-compose.yml:

```yaml
services:
  sqlserver:
    # ... existing config ...
    deploy:
      resources:
        limits:
          memory: 4G  # Increase from default 2G
```

### Backup Before Major Changes

Before major migrations or data changes:

```bash
# Create backup
docker exec eidos-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong!Passw0rd" \
  -Q "BACKUP DATABASE EidosDb TO DISK='/var/opt/mssql/backup/EidosDb.bak'"

# Copy backup to host
docker cp eidos-sqlserver:/var/opt/mssql/backup/EidosDb.bak ./backup/
```

---

## Why Docker SQL Server?

### Benefits

1. **Environment Parity**: Matches production SQL Server behavior
2. **Isolated**: Doesn't interfere with other projects
3. **Easy Setup**: No manual SQL Server installation
4. **Cross-Platform**: Works on Windows, macOS, Linux
5. **Consistent**: Team uses same SQL Server version
6. **Clean**: Easy to reset and start fresh

### When to Use SQLite Instead

Consider SQLite for:
- Quick prototypes
- Simple demos
- No SQL Server-specific features needed
- Offline development

For this project, we use SQL Server to ensure production parity.

---

## Getting Help

- Docker issues: Check Docker Desktop logs
- SQL Server issues: Check container logs with `docker-compose logs`
- Connection issues: Verify port 1433 is available
- Data issues: Consider resetting with `docker-compose down -v`

**Remember**: Your data persists in the Docker volume, so stopping/starting the container is safe!
