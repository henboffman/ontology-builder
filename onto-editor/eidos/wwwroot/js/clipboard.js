// Clipboard utility function with fallback support
window.copyToClipboardFallback = function(text) {
    // Create a temporary textarea element
    const textarea = document.createElement('textarea');
    textarea.value = text;
    textarea.style.position = 'fixed';  // Avoid scrolling to bottom
    textarea.style.opacity = '0';
    document.body.appendChild(textarea);
    textarea.focus();
    textarea.select();

    try {
        // Try the old execCommand method
        const successful = document.execCommand('copy');
        if (!successful) {
            throw new Error('execCommand failed');
        }
    } finally {
        document.body.removeChild(textarea);
    }
};

// Copy from an existing input field (more reliable in Safari)
// Returns true if copy was successful, false if user needs to manually copy
window.copyFromInputField = function(inputId) {
    const input = document.getElementById(inputId);
    if (!input) {
        throw new Error('Input field not found: ' + inputId);
    }

    // Focus and select the text in the input
    input.focus();
    input.select();
    input.setSelectionRange(0, input.value.length);

    // Try to copy
    try {
        const successful = document.execCommand('copy');
        if (successful) {
            // Deselect after successful copy
            if (window.getSelection) {
                window.getSelection().removeAllRanges();
            }
            return true;
        }
    } catch (err) {
        console.error('execCommand failed:', err);
    }

    // If we get here, keep the text selected so user can manually copy
    return false;
};
