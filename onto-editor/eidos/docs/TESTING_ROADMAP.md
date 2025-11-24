# Eidos Testing Roadmap

## Executive Summary

This roadmap outlines a phased approach to implementing comprehensive automated testing for the Eidos Ontology Builder. Tests are prioritized by **business value**, **risk mitigation**, and **ease of implementation**.

---

## Phase 1: Foundation & Critical Security (Weeks 1-2)

**Goal:** Establish basic testing infrastructure and address critical security concerns.

### 1.1 Security Fundamentals (HIGH PRIORITY)
**Why First:** Security vulnerabilities pose the highest risk to users and the application.

- [ ] **HTTPS Enforcement Test**
  - **Tool:** Simple C# integration test using `HttpClient`
  - **Checks:** All URLs redirect to HTTPS, no mixed content
  - **Effort:** 1-2 hours
  - **Value:** Critical - prevents man-in-the-middle attacks

- [ ] **Security Headers Validation**
  - **Tool:** C# integration test with `HttpClient`
  - **Headers to validate:**
    - `Strict-Transport-Security` (HSTS)
    - `X-Content-Type-Options: nosniff`
    - `X-Frame-Options: SAMEORIGIN` or `DENY`
    - `Content-Security-Policy` (CSP)
    - `Referrer-Policy`
  - **Effort:** 2-3 hours
  - **Value:** High - prevents XSS, clickjacking, MIME attacks

- [ ] **Cookie Security Test**
  - **Tool:** Playwright with cookie inspection
  - **Checks:** `HttpOnly`, `Secure`, `SameSite=Strict/Lax` on auth cookies
  - **Effort:** 2-3 hours
  - **Value:** High - prevents session hijacking

- [ ] **Authentication Flow Tests**
  - **Tool:** C# integration tests with `WebApplicationFactory`
  - **Checks:**
    - Unauthenticated users redirected to login
    - Protected routes require authentication
    - OAuth flows complete successfully
  - **Effort:** 4-6 hours
  - **Value:** Critical - core security mechanism

### 1.2 Basic E2E Test Infrastructure (HIGH PRIORITY)
**Why:** Enables all subsequent automated UI testing.

- [ ] **Playwright Setup**
  - **Tool:** Playwright for .NET or Node.js
  - **Setup:**
    - Install Playwright
    - Configure test project structure
    - Add basic page object models
  - **Effort:** 3-4 hours
  - **Value:** High - foundation for all E2E tests

- [ ] **Smoke Tests**
  - **Tests:**
    - Home page loads successfully
    - User can log in
    - User can create an ontology
    - User can add a concept
  - **Effort:** 4-6 hours
  - **Value:** High - validates critical user paths

**Phase 1 Deliverables:**
- Security test suite (8-12 tests)
- Basic E2E infrastructure
- 4-5 smoke tests covering happy paths
- CI/CD integration (GitHub Actions)

**Total Effort:** 16-24 hours (2-3 days)

---

## Phase 2: Performance & Accessibility (Weeks 3-4)

**Goal:** Ensure application is fast, responsive, and usable by all users.

### 2.1 Performance Monitoring (HIGH PRIORITY)
**Why:** Blazor Server performance directly impacts user experience.

- [ ] **Lighthouse CI Integration**
  - **Tool:** Lighthouse CI + GitHub Actions
  - **Metrics:**
    - Performance score > 80
    - First Contentful Paint (FCP) < 1.8s
    - Largest Contentful Paint (LCP) < 2.5s
    - Cumulative Layout Shift (CLS) < 0.1
    - Total Blocking Time (TBT) < 200ms
  - **Effort:** 4-6 hours
  - **Value:** High - automated performance regression detection

- [ ] **SignalR Connection Performance**
  - **Tool:** Custom C# performance tests
  - **Checks:**
    - SignalR connection establishment time < 500ms
    - Real-time updates latency < 100ms
    - Connection resilience under load
  - **Effort:** 6-8 hours
  - **Value:** High - critical for collaborative features

- [ ] **Page Load Performance Tests**
  - **Tool:** Playwright with Performance API
  - **Metrics per page:**
    - Time to Interactive (TTI)
    - Resource count and total size
    - Blazor circuit establishment time
  - **Effort:** 4-6 hours
  - **Value:** Medium-High - identifies bottlenecks

### 2.2 Core Web Vitals Monitoring (MEDIUM PRIORITY)
- [ ] **Automated CWV Tracking**
  - **Tool:** Playwright + web-vitals library
  - **Metrics:** LCP, INP, CLS for key pages
  - **Effort:** 3-4 hours
  - **Value:** Medium - important for UX, less critical than security

