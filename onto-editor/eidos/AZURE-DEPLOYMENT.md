# Azure Deployment Guide for Eidos

This guide walks you through deploying Eidos to Azure with enterprise-grade security using Azure Key Vault.

---

## Prerequisites

- Azure subscription
- Azure CLI installed: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
- Azure Key Vault created (you mentioned you already have one!)
- Git repository (for Azure deployment)

---

## Part 1: Azure Key Vault Setup

### Step 1: Store Your Secrets in Azure Key Vault

First, you need to add your OAuth credentials to your Azure Key Vault.

#### Using Azure Portal

1. Go to https://portal.azure.com
2. Navigate to your Key Vault
3. Click on "Secrets" in the left menu
4. Click "+ Generate/Import"

Add these secrets (use the exact names below):

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `Authentication--GitHub--ClientId` | Your GitHub Client ID | GitHub OAuth Client ID |
| `Authentication--GitHub--ClientSecret` | Your GitHub Secret | GitHub OAuth Secret |
| `Authentication--Google--ClientId` | Your Google Client ID | Google OAuth Client ID (optional) |
| `Authentication--Google--ClientSecret` | Your Google Secret | Google OAuth Secret (optional) |
| `Authentication--Microsoft--ClientId` | Your Microsoft Client ID | Microsoft OAuth Client ID (optional) |
| `Authentication--Microsoft--ClientSecret` | Your Microsoft Secret | Microsoft OAuth Secret (optional) |

**Note**: Azure Key Vault uses `--` instead of `:` for hierarchical keys.

#### Using Azure CLI

```bash
# Login to Azure
az login

# Set your Key Vault name
KEYVAULT_NAME="your-keyvault-name"

# Add GitHub OAuth credentials (REQUIRED)
az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "Authentication--GitHub--ClientId" \
  --value "YOUR_GITHUB_CLIENT_ID"

az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "Authentication--GitHub--ClientSecret" \
  --value "YOUR_GITHUB_CLIENT_SECRET"

# Add Google OAuth credentials (OPTIONAL)
az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "Authentication--Google--ClientId" \
  --value "YOUR_GOOGLE_CLIENT_ID"

az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "Authentication--Google--ClientSecret" \
  --value "YOUR_GOOGLE_CLIENT_SECRET"

# Add Microsoft OAuth credentials (OPTIONAL)
az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "Authentication--Microsoft--ClientId" \
  --value "YOUR_MICROSOFT_CLIENT_ID"

az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "Authentication--Microsoft--ClientSecret" \
  --value "YOUR_MICROSOFT_CLIENT_SECRET"
```

### Step 2: Get Your Key Vault URI

```bash
az keyvault show --name $KEYVAULT_NAME --query properties.vaultUri --output tsv
```

Save this URI - you'll need it for App Service configuration.

Example: `https://your-keyvault-name.vault.azure.net/`

---

## Part 2: Azure App Service Setup

### Option A: Using Azure Portal

#### 1. Create App Service

1. Go to https://portal.azure.com
2. Click "Create a resource"
3. Search for "Web App"
4. Fill in the details:
   - **Resource Group**: Create new or use existing
   - **Name**: `eidos-app` (or your preferred name)
   - **Publish**: Code
   - **Runtime stack**: .NET 9
   - **Operating System**: Linux (recommended) or Windows
   - **Region**: Choose closest to your users
   - **App Service Plan**: Choose appropriate tier (B1 or higher recommended)

5. Click "Review + create" → "Create"

#### 2. Enable Managed Identity

1. Go to your App Service
2. Navigate to "Identity" (under Settings)
3. Switch "Status" to "On"
4. Click "Save"
5. Copy the "Object (principal) ID" - you'll need this

#### 3. Grant Key Vault Access to App Service

1. Go to your Key Vault
2. Navigate to "Access policies"
3. Click "+ Create"
4. Under "Secret permissions", select:
   - Get
   - List
5. Click "Next"
6. Under "Principal", search for your App Service name and select it
7. Click "Next" → "Next" → "Create"

