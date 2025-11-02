# Testing Azure Key Vault Integration Locally

This guide shows you how to test Azure Key Vault integration in your local development environment.

---

## Prerequisites

1. **Azure CLI installed** and logged in:
   ```bash
   az login
   ```

2. **Azure Key Vault created** with secrets stored

3. **Access to the Key Vault** - your Azure account must have "Get" and "List" permissions for secrets

---

## Step 1: Verify Azure Login

```bash
# Check if you're logged in
az account show

# If not logged in
az login
```

---

## Step 2: Add Test Secrets to Key Vault

If you haven't already, add some test secrets to your Key Vault:

```bash
# Set your Key Vault name
KEYVAULT_NAME="your-keyvault-name"

# Add a test secret (GitHub OAuth for example)
az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name "Authentication--GitHub--ClientId" \
  --value "test-client-id"

az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name "Authentication--GitHub--ClientSecret" \
  --value "test-client-secret"
```

**Note**: Use `--` (double dash) instead of `:` in secret names!

---

## Step 3: Enable Key Vault in Development

### Option A: Edit appsettings.Development.json

1. Open `appsettings.Development.json`
2. Update the KeyVault section:

```json
{
  "KeyVault": {
    "EnableInDevelopment": true,
    "Uri": "https://your-keyvault-name.vault.azure.net/"
  }
}
```

### Option B: Use User Secrets (Recommended)

This keeps your Key Vault URI out of git:

```bash
dotnet user-secrets set "KeyVault:EnableInDevelopment" "true"
dotnet user-secrets set "KeyVault:Uri" "https://your-keyvault-name.vault.azure.net/"
```

---

## Step 4: Run the Application

```bash
dotnet run
```

You should see this message on startup:

```
✓ Azure Key Vault configured at https://your-keyvault-name.vault.azure.net/ [Development (Testing)]
```

If you see this instead, Key Vault is disabled (normal default behavior):

```
Azure Key Vault disabled in Development. Using User Secrets and appsettings.
```

---

## Step 5: Verify Secrets Are Loaded

Check if your application is using secrets from Key Vault:

1. Try logging in with GitHub OAuth
2. Check the console output for any authentication errors
3. The app should use the secrets from Key Vault instead of User Secrets

---

## Troubleshooting

### "Unable to get token" or "Forbidden" errors

**Problem**: Your Azure account doesn't have access to the Key Vault

**Solution**:
```bash
# Add yourself as a Key Vault user
az keyvault set-policy \
  --name your-keyvault-name \
  --upn your-email@domain.com \
  --secret-permissions get list
```

### "The specified Azure Key Vault was not found"

**Problem**: The Key Vault URI is incorrect

**Solution**: Verify your Key Vault URI:
```bash
az keyvault show --name your-keyvault-name --query properties.vaultUri
```

### "Azure CLI not authenticated"

**Problem**: Not logged into Azure CLI

**Solution**:
```bash
az login
# Follow the browser authentication flow
```

### Key Vault works but secrets aren't loading

**Problem**: Secret names don't match configuration keys

**Solution**: Verify secret names use `--` instead of `:`:
```bash
# List all secrets
az keyvault secret list --vault-name your-keyvault-name --output table

# Correct format:
Authentication--GitHub--ClientId  ✓
# Wrong format:
Authentication:GitHub:ClientId    ✗
```

---

## Disabling Key Vault Testing

When you're done testing, disable Key Vault in development:

### If using appsettings.Development.json:

```json
{
  "KeyVault": {
    "EnableInDevelopment": false,
    "Uri": ""
  }
}
```

### If using User Secrets:

```bash
dotnet user-secrets set "KeyVault:EnableInDevelopment" "false"
```

Or just remove the secrets:
```bash
dotnet user-secrets remove "KeyVault:EnableInDevelopment"
dotnet user-secrets remove "KeyVault:Uri"
```

---

## How It Works

The application checks for the `KeyVault:EnableInDevelopment` flag:

```csharp
// From Program.cs
var useKeyVault = !builder.Environment.IsDevelopment() ||
                  builder.Configuration.GetValue<bool>("KeyVault:EnableInDevelopment");
```

- **Production**: Always uses Key Vault
- **Development (default)**: Uses User Secrets and appsettings
- **Development (testing)**: Uses Key Vault if `EnableInDevelopment` is true

---

## Best Practices

1. **Don't commit Key Vault URIs** - Use User Secrets for local testing
2. **Test before deploying** - Verify Key Vault integration works locally
3. **Use separate Key Vaults** - Dev Key Vault for testing, prod Key Vault for production
4. **Rotate secrets after testing** - Don't use production secrets in dev
5. **Disable after testing** - Set `EnableInDevelopment` back to false

---

## Next Steps

Once you've verified Key Vault works locally:

1. Follow `AZURE-DEPLOYMENT.md` to deploy to Azure
2. Configure production secrets in your production Key Vault
3. Set up Managed Identity for your App Service
4. Test in Azure to ensure it all works end-to-end

Happy testing! 🚀
