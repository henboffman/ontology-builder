# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue, please report it responsibly.

### How to Report

**Please DO NOT create a public GitHub issue for security vulnerabilities.**

Instead, please report security issues via:

1. **Email**: <hoffchops@outlook.com>
2. **Subject Line**: [SECURITY] Brief description
3. **Include**:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if known)

### What to Expect

- **Acknowledgment**: Within 48 hours
- **Initial Assessment**: Within 5 business days
- **Status Updates**: Every 7 days until resolved
- **Fix Timeline**: Critical issues within 7 days, others within 30 days

### Security Measures

Eidos implements multiple security layers:

- ✅ **Authentication**: ASP.NET Core Identity with OAuth 2.0
- ✅ **HTTPS Only**: TLS 1.2+ enforced
- ✅ **CSRF Protection**: Anti-forgery tokens on all forms
- ✅ **Rate Limiting**: IP-based request throttling
- ✅ **Input Validation**: All user inputs sanitized
- ✅ **SQL Injection Prevention**: Parameterized queries only
- ✅ **XSS Prevention**: Output encoding and CSP headers
- ✅ **Dependency Scanning**: Automated weekly scans
- ✅ **Security Headers**: HSTS, X-Frame-Options, X-Content-Type-Options
- ✅ **Logging & Monitoring**: Application Insights telemetry

### Automated Security Testing

Our CI/CD pipeline includes:

- OWASP Dependency Check (weekly)
- Security Code Scan (every commit)
- OWASP ZAP baseline scans (weekly)
- GitHub Dependabot alerts

### Responsible Disclosure

We follow responsible disclosure practices:

1. Report received → Investigation begins
2. Fix developed → Security advisory drafted
3. Patch released → Public disclosure (if appropriate)
4. Credit given to reporter (if desired)

## Security Best Practices for Contributors

If you're contributing to Eidos:

1. **Never commit secrets** - Use user-secrets or Azure Key Vault
2. **Validate all inputs** - Assume all user data is malicious
3. **Use parameterized queries** - Never concatenate SQL
4. **Sanitize outputs** - Prevent XSS attacks
5. **Follow principle of least privilege** - Only request necessary permissions
6. **Keep dependencies updated** - Review Dependabot PRs promptly

## Security Hall of Fame

We appreciate security researchers who help keep Eidos secure. Reporters will be listed here (with permission):

_No vulnerabilities reported yet._
