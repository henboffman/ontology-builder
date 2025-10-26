# Security Setup Guide for Eidos

## 🚨 CRITICAL: OAuth Secrets Have Been Exposed

Your OAuth client secrets were committed to git history. You **MUST** complete these steps before deploying to production.

---

## Immediate Actions Required

### 1. Revoke and Regenerate ALL OAuth Credentials

#### GitHub OAuth
1. Go to https://github.com/settings/developers
2. Click on your OAuth App
3. Click "Generate a new client secret"
4. **Copy the new secret immediately** (you won't be able to see it again)
5. Delete the old secret

#### Google OAuth
1. Go to https://console.cloud.google.com/apis/credentials
2. Find your OAuth 2.0 Client ID
3. Click "Reset Secret" or create a new OAuth client
4. **Copy the new secret immediately**

#### Microsoft OAuth
1. Go to https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade
2. Click on your app registration
3. Go to "Certificates & secrets"
4. Add a new client secret
5. **Copy the new secret immediately**
6. Delete the old secret

---

## Development Setup (User Secrets)

User Secrets is already initialized for this project. Now store your **NEW** OAuth credentials securely:

```bash
# GitHub OAuth (REQUIRED - app will not run without this)
dotnet user-secrets set "Authentication:GitHub:ClientId" "YOUR_NEW_GITHUB_CLIENT_ID"
dotnet user-secrets set "Authentication:GitHub:ClientSecret" "YOUR_NEW_GITHUB_SECRET"

# Google OAuth (OPTIONAL - only if you want Google login)
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_NEW_GOOGLE_SECRET"

# Microsoft OAuth (OPTIONAL - only if you want Microsoft login)
dotnet user-secrets set "Authentication:Microsoft:ClientId" "YOUR_MICROSOFT_CLIENT_ID"
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "YOUR_NEW_MICROSOFT_SECRET"
```

### View Your Stored Secrets
```bash
dotnet user-secrets list
```

### Clear All Secrets (if needed)
```bash
dotnet user-secrets clear
```

---

## Production Setup (Environment Variables)

For production deployment, use environment variables instead of User Secrets:

### Option 1: Linux/macOS Environment Variables
```bash
export Authentication__GitHub__ClientId="your_client_id"
export Authentication__GitHub__ClientSecret="your_client_secret"
export Authentication__Google__ClientId="your_client_id"
export Authentication__Google__ClientSecret="your_client_secret"
export Authentication__Microsoft__ClientId="your_client_id"
export Authentication__Microsoft__ClientSecret="your_client_secret"
```

### Option 2: Azure App Service Configuration
1. Go to Azure Portal → Your App Service
2. Navigate to "Configuration" → "Application settings"
3. Add new settings:
   - `Authentication:GitHub:ClientId`
   - `Authentication:GitHub:ClientSecret`
   - `Authentication:Google:ClientId`
   - `Authentication:Google:ClientSecret`
   - `Authentication:Microsoft:ClientId`
   - `Authentication:Microsoft:ClientSecret`

### Option 3: Azure Key Vault (Recommended for Production) ✅ CONFIGURED

**Good news**: Azure Key Vault integration is already set up in your app!

The application will automatically load secrets from Azure Key Vault in production if you configure the `KeyVault:Uri` setting.

**See the complete Azure deployment guide**: `AZURE-DEPLOYMENT.md`

**Quick start guide**: `AZURE-QUICK-START.md`

To use Azure Key Vault:
1. Add your OAuth secrets to your Key Vault (use `--` instead of `:` in secret names)
2. Enable Managed Identity on your App Service
3. Grant your App Service access to Key Vault
4. Set `KeyVault__Uri` in App Service Configuration

The app automatically uses `DefaultAzureCredential` which supports:
- Managed Identity (when running in Azure)
- Azure CLI (for local testing)
- Visual Studio / VS Code credentials
- Environment variables

---

## Clean Git History (Remove Exposed Secrets)

⚠️ **WARNING**: This rewrites git history and will affect all collaborators.

### Using BFG Repo-Cleaner (Recommended)
```bash
# Create a backup first
git branch backup-before-cleanup

# Download BFG: https://rtyley.github.io/bfg-repo-cleaner/
# Then run:
java -jar bfg.jar --delete-files appsettings.Development.json

# Clean up and push
git reflog expire --expire=now --all && git gc --prune=now --aggressive
git push origin --force --all
```

### Using git-filter-repo (Alternative)
```bash
# Install git-filter-repo
pip install git-filter-repo

# Create a backup
git branch backup-before-cleanup

# Remove the file from history
git filter-repo --path appsettings.Development.json --invert-paths

# Force push (WARNING: This affects all collaborators)
git push origin --force --all
```

---

## Security Features Implemented

### ✅ Rate Limiting
- General API: 100 requests per minute
- Login endpoint: 5 attempts per 5 minutes
- Registration: 3 attempts per hour
- OAuth login: 10 attempts per 5 minutes

Configuration in `appsettings.json` under `IpRateLimiting`.

### ✅ Security Headers
- **Content-Security-Policy**: Prevents XSS attacks while allowing required CDNs
  - Scripts allowed from: `'self'`, `cdn.jsdelivr.net`, `unpkg.com` (for Cytoscape)
  - Images allowed from: `'self'`, `data:`, `https:`, `blob:` (for graph exports)
  - Styles allowed from: `'self'`, `cdn.jsdelivr.net`
  - `'unsafe-inline'` and `'unsafe-eval'` enabled for Blazor functionality
- **X-Content-Type-Options**: Prevents MIME sniffing
- **X-Frame-Options**: Prevents clickjacking
- **X-XSS-Protection**: Browser-level XSS protection
- **Referrer-Policy**: Controls referrer information
- **Permissions-Policy**: Limits browser features

### ✅ HTTPS Enforcement
- Automatic HTTP to HTTPS redirection
- HSTS headers (30 days) in production

### ✅ Strong Password Requirements
- Minimum 8 characters
- Requires uppercase, lowercase, number, special character
- Minimum 4 unique characters

### ✅ Account Lockout
- 5 failed login attempts → 15 minute lockout
- Prevents brute force attacks

### ✅ CSRF Protection
- Anti-forgery tokens automatically applied by Blazor
- `UseAntiforgery()` middleware enabled

### ✅ Secure Cookies
- HttpOnly (prevents JavaScript access)
- Secure (HTTPS-only in production)
- SameSite=Lax (CSRF protection)

---

## Pre-Deployment Checklist

Before deploying to production:

- [ ] Revoked all exposed OAuth credentials
- [ ] Generated new OAuth credentials
- [ ] Stored new credentials in User Secrets (development)
- [ ] Configured environment variables or Key Vault (production)
- [ ] Cleaned exposed secrets from git history
- [ ] Tested login with new credentials
- [ ] Verified `appsettings.Development.json` is in `.gitignore`
- [ ] Set `AllowedHosts` to specific domains (currently `*`)
- [ ] Configured email service for email confirmation
- [ ] Set `RequireConfirmedEmail = true` in production
- [ ] Tested rate limiting (try exceeding limits)
- [ ] Verified security headers in browser dev tools
- [ ] Ensured HTTPS is properly configured
- [ ] Set up application logging/monitoring
- [ ] Configured database backups
- [ ] Reviewed error handling (no sensitive info leaked)

---

## Testing Security Features

### Test Rate Limiting
```bash
# Test login rate limit (should block after 5 attempts)
for i in {1..10}; do
  curl -X POST http://localhost:5026/Account/Login \
    -d "Input.Email=test@test.com&Input.Password=wrong"
done
```

### Test Security Headers
```bash
curl -I https://yourdomain.com
# Look for: Content-Security-Policy, X-Frame-Options, etc.
```

### Test HTTPS Redirect
```bash
curl -I http://yourdomain.com
# Should return 307 redirect to https://
```

---

## Additional Security Recommendations

### Short-term (Next Sprint)
1. **Email Verification**
   - Set up SendGrid, AWS SES, or similar
   - Enable email confirmation before login
   - Add "forgot password" flow

2. **Logging & Monitoring**
   - Set up Application Insights or Serilog
   - Log security events (failed logins, lockouts, etc.)
   - Set up alerts for suspicious activity

3. **Database Security**
   - Ensure production database uses strong password
   - Enable database encryption at rest
   - Regular automated backups

### Medium-term (Future Releases)
1. **Two-Factor Authentication (2FA)**
2. **Account activity log** (login history, IP addresses)
3. **Session management dashboard**
4. **API keys for programmatic access**

### Long-term (As Product Grows)
1. **Regular security audits**
2. **Penetration testing**
3. **Bug bounty program**
4. **Compliance certifications** (SOC 2, ISO 27001, etc.)

---

## Support & Questions

If you need help with security setup:
- Review ASP.NET Core security docs: https://learn.microsoft.com/en-us/aspnet/core/security/
- Check OAuth provider documentation
- Review this project's security audit report

**Remember**: Security is an ongoing process, not a one-time setup!
