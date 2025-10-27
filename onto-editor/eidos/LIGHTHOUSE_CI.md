# Lighthouse CI Setup Guide

## Overview

Lighthouse CI is configured to automatically test performance, accessibility, best practices, and SEO for Eidos. It runs on every push to `main`/`develop`, pull requests, and weekly to track performance trends.

---

## Quick Start

### Local Testing

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Start your application:**
   ```bash
   dotnet run
   ```

3. **In another terminal, run Lighthouse:**
   ```bash
   # Desktop audit
   npm run lighthouse

   # Mobile audit
   npm run lighthouse:mobile
   ```

4. **View results:**
   - HTML reports: `.lighthouseci/lhr-*.html`
   - JSON data: `.lighthouseci/lhr-*.json`
   - Console output shows pass/fail for each assertion

---

## Configuration

### Performance Budgets

#### Desktop (`lighthouserc.js`)
- **Performance Score:** ≥ 80%
- **Accessibility Score:** ≥ 90%
- **Best Practices Score:** ≥ 90%
- **SEO Score:** ≥ 85%

**Core Web Vitals:**
- First Contentful Paint (FCP): ≤ 1.8s
- Largest Contentful Paint (LCP): ≤ 2.5s
- Cumulative Layout Shift (CLS): ≤ 0.1
- Total Blocking Time (TBT): ≤ 200ms
- Speed Index: ≤ 3.4s
- Time to Interactive (TTI): ≤ 3.8s

**Resource Budgets:**
- JavaScript: ≤ 500 KB
- CSS: ≤ 150 KB
- Images: ≤ 1 MB
- Total Page Size: ≤ 3 MB

#### Mobile (`lighthouserc.mobile.js`)
- **Performance Score:** ≥ 70% (more lenient due to throttling)
- **Accessibility/Best Practices/SEO:** Same as desktop

**Core Web Vitals (Mobile):**
- FCP: ≤ 2.5s
- LCP: ≤ 4.0s
- CLS: ≤ 0.1
- TBT: ≤ 300ms
- TTI: ≤ 5.0s

---

## GitHub Actions Integration

### Automatic Runs

Lighthouse CI runs automatically:
- ✅ On every push to `main` or `develop`
- ✅ On every pull request
- ✅ Weekly (Sunday midnight) for trend monitoring
- ✅ Manual trigger via "Actions" tab

### Viewing Results

1. **In GitHub Actions:**
   - Go to the "Actions" tab
   - Click on "Lighthouse CI" workflow
   - View the run results

2. **Download Artifacts:**
   - Scroll to the bottom of the workflow run
   - Download `lighthouse-results-desktop` or `lighthouse-results-mobile`
   - Unzip and open `.html` files in browser

3. **PR Comments:**
   - Lighthouse automatically comments on PRs with score summaries
   - Includes links to full reports

---

## Understanding Results

### Scores

| Score | Meaning |
|-------|---------|
| 90-100 | ✅ Excellent |
| 50-89 | ⚠️ Needs Improvement |
| 0-49 | ❌ Poor |

### Core Web Vitals

| Metric | Good | Needs Improvement | Poor |
|--------|------|-------------------|------|
| **LCP** | ≤ 2.5s | 2.5s - 4.0s | > 4.0s |
| **FCP** | ≤ 1.8s | 1.8s - 3.0s | > 3.0s |
| **CLS** | ≤ 0.1 | 0.1 - 0.25 | > 0.25 |
| **TBT** | ≤ 200ms | 200ms - 600ms | > 600ms |

### Common Issues & Fixes

#### Performance Issues

**Problem:** Large JavaScript bundle size
- **Fix:** Enable code splitting, lazy loading
- **Check:** `resource-summary:script:size`

**Problem:** Slow Time to Interactive (TTI)
- **Fix:** Minimize main thread work, defer non-critical JS
- **Check:** `interactive` metric

**Problem:** Large images
- **Fix:** Compress images, use WebP format, lazy load
- **Check:** `resource-summary:image:size`

**Problem:** No HTTP/2
- **Fix:** Ensure server supports HTTP/2
- **Check:** `uses-http2`

#### Accessibility Issues

**Problem:** Missing alt text
- **Fix:** Add `alt` attributes to all `<img>` tags
- **Check:** `image-alt` assertion

**Problem:** Low color contrast
- **Fix:** Increase contrast ratio to at least 4.5:1
- **Check:** `color-contrast` assertion

**Problem:** Missing form labels
- **Fix:** Add `<label>` elements or `aria-label`
- **Check:** `label` assertion

#### SEO Issues

**Problem:** Missing meta description
- **Fix:** Add `<meta name="description">` to each page
- **Check:** `meta-description` assertion

**Problem:** Missing page title
- **Fix:** Add unique `<title>` to each page
- **Check:** `document-title` assertion

---

## Customizing Configuration

### Adding URLs to Test

Edit `lighthouserc.js`:

