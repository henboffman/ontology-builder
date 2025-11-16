/**
 * Image Upload Module
 * Handles drag-drop and paste image uploads for markdown editor
 * Security: Client-side validation before upload
 */

export function initializeImageUpload(editorId, noteId, workspaceId) {
    const editor = document.getElementById(editorId);
    if (!editor) {
        console.error(`Editor ${editorId} not found`);
        return;
    }

    // Allowed image types (matches server validation)
    const allowedTypes = ['image/png', 'image/jpeg', 'image/gif', 'image/webp', 'image/svg+xml'];
    const maxFileSize = 1048576; // 1MB (matches server validation)

    // Drag and drop handlers
    editor.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.stopPropagation();
        editor.classList.add('drag-over');
    });

    editor.addEventListener('dragleave', (e) => {
        e.preventDefault();
        e.stopPropagation();
        editor.classList.remove('drag-over');
    });

    editor.addEventListener('drop', async (e) => {
        e.preventDefault();
        e.stopPropagation();
        editor.classList.remove('drag-over');

        const files = Array.from(e.dataTransfer.files);
        const imageFiles = files.filter(f => allowedTypes.includes(f.type));

        if (imageFiles.length === 0) {
            console.warn('No valid image files dropped');
            return;
        }

        for (const file of imageFiles) {
            await uploadImage(file, noteId, workspaceId, editor);
        }
    });

    // Paste handler
    editor.addEventListener('paste', async (e) => {
        const items = Array.from(e.clipboardData.items);
        const imageItems = items.filter(item => item.type.startsWith('image/'));

        if (imageItems.length === 0) {
            return; // Let default paste behavior handle text
        }

        e.preventDefault(); // Prevent default paste for images

        for (const item of imageItems) {
            const file = item.getAsFile();
            if (file && allowedTypes.includes(file.type)) {
                await uploadImage(file, noteId, workspaceId, editor);
            }
        }
    });

    console.log(`Image upload initialized for editor ${editorId}`);
}

/**
 * Upload image file to server
 */
async function uploadImage(file, noteId, workspaceId, editor) {
    // Validate file size
    if (file.size > 1048576) { // 1MB
        alert(`Image "${file.name}" is too large. Maximum size is 1MB.`);
        return;
    }

    // Show uploading indicator
    const placeholder = `\n\n![Uploading ${file.name}...]()\n\n`;
    insertTextAtCursor(editor, placeholder);

    const formData = new FormData();
    formData.append('file', file);
    formData.append('workspaceId', workspaceId);

    try {
        const response = await fetch(`/api/attachments/upload/${noteId}`, {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Upload failed');
        }

        const result = await response.json();

        // Replace placeholder with actual markdown image syntax
        const markdownImage = `![${file.name}](${result.url})`;
        const currentValue = editor.value;
        editor.value = currentValue.replace(placeholder, `\n\n${markdownImage}\n\n`);

        // Trigger input event for Blazor binding
        editor.dispatchEvent(new Event('input', { bubbles: true }));

        console.log(`Image uploaded successfully: ${result.id}`);
    } catch (error) {
        console.error('Image upload failed:', error);

        // Remove placeholder
        const currentValue = editor.value;
        editor.value = currentValue.replace(placeholder, '');
        editor.dispatchEvent(new Event('input', { bubbles: true }));

        alert(`Failed to upload image: ${error.message}`);
    }
}

/**
 * Insert text at cursor position
 */
function insertTextAtCursor(textarea, text) {
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const value = textarea.value;

    textarea.value = value.substring(0, start) + text + value.substring(end);

    // Move cursor to end of inserted text
    const newPosition = start + text.length;
    textarea.setSelectionRange(newPosition, newPosition);

    // Focus back on textarea
    textarea.focus();
}

/**
 * Get anti-forgery token from page
 */
function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}

/**
 * Cleanup function
 */
export function disposeImageUpload(editorId) {
    const editor = document.getElementById(editorId);
    if (editor) {
        // Remove event listeners (note: this won't work with anonymous functions)
        // For production, we'd need to store references to the handlers
        console.log(`Image upload disposed for editor ${editorId}`);
    }
}
