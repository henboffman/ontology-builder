/**
 * Theme Handler - Dark/Light Mode Support
 * Manages theme switching and persistence
 */

const ThemeHandler = {
    /**
     * Initialize the theme system
     * Note: This is now called from Blazor after loading user preferences
     */
    init: function() {
        // Only apply saved theme from localStorage if available
        // Otherwise, do nothing and wait for Blazor to set the theme
        const stored = localStorage.getItem('theme');
        if (stored) {
            this.applyTheme(stored);
        } else {
        }
    },

    /**
     * Get the currently active theme
     * @returns {string} 'light' or 'dark'
     */
    getTheme: function() {
        // Check localStorage first
        const stored = localStorage.getItem('theme');
        if (stored) {
            return stored;
        }

        // Check Bootstrap data-bs-theme attribute
        const docTheme = document.documentElement.getAttribute('data-bs-theme');
        if (docTheme) {
            return docTheme;
        }

        // Check system preference
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }

        // Default to light
        return 'light';
    },

    /**
     * Apply a theme to the document using Bootstrap 5.3+ data-bs-theme attribute
     * @param {string} theme - 'light' or 'dark'
     */
    applyTheme: function(theme) {

        // Validate theme
        if (theme !== 'light' && theme !== 'dark') {
            theme = 'light';
        }

        // Set expected theme for the observer to enforce
        window.expectedTheme = theme;

        // Apply to document using Bootstrap 5.3+ data-bs-theme attribute
        window.isApplying = true;
        if (theme === 'dark') {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
        } else {
            document.documentElement.setAttribute('data-bs-theme', 'light');
        }
        window.isApplying = false;

        // Verify it was set
        const actualTheme = document.documentElement.getAttribute('data-bs-theme');

        // Save to localStorage
        localStorage.setItem('theme', theme);

        // Dispatch custom event for components that need to react
        window.dispatchEvent(new CustomEvent('themeChanged', {
            detail: { theme: theme }
        }));

    },

    /**
     * Toggle between light and dark themes
     * @returns {string} The new theme
     */
    toggleTheme: function() {
        const currentTheme = this.getTheme();
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        this.applyTheme(newTheme);
        return newTheme;
    },

    /**
     * Set a specific theme
     * @param {string} theme - 'light' or 'dark'
     */
    setTheme: function(theme) {
        this.applyTheme(theme);
    },

    /**
     * Set theme with a delay to work around Blazor's rendering
     * @param {string} theme - 'light' or 'dark'
     * @param {number} delay - Delay in milliseconds (default 50)
     */
    setThemeDelayed: function(theme, delay = 50) {
        const self = this;
        setTimeout(function() {
            self.applyTheme(theme);
        }, delay);
    }
};

// Don't auto-initialize - let Blazor handle initialization after loading user preferences
// The ThemeInitializer component will call setTheme() after loading from the database
// This ensures the user's saved theme preference is applied correctly

// Export for use in Blazor
window.ThemeHandler = ThemeHandler;

// Listen for system theme changes
if (window.matchMedia) {
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function(e) {
        // Only auto-switch if user hasn't manually set a preference
        if (!localStorage.getItem('theme')) {
            ThemeHandler.applyTheme(e.matches ? 'dark' : 'light');
        } else {
        }
    });
}

// Auto-fix: Automatically reapply theme when Blazor changes it
window.expectedTheme = null;
window.isApplying = false;

const observer = new MutationObserver(function(mutations) {
    mutations.forEach(function(mutation) {
        if (mutation.type === 'attributes' && mutation.attributeName === 'data-bs-theme') {
            const currentValue = document.documentElement.getAttribute('data-bs-theme');


            // If Blazor changed our theme, reapply it instantly
            if (!window.isApplying && window.expectedTheme && currentValue !== window.expectedTheme) {
                window.isApplying = true;
                document.documentElement.setAttribute('data-bs-theme', window.expectedTheme);
                localStorage.setItem('theme', window.expectedTheme);
                window.isApplying = false;
            }
        }
    });
});

observer.observe(document.documentElement, {
    attributes: true,
    attributeFilter: ['data-bs-theme']
});

