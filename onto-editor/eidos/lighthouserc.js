module.exports = {
  ci: {
    collect: {
      // URLs to test - adjust these to match your deployment
      url: [
        'https://localhost:7216',                    // Home page
        'https://localhost:7216/features',           // Features page
        'https://localhost:7216/learn',              // Learn page
        'https://localhost:7216/documentation',      // Documentation page
        'https://localhost:7216/Account/Login',      // Login page
        // Note: Authenticated pages require additional setup
        // 'https://localhost:7216/ontology/1',      // Ontology editor (requires auth)
      ],
      numberOfRuns: 3, // Run 3 times and take median
      chromePath: require('puppeteer').executablePath(), // Use Puppeteer's bundled Chrome
      settings: {
        preset: 'desktop',
        // Throttling settings (simulated 4G)
        throttling: {
          rttMs: 40,
          throughputKbps: 10240,
          requestLatencyMs: 0,
          downloadThroughputKbps: 10240,
          uploadThroughputKbps: 5120,
        },
        // Screen emulation
        screenEmulation: {
          mobile: false,
          width: 1350,
          height: 940,
          deviceScaleFactor: 1,
          disabled: false,
        },
        // Form factor
        formFactor: 'desktop',
        // Additional options
        onlyCategories: ['performance', 'accessibility', 'best-practices', 'seo'],
      },
    },
    assert: {
      preset: 'lighthouse:recommended',
      assertions: {
        // Performance Budgets
        'categories:performance': ['error', { minScore: 0.8 }],  // 80% minimum
        'categories:accessibility': ['error', { minScore: 0.9 }], // 90% minimum
        'categories:best-practices': ['error', { minScore: 0.9 }],
        'categories:seo': ['error', { minScore: 0.85 }],

        // Core Web Vitals
        'first-contentful-paint': ['error', { maxNumericValue: 1800 }],        // 1.8s
        'largest-contentful-paint': ['error', { maxNumericValue: 2500 }],      // 2.5s
        'cumulative-layout-shift': ['error', { maxNumericValue: 0.1 }],        // 0.1
        'total-blocking-time': ['error', { maxNumericValue: 200 }],            // 200ms
        'speed-index': ['error', { maxNumericValue: 3400 }],                   // 3.4s
        'interactive': ['error', { maxNumericValue: 3800 }],                   // 3.8s (TTI)

        // Resource Budgets
        'resource-summary:script:size': ['warn', { maxNumericValue: 500000 }],  // 500KB scripts
        'resource-summary:stylesheet:size': ['warn', { maxNumericValue: 150000 }], // 150KB CSS
        'resource-summary:image:size': ['warn', { maxNumericValue: 1000000 }],  // 1MB images
        'resource-summary:total:size': ['warn', { maxNumericValue: 3000000 }],  // 3MB total

        // Best Practices
        'uses-http2': 'error',
        'uses-passive-listeners': 'warn',
        'no-document-write': 'error',
        'uses-text-compression': 'error',
        'efficient-animated-content': 'warn',

        // Accessibility (Critical)
        'color-contrast': 'error',
        'image-alt': 'error',
        'label': 'error',
        'html-has-lang': 'error',
        'valid-lang': 'error',
        'aria-roles': 'error',
        'button-name': 'error',
        'link-name': 'error',

        // SEO
        'document-title': 'error',
        'meta-description': 'error',
        'crawlable-anchors': 'warn',
        'robots-txt': 'warn',
      },
    },
    upload: {
      target: 'temporary-public-storage', // Temporary storage for GitHub Actions
      // For private storage, use: target: 'lhci', serverBaseUrl: 'your-server'
    },
  },
};
