# Azure Quick Start Guide

Get your Eidos app running in Azure in 15 minutes!

---

## Prerequisites

- Azure subscription
- Azure Key Vault created
- Azure CLI installed (optional, but recommended)

---

## Step 1: Add Secrets to Azure Key Vault (5 minutes)

Using Azure Portal:

1. Go to https://portal.azure.com
2. Find your Key Vault
3. Click "Secrets" → "+ Generate/Import"
4. Add these secrets:

| Secret Name | Value |
|-------------|-------|
| `Authentication--GitHub--ClientId` | Your new GitHub Client ID |
| `Authentication--GitHub--ClientSecret` | Your new GitHub Secret |

**Important**: Use `--` (double dash) not `:` for secret names!

---

## Step 2: Create App Service (3 minutes)

1. Portal → "Create a resource" → "Web App"
2. Fill in:
   - **Name**: `eidos-app` (must be unique)
   - **Runtime**: .NET 9
   - **OS**: Linux
   - **Plan**: B1 (Basic)
3. Click "Review + create" → "Create"

---

## Step 3: Enable Managed Identity (1 minute)

1. Go to your new App Service
2. Click "Identity" (left menu)
3. Switch to "On"
4. Click "Save"
5. **Copy the Object ID** (you'll need it)

---

## Step 4: Grant Key Vault Access (2 minutes)

1. Go back to your Key Vault
2. Click "Access policies" → "+ Create"
3. Select permissions: Get, List (under Secret permissions)
4. Click "Next"
5. Search for your App Service name
6. Select it → "Next" → "Next" → "Create"

---

## Step 5: Configure App Service (2 minutes)

1. Go to your App Service
2. Click "Configuration" → "New application setting"
3. Add these settings:

| Name | Value |
|------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `KeyVault__Uri` | `https://your-keyvault-name.vault.azure.net/` |

4. Click "Save" → "Continue"

---

## Step 6: Deploy Your App (2 minutes)

### Option A: From GitHub
1. App Service → "Deployment Center"
2. Select "GitHub"
3. Authorize and select your repo
4. Click "Save"

### Option B: From Local
```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../deploy.zip .
cd ..

az webapp deploy --name eidos-app --resource-group YourResourceGroup --src-path deploy.zip
```

---

## Step 7: Update OAuth Redirect URLs (2 minutes)

### GitHub
1. https://github.com/settings/developers
2. Edit your OAuth App
3. Add: `https://eidos-app.azurewebsites.net/signin-github`

### Google (if using)
1. https://console.cloud.google.com/apis/credentials
2. Edit OAuth client
3. Add: `https://eidos-app.azurewebsites.net/signin-google`

### Microsoft (if using)
1. https://portal.azure.com → App registrations
2. Edit your app → Authentication
3. Add: `https://eidos-app.azurewebsites.net/signin-microsoft`

---

## Done! 🎉

Visit: `https://eidos-app.azurewebsites.net`

---

## Troubleshooting

### App won't start?
Check logs: App Service → Log stream

### Can't login?
1. Verify Key Vault secrets are correct
2. Check Managed Identity is enabled
3. Verify Key Vault access policy

### Need SSL?
App Service → TLS/SSL settings → Create managed certificate (FREE!)

---

## Next Steps

- Add custom domain
- Enable Application Insights
- Set up GitHub Actions for CI/CD
- Configure auto-scaling

Full guide: See AZURE-DEPLOYMENT.md
