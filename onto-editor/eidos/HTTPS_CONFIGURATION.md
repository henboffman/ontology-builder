# HTTPS Configuration

**Implemented:** 2025-10-25

## Overview

The application is now configured to **only use HTTPS** in all environments (development and production). HTTP connections are automatically redirected to HTTPS, and all security features enforce HTTPS-only access.

---

## Changes Made

### 1. Launch Settings (`Properties/launchSettings.json`)

**Changes:**
- ✅ Removed HTTP profile
- ✅ HTTPS profile now only uses `https://localhost:7216`
- ✅ No HTTP fallback port

**Before:**
```json
"http": {
  "applicationUrl": "http://localhost:5026"
},
"https": {
  "applicationUrl": "https://localhost:7216;http://localhost:5026"
}
```

**After:**
```json
"https": {
  "applicationUrl": "https://localhost:7216"
}
```

### 2. Program.cs Security Configuration

**HSTS (HTTP Strict Transport Security):**
- ✅ Enabled in ALL environments (including development)
- ✅ Forces browsers to only use HTTPS for your domain
- **Line 350**: `app.UseHsts();` - no longer conditional

**Cookie Security:**
- ✅ Application cookies: `CookieSecurePolicy.Always` (Line 234)
- ✅ External OAuth cookies: `CookieSecurePolicy.Always` (Line 247)
- ✅ GitHub OAuth correlation cookies: `CookieSecurePolicy.Always` (Line 154)
- ✅ Google OAuth correlation cookies: `CookieSecurePolicy.Always` (Line 175)
- ✅ Microsoft OAuth correlation cookies: `CookieSecurePolicy.Always` (Line 197)

**HTTPS Redirection:**
- ✅ `app.UseHttpsRedirection();` on Line 382
- ✅ Redirects all HTTP requests to HTTPS

### 3. Development Certificate

**Status:** ✅ Trusted
- Certificate valid until: **2026-10-04**
- Certificate for: `localhost`
- Auto-trusted by .NET development tools

---

## URLs Updated

| Environment | Old URL | New URL |
|-------------|---------|---------|
| Development | http://localhost:5026 | https://localhost:7216 |
| Production | (varies) | HTTPS only |

---

## OAuth Provider Configuration

**⚠️ IMPORTANT:** You must update your OAuth redirect URIs in each provider's settings:

### GitHub OAuth App
1. Go to: https://github.com/settings/developers
2. Select your OAuth app
3. Update redirect URIs:
   - **Old:** `http://localhost:5026/signin-github`
   - **New:** `https://localhost:7216/signin-github`
4. Save changes

### Google OAuth App (if configured)
1. Go to: https://console.cloud.google.com/apis/credentials
2. Select your OAuth 2.0 Client ID
3. Update redirect URIs:
   - **Old:** `http://localhost:5026/signin-google`
   - **New:** `https://localhost:7216/signin-google`
4. Save changes

### Microsoft OAuth App (if configured)
1. Go to: https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade
2. Select your app registration
3. Go to Authentication → Redirect URIs
4. Update redirect URIs:
   - **Old:** `http://localhost:5026/signin-microsoft`
   - **New:** `https://localhost:7216/signin-microsoft`
5. Save changes

---

## Testing Checklist

- [ ] Start application: `dotnet run`
- [ ] Browser opens at `https://localhost:7216`
- [ ] No certificate warnings (certificate is trusted)
- [ ] OAuth login works (GitHub, Google, Microsoft)
- [ ] All pages load over HTTPS
- [ ] HTTP requests (if any) redirect to HTTPS

---

## Security Benefits

1. **✅ Encrypted Traffic** - All data transmitted between browser and server is encrypted
2. **✅ MITM Protection** - Prevents man-in-the-middle attacks
3. **✅ Browser Trust** - Modern browsers show security indicators for HTTPS sites
4. **✅ OAuth Security** - OAuth providers require HTTPS for security
5. **✅ Cookie Protection** - Cookies are only sent over secure connections
6. **✅ HSTS Enforcement** - Browsers will refuse HTTP connections after first HTTPS visit
7. **✅ Production Ready** - Same security configuration in development and production

---

## Troubleshooting

### Certificate Not Trusted
If you see certificate warnings:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Port Already in Use
If port 7216 is in use, update `launchSettings.json`:
```json
"applicationUrl": "https://localhost:YOUR_PORT"
```

### OAuth Redirect Mismatch
Error: `redirect_uri_mismatch`
- **Cause:** OAuth provider redirect URI doesn't match
- **Fix:** Update redirect URIs in provider settings (see above)

---

## Production Deployment

When deploying to production (Azure, AWS, etc.):

1. **Azure App Service:**
   - HTTPS is enabled by default
   - Azure handles SSL certificates automatically
   - Your app configuration already enforces HTTPS

2. **Custom Domain:**
   - Configure SSL certificate in your hosting provider
   - Update OAuth redirect URIs to your production domain
   - HSTS will be enforced automatically

3. **Environment Variables:**
   - No changes needed - configuration is environment-aware
   - OAuth secrets loaded from Azure Key Vault in production

---

## Summary

✅ **Development:** Uses `https://localhost:7216`
✅ **HTTP → HTTPS:** Automatic redirection
✅ **HSTS:** Enabled everywhere
✅ **Cookies:** HTTPS-only
✅ **OAuth:** Secure by default
✅ **Certificate:** Trusted and valid

Your application now uses HTTPS only, providing encryption, authentication, and data integrity for all connections.