```javascript
url: [
  'http://localhost:5000',
  'http://localhost:5000/your-new-page',
],
```

### Adjusting Budgets

To make budgets more/less strict:

```javascript
assertions: {
  'categories:performance': ['error', { minScore: 0.85 }],  // Raise to 85%
  'largest-contentful-paint': ['error', { maxNumericValue: 3000 }],  // Allow 3s
},
```

### Testing Authenticated Pages

For pages requiring login, you need to:

1. **Option A: Use a test account**
   ```javascript
   collect: {
     settings: {
       extraHeaders: JSON.stringify({
         'Cookie': 'your-auth-cookie-here'
       }),
     },
   },
   ```

2. **Option B: Use Puppeteer script**
   ```javascript
   collect: {
     startServerCommand: 'npm run start',
     puppeteerScript: './lighthouse-auth.js',  // Custom login script
   },
   ```

   Create `lighthouse-auth.js`:
   ```javascript
   module.exports = async (browser, context) => {
     const page = await browser.newPage();
     await page.goto('http://localhost:5000/Account/Login');
     await page.type('#email', 'test@example.com');
     await page.type('#password', 'TestPassword123!');
     await page.click('button[type="submit"]');
     await page.waitForNavigation();
   };
   ```

---

## Continuous Monitoring

### Tracking Performance Over Time

1. **Weekly Runs:** Automatically run every Sunday
2. **Trend Analysis:** Compare scores week-over-week
3. **Regression Detection:** CI fails if scores drop below budgets

### Setting Up LHCI Server (Optional)

For historical data storage:

1. **Deploy LHCI Server:**
   ```bash
   docker run -p 9001:9001 -v lhci-data:/data patrickhulce/lhci-server
   ```

2. **Update configuration:**
   ```javascript
   upload: {
     target: 'lhci',
     serverBaseUrl: 'https://your-lhci-server.com',
     token: process.env.LHCI_TOKEN,
   },
   ```

3. **Set GitHub Secret:**
   - Go to Settings → Secrets → Actions
   - Add `LHCI_TOKEN` with your server token

---

## Troubleshooting

### Application Won't Start

**Problem:** Lighthouse can't connect to `http://localhost:5000`

**Solutions:**
- Check if port 5000 is already in use: `lsof -i :5000`
- Verify application starts: `dotnet run` manually
- Check `ASPNETCORE_URLS` environment variable

### Tests Fail Locally But Pass in CI

**Problem:** Different environments produce different results

**Solutions:**
- Clear browser cache and restart browser
- Use incognito/private mode
- Check Node.js version matches CI (20.x)
- Disable browser extensions

### Performance Scores Vary Between Runs

**Problem:** Lighthouse scores fluctuate ±5 points

**Solutions:**
- Run multiple times (config uses `numberOfRuns: 3` for median)
- Close other applications to free CPU/memory
- Use consistent network conditions
- Consider using `throttling` settings

### Out of Memory Errors

**Problem:** Lighthouse crashes with OOM

**Solutions:**
- Increase Node.js memory: `NODE_OPTIONS=--max-old-space-size=4096 npm run lighthouse`
- Test fewer URLs at once
- Reduce `numberOfRuns` from 3 to 1

---

## Best Practices

### Before Committing

1. Run Lighthouse locally: `npm run lighthouse`
2. Fix any failing assertions
3. Check HTML reports for opportunities
4. Test both desktop and mobile

### Monitoring

- Review weekly Lighthouse results
- Investigate sudden score drops
- Track Core Web Vitals trends
- Monitor resource sizes

### Budgets

- **Start lenient**, tighten over time
- **Don't obsess** over 100/100 scores
- **Focus on** Core Web Vitals first
- **Prioritize** user-facing pages

---

## Resources

- [Lighthouse Documentation](https://developers.google.com/web/tools/lighthouse)
- [Lighthouse CI GitHub](https://github.com/GoogleChrome/lighthouse-ci)
- [Core Web Vitals Guide](https://web.dev/vitals/)
- [Performance Budgets](https://web.dev/performance-budgets-101/)
- [Accessibility Testing](https://web.dev/accessibility/)

---

## FAQ

**Q: Why do mobile scores differ from desktop?**
A: Mobile uses CPU throttling and slower network simulation to match real-world mobile devices.

**Q: Should I aim for 100/100 on all metrics?**
A: No. Aim for 80+ performance, 90+ accessibility. Focus on real user experience.

**Q: How often should I run Lighthouse?**
A: Automatically on every PR + weekly. Manually when optimizing.

**Q: Can I test production instead of localhost?**
A: Yes! Change URLs in `lighthouserc.js` to `https://eidosonto.com`.

**Q: What if Blazor Server startup is slow?**
A: This is expected. Focus on optimizing post-load interactivity and Core Web Vitals.

---

**Last Updated:** 2025-10-26
**Maintainer:** Development Team
