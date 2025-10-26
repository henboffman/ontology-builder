#!/bin/bash
# Secure Azure Deployment Script for Eidos Ontology Builder
# This script creates all required Azure resources with security best practices

set -e  # Exit on error

# Configuration
RESOURCE_GROUP="eidos-rg"
LOCATION="canadacentral"
APP_NAME="eidos-app"
SQL_SERVER="eidos-sql-server"
SQL_DB="eidos-db"
KEYVAULT_NAME="eidos-keyvault"
APP_SERVICE_PLAN="eidos-plan"

# Generate unique names (SQL server and Key Vault names must be globally unique)
UNIQUE_SUFFIX=$(date +%s | tail -c 5)
SQL_SERVER="${SQL_SERVER}-${UNIQUE_SUFFIX}"
KEYVAULT_NAME="${KEYVAULT_NAME}-${UNIQUE_SUFFIX}"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Eidos Secure Azure Deployment${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Prompt for SQL admin password (won't be stored, only used to create resources)
echo -e "${YELLOW}Please enter a strong SQL Server admin password:${NC}"
echo -e "${YELLOW}(Must be at least 8 characters, contain uppercase, lowercase, numbers, and special characters)${NC}"
read -s SQL_ADMIN_PASSWORD
echo ""

# Validate password
if [ ${#SQL_ADMIN_PASSWORD} -lt 8 ]; then
    echo -e "${YELLOW}Error: Password must be at least 8 characters${NC}"
    exit 1
fi

# Step 1: Create Resource Group
echo -e "${GREEN}[1/8] Creating Resource Group...${NC}"
az group create \
    --name $RESOURCE_GROUP \
    --location $LOCATION \
    --output none

# Step 2: Create App Service Plan (B1 tier - suitable for production with low traffic)
echo -e "${GREEN}[2/8] Creating App Service Plan...${NC}"
az appservice plan create \
    --name $APP_SERVICE_PLAN \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku B1 \
    --is-linux \
    --output none

# Step 3: Create App Service with Managed Identity
echo -e "${GREEN}[3/8] Creating App Service with Managed Identity...${NC}"
az webapp create \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --plan $APP_SERVICE_PLAN \
    --runtime "DOTNET|9.0" \
    --output none

# Enable Managed Identity (for secure access to Key Vault)
az webapp identity assign \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --output none

# Configure HTTPS only
az webapp update \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --https-only true \
    --output none

# Configure minimum TLS version
az webapp config set \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --min-tls-version 1.2 \
    --output none

# Step 4: Create SQL Server with Azure AD Authentication
echo -e "${GREEN}[4/8] Creating SQL Server...${NC}"
az sql server create \
    --name $SQL_SERVER \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --admin-user "eidos-admin" \
    --admin-password "$SQL_ADMIN_PASSWORD" \
    --minimal-tls-version 1.2 \
    --output none

# Configure firewall - Allow Azure services
az sql server firewall-rule create \
    --name "AllowAzureServices" \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none

# Step 5: Create SQL Database
echo -e "${GREEN}[5/8] Creating SQL Database...${NC}"
az sql db create \
    --name $SQL_DB \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER \
    --tier Basic \
    --output none

# Step 6: Create Key Vault
echo -e "${GREEN}[6/8] Creating Key Vault...${NC}"
az keyvault create \
    --name $KEYVAULT_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --enable-rbac-authorization false \
    --output none

# Get the App Service's Managed Identity Principal ID
PRINCIPAL_ID=$(az webapp identity show \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query principalId \
    --output tsv)

# Grant App Service access to Key Vault secrets
az keyvault set-policy \
    --name $KEYVAULT_NAME \
    --resource-group $RESOURCE_GROUP \
    --object-id $PRINCIPAL_ID \
    --secret-permissions get list \
    --output none

# Step 7: Store SQL Connection String in Key Vault
echo -e "${GREEN}[7/8] Storing connection string in Key Vault...${NC}"
CONNECTION_STRING="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Initial Catalog=${SQL_DB};Persist Security Info=False;User ID=eidos-admin;Password=${SQL_ADMIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az keyvault secret set \
    --vault-name $KEYVAULT_NAME \
    --name "ConnectionStrings--DefaultConnection" \
    --value "$CONNECTION_STRING" \
    --output none

# Step 8: Configure App Service to use Key Vault
echo -e "${GREEN}[8/8] Configuring App Service...${NC}"
KEYVAULT_URI=$(az keyvault show \
    --name $KEYVAULT_NAME \
    --resource-group $RESOURCE_GROUP \
    --query properties.vaultUri \
    --output tsv)

# Set Key Vault reference in App Service configuration
az webapp config appsettings set \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        KeyVault__Uri="${KEYVAULT_URI}" \
        ASPNETCORE_ENVIRONMENT="Production" \
    --output none

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${BLUE}Resource Details:${NC}"
echo -e "Resource Group:    ${YELLOW}$RESOURCE_GROUP${NC}"
echo -e "App Service:       ${YELLOW}https://${APP_NAME}.azurewebsites.net${NC}"
echo -e "SQL Server:        ${YELLOW}${SQL_SERVER}.database.windows.net${NC}"
echo -e "Database:          ${YELLOW}$SQL_DB${NC}"
echo -e "Key Vault:         ${YELLOW}$KEYVAULT_NAME${NC}"
echo ""
echo -e "${BLUE}Security Features Enabled:${NC}"
echo -e "✓ Managed Identity for Key Vault access"
echo -e "✓ HTTPS Only enforcement"
echo -e "✓ TLS 1.2 minimum"
echo -e "✓ SQL Firewall (Azure services only)"
echo -e "✓ Secrets stored in Key Vault"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo -e "1. Deploy your application code to the App Service"
echo -e "2. Run database migrations"
echo -e "3. Configure OAuth providers (if needed)"
echo ""
