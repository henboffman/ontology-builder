// Mobile menu toggle functionality
function toggleMobileMenu() {
    const sidebar = document.querySelector('.sidebar');
    const overlay = document.querySelector('.mobile-overlay');
    const toggleBtn = document.querySelector('.mobile-menu-toggle');
    const icon = toggleBtn?.querySelector('i');

    if (sidebar && overlay) {
        const isOpen = sidebar.classList.contains('mobile-open');

        if (isOpen) {
            // Close menu
            sidebar.classList.remove('mobile-open');
            overlay.classList.remove('mobile-open');
            if (icon) {
                icon.className = 'bi bi-list';
            }
        } else {
            // Open menu
            sidebar.classList.add('mobile-open');
            overlay.classList.add('mobile-open');
            if (icon) {
                icon.className = 'bi bi-x-lg';
            }
        }
    }
}

// Close menu when clicking a nav link (better UX on mobile)
document.addEventListener('DOMContentLoaded', function() {
    const navLinks = document.querySelectorAll('.sidebar a');
    navLinks.forEach(link => {
        link.addEventListener('click', function() {
            if (window.innerWidth <= 640) {
                const sidebar = document.querySelector('.sidebar');
                if (sidebar?.classList.contains('mobile-open')) {
                    toggleMobileMenu();
                }
            }
        });
    });
});

// Close menu on escape key
document.addEventListener('keydown', function(event) {
    if (event.key === 'Escape') {
        const sidebar = document.querySelector('.sidebar');
        if (sidebar?.classList.contains('mobile-open')) {
            toggleMobileMenu();
        }
    }
});
