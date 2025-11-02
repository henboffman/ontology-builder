# Quick Start: Secure Your Eidos Application

## 🚨 DO THIS FIRST - Critical Security Issue

Your OAuth secrets are exposed in git history. Follow these 3 steps **immediately**:

### Step 1: Revoke Old Credentials

Visit each provider and regenerate your client secrets:

- **GitHub**: https://github.com/settings/developers
- **Google**: https://console.cloud.google.com/apis/credentials
- **Microsoft**: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade

### Step 2: Store New Credentials in User Secrets

```bash
# Required for GitHub login
dotnet user-secrets set "Authentication:GitHub:ClientId" "YOUR_NEW_CLIENT_ID"
dotnet user-secrets set "Authentication:GitHub:ClientSecret" "YOUR_NEW_SECRET"

# Optional providers (only if you want them)
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_NEW_SECRET"

dotnet user-secrets set "Authentication:Microsoft:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "YOUR_NEW_SECRET"
```

### Step 3: Test the Application

```bash
dotnet run
```

Visit http://localhost:5026 and try logging in with OAuth.

---

## ✅ What's Been Implemented

### Security Features Now Active

1. **Rate Limiting**
   - 100 requests/minute general limit
   - 5 login attempts per 5 minutes
   - 3 registrations per hour
   - 10 OAuth attempts per 5 minutes

2. **Security Headers**
   - Content Security Policy (CSP)
   - X-Frame-Options (clickjacking protection)
   - X-Content-Type-Options (MIME sniffing protection)
   - X-XSS-Protection
   - Referrer-Policy
   - Permissions-Policy

3. **Secure Credential Management**
   - User Secrets for development (secrets stored outside git)
   - Production ready for environment variables

4. **Security Event Logging**
   - Login successes/failures
   - Account lockouts
   - Password changes
   - OAuth events
   - Rate limit violations
   - Unauthorized access attempts

---

## 📋 Before Going to Production

1. **Clean Git History**
   ```bash
   # See SECURITY-SETUP.md for full instructions
   git filter-repo --path appsettings.Development.json --invert-paths
   git push origin --force --all
   ```

2. **Set Production Environment Variables**
   ```bash
   export Authentication__GitHub__ClientSecret="your_production_secret"
   # Or use Azure Key Vault, AWS Secrets Manager, etc.
   ```

3. **Update AllowedHosts**
   - Edit `appsettings.Production.json`
   - Change `"AllowedHosts": "*"` to your domain
   - Example: `"AllowedHosts": "eidos.yourdomain.com"`

4. **Enable Email Confirmation** (recommended)
   - Set up SendGrid, AWS SES, or similar
   - Update Program.cs: `options.SignIn.RequireConfirmedEmail = true;`

---

## 🔍 Testing Security Features

### Test Rate Limiting

Try making multiple requests:
```bash
# Should get blocked after 5 attempts
for i in {1..10}; do
  curl -X POST http://localhost:5026/Account/Login \
    -d "Input.Email=test@test.com&Input.Password=wrong"
done
```

### Check Security Headers

```bash
curl -I http://localhost:5026
```

Look for these headers in the response:
- Content-Security-Policy
- X-Frame-Options: DENY
- X-Content-Type-Options: nosniff
- X-XSS-Protection: 1; mode=block

### Monitor Security Events

Check your application logs for security events:
```bash
# Logs will show login attempts, lockouts, etc.
dotnet run | grep -i "login\|lockout\|failed"
```

---

## 📚 Full Documentation

For complete details, see:
- **SECURITY-SETUP.md** - Comprehensive security guide
- **appsettings.json** - Rate limiting configuration
- **Program.cs** - Security middleware setup

---

## 🆘 Having Issues?

### Application won't start?
- Make sure you've set GitHub OAuth credentials in User Secrets
- Check that secrets are stored: `dotnet user-secrets list`

### Rate limiting too strict?
- Edit `appsettings.json` → `IpRateLimiting` section
- Increase `Limit` values for testing

### Need to reset everything?
```bash
dotnet user-secrets clear
# Then set your secrets again
```

---

## Summary

**What you need to do now:**
1. ✅ Revoke old OAuth secrets (GitHub, Google, Microsoft)
2. ✅ Generate new OAuth secrets
3. ✅ Store new secrets using `dotnet user-secrets set`
4. ✅ Test the application with `dotnet run`

**Before production:**
1. Clean git history
2. Set up production secrets (environment variables or Key Vault)
3. Update AllowedHosts to your domain
4. Consider enabling email confirmation

Your application now has enterprise-grade security! 🔒