### 2.3 Accessibility (a11y) Testing (HIGH PRIORITY)
**Why:** Legal compliance and ethical obligation.

- [ ] **Lighthouse Accessibility Audit**
  - **Tool:** Lighthouse CI
  - **Target:** Score > 90 on all pages
  - **Effort:** 1-2 hours (config only)
  - **Value:** High - catches common issues automatically

- [ ] **Axe-Core Integration**
  - **Tool:** Playwright-axe or Deque Axe DevTools
  - **Checks:**
    - WCAG 2.1 Level AA compliance
    - Keyboard navigation
    - Screen reader compatibility
    - Color contrast ratios
  - **Pages to test:**
    - Home page
    - Ontology editor (all view modes)
    - Login/registration
    - Settings
  - **Effort:** 8-10 hours
  - **Value:** High - comprehensive a11y coverage

- [ ] **Manual a11y Testing Checklist**
  - Tab order and focus management
  - Screen reader testing (NVDA, JAWS, VoiceOver)
  - Keyboard-only navigation
  - **Effort:** 4-6 hours
  - **Value:** Medium-High - catches issues automation misses

**Phase 2 Deliverables:**
- Lighthouse CI pipeline with performance budgets
- Accessibility test suite (15-20 tests)
- Performance baseline and monitoring
- Documented a11y compliance status

**Total Effort:** 30-42 hours (4-5 days)

---

## Phase 3: SEO & Discoverability (Week 5)

**Goal:** Optimize for search engines and improve discoverability.

### 3.1 SEO Fundamentals (MEDIUM PRIORITY)
**Why:** Helps users find Eidos through search engines.

- [ ] **Meta Tag Validation**
  - **Tool:** Playwright with DOM queries
  - **Checks per page:**
    - Unique, non-empty `<title>` (50-60 chars)
    - `<meta name="description">` (150-160 chars)
    - `<meta name="keywords">` (optional but recommended)
    - Open Graph tags for social sharing
    - Twitter Card tags
  - **Effort:** 4-6 hours
  - **Value:** Medium - important for marketing, not core functionality

- [ ] **Heading Structure Test**
  - **Checks:**
    - Exactly one `<h1>` per page
    - Logical heading hierarchy (no skipped levels)
    - Descriptive heading text
  - **Effort:** 2-3 hours
  - **Value:** Medium - helps SEO and accessibility

- [ ] **Image Alt Text Validation**
  - **Check:** All `<img>` tags have non-empty `alt` attributes
  - **Tool:** Playwright + DOM traversal
  - **Effort:** 2-3 hours
  - **Value:** Medium-High - SEO + accessibility

- [ ] **robots.txt & sitemap.xml Tests**
  - **Checks:**
    - `/robots.txt` exists and is not overly restrictive
    - `/sitemap.xml` exists and is valid XML
    - Sitemap includes all public pages
  - **Effort:** 2-3 hours
  - **Value:** Medium - helps search engine crawling

### 3.2 Structured Data (LOW-MEDIUM PRIORITY)
- [ ] **JSON-LD Validation**
  - **Tool:** Playwright + JSON parsing
  - **Checks:**
    - Valid JSON-LD structure
    - Required properties present (`@context`, `@type`)
    - Schema.org compliance (basic)
  - **Effort:** 3-4 hours
  - **Value:** Low-Medium - nice to have for rich results

**Phase 3 Deliverables:**
- SEO test suite (8-12 tests)
- SEO audit report
- Recommendations for improvements

**Total Effort:** 13-19 hours (2 days)

---

## Phase 4: Advanced Security (Week 6)

**Goal:** Comprehensive security testing and vulnerability scanning.

### 4.1 Static Application Security Testing (SAST) (MEDIUM PRIORITY)

- [ ] **Enhanced .NET Analyzers**
  - **Tool:** Microsoft.CodeAnalysis.NetAnalyzers + Roslynator
  - **Add to `.csproj`:**
    ```xml
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Roslynator.Analyzers" Version="4.12.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    ```
  - **Effort:** 2-3 hours
  - **Value:** Medium - catches code-level security issues

