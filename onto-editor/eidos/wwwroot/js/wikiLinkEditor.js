// WikiLink Editor - Interactive [[wiki-link]] support for note editor
// Provides syntax highlighting and click-to-navigate functionality

window.WikiLinkEditor = {
    // Initialize the wiki-link editor on a textarea with retry logic
    initialize: function (elementId, dotNetHelper, retryCount = 0) {
        const textareaElement = document.getElementById(elementId);

        if (!textareaElement) {
            // Retry up to 3 times with increasing delays (50ms, 100ms, 150ms)
            if (retryCount < 3) {
                const delay = (retryCount + 1) * 50;
                setTimeout(() => {
                    this.initialize(elementId, dotNetHelper, retryCount + 1);
                }, delay);
                return;
            }

            // Element not found after retries - this is normal if in preview mode
            // No logging needed as this is expected behavior
            return;
        }

        // Store reference to .NET helper for callbacks
        textareaElement._dotNetHelper = dotNetHelper;

        // Add event listeners
        this.addEventListeners(textareaElement);

        // Initial highlight
        this.highlightWikiLinks(textareaElement);

    },

    // Add event listeners to textarea
    addEventListeners: function (textarea) {
        // Highlight on input
        textarea.addEventListener('input', () => {
            this.highlightWikiLinks(textarea);
        });

        // Handle Ctrl+Click or Cmd+Click on wiki-links
        textarea.addEventListener('click', (e) => {
            if (e.ctrlKey || e.metaKey) {
                this.handleWikiLinkClick(textarea, e);
            }
        });

        // Show tooltip on hover over wiki-links
        textarea.addEventListener('mousemove', (e) => {
            this.handleHover(textarea, e);
        });
    },

    // Highlight [[wiki-links]] in the textarea using a backdrop/overlay technique
    highlightWikiLinks: function (textarea) {
        // For now, we'll add a CSS class to style the textarea
        // Full syntax highlighting would require a more complex approach with CodeMirror or Monaco

        // Add a data attribute with the wiki-link count for CSS styling
        const content = textarea.value;
        const wikiLinkRegex = /\[\[([^\]]+)\]\]/g;
        const matches = content.match(wikiLinkRegex);
        const count = matches ? matches.length : 0;

        textarea.setAttribute('data-wikilink-count', count);
    },

    // Handle click on wiki-link (Ctrl+Click or Cmd+Click)
    handleWikiLinkClick: function (textarea, event) {
        const cursorPos = textarea.selectionStart;
        const content = textarea.value;

        // Find if cursor is inside a [[wiki-link]]
        const wikiLink = this.getWikiLinkAtPosition(content, cursorPos);

        if (wikiLink) {
            event.preventDefault();

            // Call back to .NET to navigate to the concept
            if (textarea._dotNetHelper) {
                textarea._dotNetHelper.invokeMethodAsync('NavigateToConcept', wikiLink.conceptName);
            }
        }
    },

    // Handle hover to show tooltip
    handleHover: function (textarea, event) {
        const cursorPos = this.getCursorPositionFromMouse(textarea, event);
        const content = textarea.value;

        const wikiLink = this.getWikiLinkAtPosition(content, cursorPos);

        if (wikiLink) {
            textarea.style.cursor = 'pointer';
            textarea.title = `Ctrl+Click to open "${wikiLink.conceptName}"`;
        } else {
            textarea.style.cursor = 'text';
            textarea.title = '';
        }
    },

    // Get cursor position from mouse event
    getCursorPositionFromMouse: function (textarea, event) {
        // This is a simplified approach - for production, use a library
        const rect = textarea.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;

        // Approximate position based on line height and character width
        const lineHeight = parseInt(window.getComputedStyle(textarea).lineHeight);
        const fontSize = parseInt(window.getComputedStyle(textarea).fontSize);
        const charWidth = fontSize * 0.6; // Rough estimate for monospace

        const lines = textarea.value.split('\n');
        const lineIndex = Math.floor((y + textarea.scrollTop) / lineHeight);
        const charIndex = Math.floor((x + textarea.scrollLeft) / charWidth);

        let position = 0;
        for (let i = 0; i < lineIndex && i < lines.length; i++) {
            position += lines[i].length + 1; // +1 for newline
        }
        position += Math.min(charIndex, lines[lineIndex]?.length || 0);

        return position;
    },

    // Get wiki-link at a specific position in the text
    getWikiLinkAtPosition: function (content, position) {
        const wikiLinkRegex = /\[\[([^\]]+)\]\]/g;
        let match;

        while ((match = wikiLinkRegex.exec(content)) !== null) {
            const startPos = match.index;
            const endPos = match.index + match[0].length;

            if (position >= startPos && position <= endPos) {
                return {
                    conceptName: match[1],
                    startPos: startPos,
                    endPos: endPos
                };
            }
        }

        return null;
    },

    // Clean up when component is disposed
    dispose: function (elementId) {
        const textareaElement = document.getElementById(elementId);
        if (textareaElement && textareaElement._dotNetHelper) {
            textareaElement._dotNetHelper = null;
        }
    }
};
