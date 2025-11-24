/**
 * Image Lightbox Module
 * Provides full-screen image viewing with click/double-click to expand
 */

let lightboxContainer = null;

/**
 * Initialize lightbox for markdown preview images
 */
export function initializeImageLightbox(previewElementId) {
    const previewElement = document.getElementById(previewElementId);
    if (!previewElement) {
        // Silently return - element may not exist for grid notes
        return;
    }

    attachLightboxHandlers(previewElement, previewElementId);
}

/**
 * Attach lightbox click handlers to preview element
 */
function attachLightboxHandlers(previewElement, previewElementId) {
    // Create lightbox container if it doesn't exist
    if (!lightboxContainer) {
        createLightboxContainer();
    }

    // Mark all images in the preview as lightbox-enabled
    const images = previewElement.querySelectorAll('img');
    images.forEach(img => {
        img.classList.add('lightbox-enabled');
        img.style.cursor = 'pointer';
    });

}

/**
 * Create the lightbox overlay container
 */
function createLightboxContainer() {
    lightboxContainer = document.createElement('div');
    lightboxContainer.id = 'image-lightbox';
    lightboxContainer.className = 'image-lightbox';
    lightboxContainer.innerHTML = `
        <div class="lightbox-backdrop"></div>
        <div class="lightbox-content">
            <button class="lightbox-close" aria-label="Close" title="Close (Esc)">
                <svg width="24" height="24" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M2.146 2.854a.5.5 0 1 1 .708-.708L8 7.293l5.146-5.147a.5.5 0 0 1 .708.708L8.707 8l5.147 5.146a.5.5 0 0 1-.708.708L8 8.707l-5.146 5.147a.5.5 0 0 1-.708-.708L7.293 8 2.146 2.854Z"/>
                </svg>
            </button>
            <div class="lightbox-image-container">
                <img class="lightbox-image" src="" alt="">
            </div>
            <div class="lightbox-caption"></div>
        </div>
    `;
    document.body.appendChild(lightboxContainer);

    // Event listeners
    const closeBtn = lightboxContainer.querySelector('.lightbox-close');
    const backdrop = lightboxContainer.querySelector('.lightbox-backdrop');

    closeBtn.addEventListener('click', closeLightbox);
    backdrop.addEventListener('click', closeLightbox);

    // Global click handler for lightbox-enabled images
    document.addEventListener('click', (e) => {
        if (e.target.tagName === 'IMG' && e.target.classList.contains('lightbox-enabled')) {
            e.preventDefault();
            openLightbox(e.target.src, e.target.alt);
        }
    });

    // Keyboard navigation
    document.addEventListener('keydown', handleKeyPress);
}

/**
 * Open lightbox with image
 */
function openLightbox(imageSrc, imageAlt) {
    if (!lightboxContainer) {
        createLightboxContainer();
    }

    const img = lightboxContainer.querySelector('.lightbox-image');
    const caption = lightboxContainer.querySelector('.lightbox-caption');

    img.src = imageSrc;
    img.alt = imageAlt || '';
    caption.textContent = imageAlt || '';

    lightboxContainer.classList.add('active');
    document.body.style.overflow = 'hidden'; // Prevent background scrolling
}

/**
 * Close lightbox
 */
function closeLightbox() {
    if (lightboxContainer) {
        lightboxContainer.classList.remove('active');
        document.body.style.overflow = ''; // Restore scrolling
    }
}

/**
 * Handle keyboard navigation
 */
function handleKeyPress(e) {
    if (!lightboxContainer || !lightboxContainer.classList.contains('active')) {
        return;
    }

    if (e.key === 'Escape') {
        closeLightbox();
    }
}

/**
 * Cleanup function
 */
export function disposeImageLightbox() {
    if (lightboxContainer) {
        lightboxContainer.remove();
        lightboxContainer = null;
    }
    document.removeEventListener('keydown', handleKeyPress);
}
