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
            console.log('Theme handler initialized from localStorage:', stored);
        } else {
            console.log('Theme handler ready, waiting for Blazor initialization');
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

        // Check document attribute
        const docTheme = document.documentElement.getAttribute('data-theme');
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
     * Apply a theme to the document
     * @param {string} theme - 'light' or 'dark'
     */
    applyTheme: function(theme) {
        console.log('[ThemeHandler.applyTheme] Called with theme:', theme);

        // Validate theme
        if (theme !== 'light' && theme !== 'dark') {
            console.log('[ThemeHandler.applyTheme] Invalid theme, defaulting to light');
            theme = 'light';
        }

        // Set expected theme for the observer to enforce
        window.expectedTheme = theme;

        // Apply to document
        window.isApplying = true;
        if (theme === 'dark') {
            console.log('[ThemeHandler.applyTheme] Setting data-theme="dark"');
            document.documentElement.setAttribute('data-theme', 'dark');
        } else {
            console.log('[ThemeHandler.applyTheme] Removing data-theme (light mode)');
            document.documentElement.removeAttribute('data-theme');
        }
        window.isApplying = false;

        // Verify it was set
        const actualTheme = document.documentElement.getAttribute('data-theme');
        console.log('[ThemeHandler.applyTheme] Verified DOM attribute data-theme:', actualTheme);

        // Save to localStorage
        localStorage.setItem('theme', theme);
        console.log('[ThemeHandler.applyTheme] Saved to localStorage:', theme);

        // Dispatch custom event for components that need to react
        window.dispatchEvent(new CustomEvent('themeChanged', {
            detail: { theme: theme }
        }));

        console.log('[ThemeHandler.applyTheme] Complete. Theme applied:', theme);
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
        console.log(`[ThemeHandler.setThemeDelayed] Scheduling theme '${theme}' to be applied in ${delay}ms`);
        setTimeout(function() {
            console.log(`[ThemeHandler.setThemeDelayed] Now applying theme '${theme}'`);
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
            console.log('[ThemeHandler] System theme changed, applying:', e.matches ? 'dark' : 'light');
            ThemeHandler.applyTheme(e.matches ? 'dark' : 'light');
        } else {
            console.log('[ThemeHandler] System theme changed but user has manual preference, ignoring');
        }
    });
}

// Auto-fix: Automatically reapply theme when Blazor removes it
window.expectedTheme = null;
window.isApplying = false;

const observer = new MutationObserver(function(mutations) {
    mutations.forEach(function(mutation) {
        if (mutation.type === 'attributes' && mutation.attributeName === 'data-theme') {
            const currentValue = document.documentElement.getAttribute('data-theme');
            const currentTheme = currentValue || 'light';

            console.log('[ThemeHandler] DOM MUTATION DETECTED: data-theme changed to:', currentValue || '(removed/light)');

            // If Blazor removed our dark theme, reapply it instantly
            if (!window.isApplying && window.expectedTheme === 'dark' && currentValue !== 'dark') {
                console.log('[ThemeHandler] AUTO-FIX: Blazor removed dark theme, reapplying immediately!');
                window.isApplying = true;
                document.documentElement.setAttribute('data-theme', 'dark');
                localStorage.setItem('theme', 'dark');
                window.isApplying = false;
            }
            // If Blazor set dark theme when we want light, fix it
            else if (!window.isApplying && window.expectedTheme === 'light' && currentValue === 'dark') {
                console.log('[ThemeHandler] AUTO-FIX: Blazor set dark theme, removing for light mode!');
                window.isApplying = true;
                document.documentElement.removeAttribute('data-theme');
                localStorage.setItem('theme', 'light');
                window.isApplying = false;
            }
        }
    });
});

observer.observe(document.documentElement, {
    attributes: true,
    attributeFilter: ['data-theme']
});

console.log('[ThemeHandler] Auto-fix monitoring enabled');