#### 4. Configure App Service Settings

1. Go to your App Service
2. Navigate to "Configuration" (under Settings)
3. Click "New application setting" for each:

| Name | Value |
|------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `KeyVault__Uri` | Your Key Vault URI (from Step 2 above) |
| `AllowedHosts` | Your domain (e.g., `eidos.yourdomain.com`) |

4. Click "Save"

### Option B: Using Azure CLI

```bash
# Variables
RESOURCE_GROUP="eidos-rg"
APP_NAME="eidos-app"
LOCATION="eastus"
KEYVAULT_NAME="your-keyvault-name"

# Create Resource Group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service Plan
az appservice plan create \
  --name "${APP_NAME}-plan" \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan "${APP_NAME}-plan" \
  --runtime "DOTNETCORE:9.0"

# Enable Managed Identity
az webapp identity assign \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the Managed Identity Principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

# Grant Key Vault access to App Service
az keyvault set-policy \
  --name $KEYVAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Get Key Vault URI
KEYVAULT_URI=$(az keyvault show \
  --name $KEYVAULT_NAME \
  --query properties.vaultUri \
  --output tsv)

# Configure App Settings
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    KeyVault__Uri=$KEYVAULT_URI \
    AllowedHosts="your-domain.com"
```

---

## Part 3: Database Setup

### Option 1: SQLite (Simple, for small apps)

Your app already uses SQLite. It will work as-is in Azure, but:
- ⚠️ Data is stored in the container (ephemeral)
- ⚠️ Data will be lost on app restart/redeployment

**To persist SQLite data**, use Azure File Share:

```bash
# Create storage account
az storage account create \
  --name eidosstorage \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

# Create file share
az storage share create \
  --name eidos-data \
  --account-name eidosstorage

# Mount to App Service
az webapp config storage-account add \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --custom-id EidosData \
  --storage-type AzureFiles \
  --share-name eidos-data \
  --account-name eidosstorage \
  --mount-path /data

# Update connection string in App Settings
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings ConnectionStrings__DefaultConnection="Data Source=/data/ontology.db"
```

### Option 2: Azure SQL Database (Recommended for production)

```bash
# Create SQL Server
az sql server create \
  --name eidos-sql-server \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user eidosadmin \
  --admin-password "YOUR_SECURE_PASSWORD"

# Create SQL Database
az sql db create \
  --name eidos-db \
  --server eidos-sql-server \
  --resource-group $RESOURCE_GROUP \
  --service-objective S0

# Configure firewall to allow Azure services
az sql server firewall-rule create \
  --server eidos-sql-server \
  --resource-group $RESOURCE_GROUP \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Get connection string
az sql db show-connection-string \
  --name eidos-db \
  --server eidos-sql-server \
  --client ado.net

# Add connection string to Key Vault
az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "ConnectionStrings--DefaultConnection" \
  --value "YOUR_CONNECTION_STRING"
```

Then update your `Program.cs` to use SQL Server:
```csharp
// In Eidos.csproj, add:
// <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />

// In Program.cs, change:
options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
// to:
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
```

---

## Part 4: Deployment

### Option A: Deploy from GitHub (Recommended)

1. Push your code to GitHub
2. In Azure Portal, go to your App Service
3. Navigate to "Deployment Center"
4. Select "GitHub" as the source
5. Authorize Azure to access your GitHub
6. Select your repository and branch
7. Click "Save"

Azure will automatically build and deploy your app whenever you push to GitHub!

### Option B: Deploy from Local (Quick Start)

```bash
# Build the app
dotnet publish -c Release -o ./publish

# Create a zip file
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deploy \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src-path deploy.zip \
  --type zip
```

### Option C: Deploy using VS Code

1. Install "Azure App Service" extension
2. Sign in to Azure
3. Right-click your project → "Deploy to Web App"
4. Select your App Service
5. Confirm deployment

---

## Part 5: Custom Domain & HTTPS

### Add Custom Domain