- [ ] **Treat Warnings as Errors**
  - **Config:** `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  - **Effort:** 4-6 hours (to fix existing warnings)
  - **Value:** High - prevents regressions

### 4.2 Dynamic Application Security Testing (DAST) (MEDIUM PRIORITY)

- [ ] **OWASP ZAP Baseline Scan**
  - **Tool:** OWASP ZAP Docker container
  - **Setup:** Automated baseline scan in CI/CD
  - **Scans:**
    - Passive scan of all pages
    - Common vulnerabilities (XSS, SQL injection, etc.)
    - Basic authentication testing
  - **Effort:** 6-8 hours
  - **Value:** Medium-High - finds runtime vulnerabilities

- [ ] **Dependency Vulnerability Scanning**
  - **Tool:** `dotnet list package --vulnerable`
  - **Integration:** GitHub Dependabot alerts
  - **Effort:** 1-2 hours
  - **Value:** High - prevents using vulnerable packages

### 4.3 Penetration Testing (LOW PRIORITY)
- [ ] **Manual pen testing** or **bug bounty program**
  - **When:** After automated testing is mature
  - **Value:** Low-Medium - expensive, infrequent

**Phase 4 Deliverables:**
- SAST integration in build pipeline
- DAST scans in CI/CD
- Security vulnerability reports
- Remediation plan for findings

**Total Effort:** 13-19 hours (2 days)

---

## Phase 5: Advanced Testing & Monitoring (Week 7+)

**Goal:** Comprehensive coverage and continuous monitoring.

### 5.1 Load & Stress Testing (MEDIUM PRIORITY)

- [ ] **SignalR Load Testing**
  - **Tool:** NBomber or k6
  - **Scenarios:**
    - 100 concurrent users editing same ontology
    - 1000+ concurrent connections
    - Message throughput testing
  - **Effort:** 8-10 hours
  - **Value:** Medium - important for scaling

- [ ] **Database Performance Testing**
  - **Tool:** Custom C# tests with EF Core
  - **Checks:**
    - Query performance (< 100ms for common queries)
    - N+1 query detection
    - Connection pool saturation
  - **Effort:** 6-8 hours
  - **Value:** Medium - prevents performance regressions

### 5.2 Visual Regression Testing (LOW PRIORITY)

- [ ] **Percy or Chromatic Integration**
  - **Tool:** Percy.io or Chromatic (Storybook)
  - **Checks:** Visual diffs on UI changes
  - **Effort:** 4-6 hours
  - **Value:** Low-Medium - catches unintended UI changes

### 5.3 Continuous Monitoring (HIGH PRIORITY)

- [ ] **Application Insights Integration**
  - **Already implemented!** ✅
  - **Enhance:**
    - Custom metrics for ontology operations
    - Performance counters
    - User flow tracking
  - **Effort:** 4-6 hours
  - **Value:** High - real-world performance data

- [ ] **Real User Monitoring (RUM)**
  - **Tool:** Application Insights JavaScript SDK
  - **Metrics:** Actual user performance, not synthetic
  - **Effort:** 3-4 hours
  - **Value:** High - sees real user experience

**Phase 5 Deliverables:**
- Load testing suite
- Visual regression testing
- Enhanced monitoring dashboards
- Performance SLAs/SLOs

**Total Effort:** 25-34 hours (3-4 days)

---

## Implementation Priorities

### Must Have (Do First)
1. ✅ **Security headers & HTTPS** - Protects users NOW
2. ✅ **Authentication tests** - Validates core security
3. ✅ **Basic E2E smoke tests** - Prevents critical breaks
4. ✅ **Accessibility audits** - Legal/ethical obligation
5. ✅ **Lighthouse CI** - Automated performance monitoring

### Should Have (Do Soon)
6. **Axe-core integration** - Comprehensive a11y
7. **Performance tests** - User experience
8. **SEO validation** - Discoverability
9. **OWASP ZAP scans** - Security depth
10. **Dependency scanning** - Supply chain security

### Nice to Have (Do Later)
11. Load testing - Scaling concerns
12. Visual regression - UI consistency
13. Structured data validation - Rich SEO
14. Manual penetration testing - Expensive

---

## Test Pyramid for Eidos

```
        /\
       /  \      E2E Tests (10%)
      /    \     - Playwright critical paths
     /------\    - Smoke tests
    /        \
   /  AVOID   \  Integration Tests (20%)
  /   HEAVY   \ - API endpoint tests
 /   E2E BIAS \ - SignalR hub tests
/--------------\ - Database integration
/              \
/                \ Unit Tests (70%)
/   FAST & CHEAP \ - Services (ConceptService, etc.)
/------------------\ - Business logic
|  UNIT TESTS FOUNDATION | - Utilities & helpers
```

**Rationale:**
- **70% Unit Tests:** Fast, cheap, test business logic in isolation
- **20% Integration Tests:** Test component interactions (DB, SignalR, APIs)
- **10% E2E Tests:** Cover critical user journeys only (expensive, slow, flaky)

---

## CI/CD Integration

### GitHub Actions Pipeline (`.github/workflows/test.yml`)

```yaml
name: Test Suite

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore --warningsAsErrors
      - name: Run unit tests
        run: dotnet test --no-build --verbosity normal

  security-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run security header tests
        run: dotnet test --filter Category=Security
      - name: Dependency vulnerability scan
        run: dotnet list package --vulnerable --include-transitive

  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      - name: Install Playwright
        run: npx playwright install --with-deps
      - name: Run E2E tests
        run: npx playwright test

  lighthouse:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run Lighthouse CI
        uses: treosh/lighthouse-ci-action@v10
        with:
          urls: |
            http://localhost:5000
            http://localhost:5000/ontology/1
          uploadArtifacts: true
          temporaryPublicStorage: true

  accessibility:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run Axe-core tests
        run: npx playwright test --grep @a11y
