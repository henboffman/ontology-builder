/**
 * Markdown Toolbar Helper Functions
 * Provides text insertion utilities for markdown formatting toolbar
 */

/**
 * Insert markdown syntax around selected text or at cursor position
 * @param {string} elementId - The textarea element ID
 * @param {string} before - Text to insert before selection
 * @param {string} after - Text to insert after selection
 * @param {string} defaultText - Default text if nothing is selected
 */
window.insertMarkdownSyntax = function (elementId, before, after, defaultText) {
    const textarea = document.getElementById(elementId);
    if (!textarea) {
        console.warn(`Textarea with ID ${elementId} not found`);
        return;
    }

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const selectedText = textarea.value.substring(start, end);
    const textToInsert = selectedText || defaultText;

    const newText = before + textToInsert + after;
    const beforeText = textarea.value.substring(0, start);
    const afterText = textarea.value.substring(end);

    textarea.value = beforeText + newText + afterText;

    // Set cursor position
    const newCursorPos = start + before.length + textToInsert.length;
    textarea.selectionStart = newCursorPos;
    textarea.selectionEnd = newCursorPos;

    // Focus back to textarea
    textarea.focus();

    // Trigger input event to update Blazor binding
    textarea.dispatchEvent(new Event('input', { bubbles: true }));
};

/**
 * Insert markdown syntax at the beginning of the current line
 * @param {string} elementId - The textarea element ID
 * @param {string} prefix - Prefix to insert at line start
 * @param {string} defaultText - Default text if line is empty
 */
window.insertMarkdownLine = function (elementId, prefix, defaultText) {
    const textarea = document.getElementById(elementId);
    if (!textarea) {
        console.warn(`Textarea with ID ${elementId} not found`);
        return;
    }

    const start = textarea.selectionStart;
    const value = textarea.value;

    // Find the start of the current line
    const lineStart = value.lastIndexOf('\n', start - 1) + 1;
    const lineEnd = value.indexOf('\n', start);
    const actualLineEnd = lineEnd === -1 ? value.length : lineEnd;

    // Get current line text
    const currentLine = value.substring(lineStart, actualLineEnd);

    // Check if prefix already exists at line start
    if (currentLine.startsWith(prefix)) {
        // Remove prefix if it exists
        const newLine = currentLine.substring(prefix.length);
        const beforeLine = value.substring(0, lineStart);
        const afterLine = value.substring(actualLineEnd);

        textarea.value = beforeLine + newLine + afterLine;
        textarea.selectionStart = lineStart;
        textarea.selectionEnd = lineStart;
    } else {
        // Add prefix
        const textToAdd = currentLine.trim() || defaultText;
        const newLine = prefix + textToAdd;
        const beforeLine = value.substring(0, lineStart);
        const afterLine = value.substring(actualLineEnd);

        textarea.value = beforeLine + newLine + afterLine;

        // Set cursor after prefix
        const newCursorPos = lineStart + prefix.length + textToAdd.length;
        textarea.selectionStart = newCursorPos;
        textarea.selectionEnd = newCursorPos;
    }

    // Focus back to textarea
    textarea.focus();

    // Trigger input event to update Blazor binding
    textarea.dispatchEvent(new Event('input', { bubbles: true }));
};

/**
 * Insert a code block
 * @param {string} elementId - The textarea element ID
 */
window.insertCodeBlock = function (elementId) {
    const textarea = document.getElementById(elementId);
    if (!textarea) {
        console.warn(`Textarea with ID ${elementId} not found`);
        return;
    }

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const selectedText = textarea.value.substring(start, end);
    const textToInsert = selectedText || 'code';

    const codeBlock = '```\n' + textToInsert + '\n```';
    const beforeText = textarea.value.substring(0, start);
    const afterText = textarea.value.substring(end);

    // Add newlines if not at start/end
    const prefix = start > 0 && beforeText[beforeText.length - 1] !== '\n' ? '\n' : '';
    const suffix = afterText.length > 0 && afterText[0] !== '\n' ? '\n' : '';

    textarea.value = beforeText + prefix + codeBlock + suffix + afterText;

    // Position cursor inside code block
    const newCursorPos = start + prefix.length + 4; // After ```\n
    textarea.selectionStart = newCursorPos;
    textarea.selectionEnd = newCursorPos + textToInsert.length;

    // Focus back to textarea
    textarea.focus();

    // Trigger input event to update Blazor binding
    textarea.dispatchEvent(new Event('input', { bubbles: true }));
};

/**
 * Insert a markdown table
 * @param {string} elementId - The textarea element ID
 */
window.insertTable = function (elementId) {
    const textarea = document.getElementById(elementId);
    if (!textarea) {
        console.warn(`Textarea with ID ${elementId} not found`);
        return;
    }

    const start = textarea.selectionStart;
    const beforeText = textarea.value.substring(0, start);
    const afterText = textarea.value.substring(start);

    const table = '| Column 1 | Column 2 | Column 3 |\n| -------- | -------- | -------- |\n| Cell 1   | Cell 2   | Cell 3   |';

    // Add newlines if not at start/end
    const prefix = start > 0 && beforeText[beforeText.length - 1] !== '\n' ? '\n\n' : '';
    const suffix = afterText.length > 0 && afterText[0] !== '\n' ? '\n\n' : '';

    textarea.value = beforeText + prefix + table + suffix + afterText;

    // Position cursor at first cell
    const newCursorPos = start + prefix.length + 2; // After "| "
    textarea.selectionStart = newCursorPos;
    textarea.selectionEnd = newCursorPos;

    // Focus back to textarea
    textarea.focus();

    // Trigger input event to update Blazor binding
    textarea.dispatchEvent(new Event('input', { bubbles: true }));
};

/**
 * Insert a horizontal rule
 * @param {string} elementId - The textarea element ID
 */
window.insertHorizontalRule = function (elementId) {
    const textarea = document.getElementById(elementId);
    if (!textarea) {
        console.warn(`Textarea with ID ${elementId} not found`);
        return;
    }

    const start = textarea.selectionStart;
    const beforeText = textarea.value.substring(0, start);
    const afterText = textarea.value.substring(start);

    const hr = '---';

    // Add newlines if not at start/end
    const prefix = start > 0 && beforeText[beforeText.length - 1] !== '\n' ? '\n\n' : '';
    const suffix = afterText.length > 0 && afterText[0] !== '\n' ? '\n\n' : '';

    textarea.value = beforeText + prefix + hr + suffix + afterText;

    // Position cursor after HR
    const newCursorPos = start + prefix.length + hr.length + suffix.length;
    textarea.selectionStart = newCursorPos;
    textarea.selectionEnd = newCursorPos;

    // Focus back to textarea
    textarea.focus();

    // Trigger input event to update Blazor binding
    textarea.dispatchEvent(new Event('input', { bubbles: true }));
};
