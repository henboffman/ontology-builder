module.exports = {
  ci: {
    collect: {
      url: [
        'https://localhost:7216',
        'https://localhost:7216/features',
        'https://localhost:7216/learn',
        'https://localhost:7216/documentation',
        'https://localhost:7216/Account/Login',
      ],
      numberOfRuns: 3,
      settings: {
        preset: 'mobile', // Use mobile preset
        // Mobile throttling (simulated 4G)
        throttling: {
          rttMs: 150,
          throughputKbps: 1638.4,
          requestLatencyMs: 562.5,
          downloadThroughputKbps: 1638.4,
          uploadThroughputKbps: 675,
          cpuSlowdownMultiplier: 4, // Simulate slower mobile CPU
        },
        // Mobile screen emulation
        screenEmulation: {
          mobile: true,
          width: 375,
          height: 667,
          deviceScaleFactor: 2,
          disabled: false,
        },
        formFactor: 'mobile',
        onlyCategories: ['performance', 'accessibility', 'best-practices', 'seo'],
      },
    },
    assert: {
      preset: 'lighthouse:recommended',
      assertions: {
        // More lenient performance budgets for mobile
        'categories:performance': ['error', { minScore: 0.7 }],   // 70% for mobile
        'categories:accessibility': ['error', { minScore: 0.9 }],
        'categories:best-practices': ['error', { minScore: 0.9 }],
        'categories:seo': ['error', { minScore: 0.85 }],

        // Mobile Core Web Vitals (more lenient)
        'first-contentful-paint': ['error', { maxNumericValue: 2500 }],    // 2.5s
        'largest-contentful-paint': ['error', { maxNumericValue: 4000 }],  // 4s
        'cumulative-layout-shift': ['error', { maxNumericValue: 0.1 }],
        'total-blocking-time': ['error', { maxNumericValue: 300 }],        // 300ms
        'speed-index': ['error', { maxNumericValue: 4500 }],
        'interactive': ['error', { maxNumericValue: 5000 }],               // 5s TTI

        // Mobile-specific
        'viewport': 'error',
        'tap-targets': 'error',  // Touch targets must be large enough

        // Resource Budgets (same as desktop)
        'resource-summary:script:size': ['warn', { maxNumericValue: 500000 }],
        'resource-summary:stylesheet:size': ['warn', { maxNumericValue: 150000 }],
        'resource-summary:image:size': ['warn', { maxNumericValue: 1000000 }],
        'resource-summary:total:size': ['warn', { maxNumericValue: 3000000 }],

        // Accessibility (same as desktop)
        'color-contrast': 'error',
        'image-alt': 'error',
        'label': 'error',
        'html-has-lang': 'error',
        'button-name': 'error',
        'link-name': 'error',
      },
    },
    upload: {
      target: 'temporary-public-storage',
    },
  },
};