```

---

## Success Metrics

### Phase 1 (Foundation)
- [ ] 100% of critical security tests passing
- [ ] HTTPS enforced on all pages
- [ ] Secure cookie attributes validated
- [ ] 5+ smoke tests covering happy paths

### Phase 2 (Performance & a11y)
- [ ] Lighthouse performance score > 80
- [ ] Lighthouse accessibility score > 90
- [ ] LCP < 2.5s, CLS < 0.1
- [ ] Zero critical a11y violations

### Phase 3 (SEO)
- [ ] All pages have unique titles/descriptions
- [ ] robots.txt and sitemap.xml present
- [ ] 100% image alt text coverage

### Phase 4 (Security)
- [ ] OWASP ZAP baseline scan clean
- [ ] Zero high/critical vulnerabilities in dependencies
- [ ] Warnings treated as errors

### Phase 5 (Advanced)
- [ ] Load tests pass at 100 concurrent users
- [ ] Real user monitoring active
- [ ] Performance budgets enforced

---

## Estimated Total Effort

| Phase | Hours | Days |
|-------|-------|------|
| Phase 1: Foundation & Security | 16-24 | 2-3 |
| Phase 2: Performance & a11y | 30-42 | 4-5 |
| Phase 3: SEO | 13-19 | 2 |
| Phase 4: Advanced Security | 13-19 | 2 |
| Phase 5: Advanced Testing | 25-34 | 3-4 |
| **Total** | **97-138** | **13-18** |

**Note:** These are development effort estimates. Actual calendar time will be longer due to test maintenance, addressing findings, and iteration.

---

## Maintenance & Iteration

### Ongoing (Weekly)
- Review test failures and flakiness
- Update tests for new features
- Monitor performance trends

### Monthly
- Review security scan results
- Update dependencies
- Lighthouse audit review

### Quarterly
- Manual accessibility testing
- Load testing review
- Update testing strategy based on learnings

---

## Tools & Technologies

### Testing Frameworks
- **Playwright** - E2E testing (cross-browser)
- **xUnit** - .NET unit/integration tests
- **bUnit** - Blazor component testing

### Performance
- **Lighthouse CI** - Automated performance audits
- **Application Insights** - Real user monitoring (already integrated)
- **NBomber** or **k6** - Load testing

### Security
- **OWASP ZAP** - DAST scanning
- **Microsoft.CodeAnalysis.NetAnalyzers** - SAST
- **Dependabot** - Dependency scanning

### Accessibility
- **Axe-core** - Automated a11y testing
- **Pa11y** or **Lighthouse** - Additional a11y audits

### SEO
- **Playwright** - Meta tag validation
- **Google Search Console** - Real-world SEO monitoring

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Test flakiness | Low test confidence | Use stable selectors, add waits, retry logic |
| Slow E2E tests | CI/CD bottleneck | Run in parallel, limit to critical paths |
| False positives | Developer frustration | Tune thresholds, suppress known issues |
| Maintenance burden | Tests become stale | Regular reviews, delete obsolete tests |
| Cost of tools | Budget concerns | Use free tiers, open-source tools |

---

## Next Steps

1. **Review & Approve Roadmap** - Team discussion on priorities
2. **Set Up Phase 1** - Security & E2E infrastructure (this week)
3. **Establish Baselines** - Current performance/a11y scores
4. **Begin Implementation** - Start with highest priority tests
5. **Iterate & Learn** - Adjust roadmap based on findings

---

## Questions for Team Discussion

1. **What is our target Lighthouse performance score?** (80? 90?)
2. **Do we need WCAG 2.1 Level AA or AAA compliance?**
3. **Should we implement a bug bounty program?** (Phase 4 alternative)
4. **What is our load testing target?** (100 users? 1000?)
5. **Do we want visual regression testing?** (Nice to have vs. cost)
6. **What is our testing budget?** (Tools, time, infrastructure)

---

**Last Updated:** 2025-10-26
**Owner:** Development Team
**Review Schedule:** Monthly