```bash
# Add custom domain
az webapp config hostname add \
  --webapp-name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --hostname eidos.yourdomain.com

# Enable HTTPS
az webapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --https-only true
```

### Enable Free SSL Certificate

1. In Azure Portal, go to your App Service
2. Navigate to "TLS/SSL settings"
3. Click "Private Key Certificates (.pfx)"
4. Click "+ Create App Service Managed Certificate"
5. Select your custom domain
6. Click "Create"
7. Go to "Custom domains"
8. Click "Add binding" for your domain
9. Select the certificate you just created
10. Click "Add"

---

## Part 6: Update OAuth Redirect URIs

Update your OAuth app settings to include your Azure domain:

### GitHub OAuth
1. Go to https://github.com/settings/developers
2. Edit your OAuth App
3. Add callback URL: `https://your-app-name.azurewebsites.net/signin-github`
4. Or use your custom domain: `https://eidos.yourdomain.com/signin-github`

### Google OAuth
1. Go to https://console.cloud.google.com/apis/credentials
2. Edit your OAuth 2.0 Client ID
3. Add authorized redirect URI: `https://your-app-name.azurewebsites.net/signin-google`

### Microsoft OAuth
1. Go to https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps
2. Edit your app registration
3. Go to "Authentication"
4. Add redirect URI: `https://your-app-name.azurewebsites.net/signin-microsoft`

---

## Part 7: Monitoring & Logging

### Enable Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app eidos-insights \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app eidos-insights \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey \
  --output tsv)

# Configure App Service to use Application Insights
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings APPINSIGHTS_INSTRUMENTATIONKEY=$INSTRUMENTATION_KEY
```

### View Logs

```bash
# Stream logs
az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP

# Or in Azure Portal:
# App Service → Monitoring → Log stream
```

---

## Deployment Checklist

Before going live:

- [ ] OAuth secrets stored in Azure Key Vault
- [ ] Managed Identity enabled for App Service
- [ ] Key Vault access policy configured
- [ ] App Service configured with Key Vault URI
- [ ] AllowedHosts set to your domain
- [ ] Database configured (SQLite with persistent storage OR Azure SQL)
- [ ] OAuth redirect URIs updated to include Azure domain
- [ ] Custom domain configured (optional)
- [ ] HTTPS enabled
- [ ] Application Insights configured
- [ ] Test the deployed application thoroughly
- [ ] Monitor logs for any errors

---

## Troubleshooting

### App won't start
```bash
# Check logs
az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP

# Common issues:
# - Key Vault URI not set
# - Managed Identity not enabled
# - Missing Key Vault access policy
# - OAuth secrets not in Key Vault
```

### Can't access Key Vault
```bash
# Verify Managed Identity
az webapp identity show --name $APP_NAME --resource-group $RESOURCE_GROUP

# Verify Key Vault policy
az keyvault show --name $KEYVAULT_NAME --query properties.accessPolicies
```

### Database errors
```bash
# For SQLite: Check if mount path is correct
# For Azure SQL: Check connection string and firewall rules
```

---

## Cost Estimates

Here's an approximate monthly cost (as of 2025, East US region):

- **App Service B1**: ~$13/month
- **Azure SQL S0**: ~$15/month (if using SQL Server)
- **Storage Account**: ~$0.05/month (for SQLite file share)
- **Application Insights**: Free tier available (1GB/month)
- **Key Vault**: $0.03 per 10,000 operations (negligible)

**Total**: ~$13-28/month depending on database choice

---

## Next Steps

1. **Set up CI/CD**: Configure GitHub Actions for automatic deployments
2. **Configure monitoring**: Set up alerts in Application Insights
3. **Backup strategy**: Configure database backups
4. **Scaling**: Configure auto-scaling rules if needed
5. **CDN**: Consider Azure CDN for static assets

---

## Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Managed Identity Documentation](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [Application Insights Documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)

---

Your Eidos application is now ready for Azure deployment with enterprise-grade security! 🚀
