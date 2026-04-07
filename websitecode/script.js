function navigateTo(pageId) {
    const pages = document.querySelectorAll('.view-page');

    pages.forEach(page => {
        page.classList.remove('active');
    });

    const targetPage = document.getElementById(pageId);
    if (targetPage) {
        targetPage.classList.add('active');
        window.scrollTo(0, 0);
    }
}

function showLoginModal() {
    const modal = document.getElementById('login-modal');
    modal.style.display = 'flex';
}

function hideLoginModal() {
    const modal = document.getElementById('login-modal');
    modal.style.display = 'none';
}

window.onclick = function(event) {
    const modal = document.getElementById('login-modal');
    if (event.target === modal) {
        hideLoginModal();
    }
}