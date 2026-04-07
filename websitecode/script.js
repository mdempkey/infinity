
function showLoginModal() {
    document.getElementById('login-modal').style.display = 'flex';
}

function hideLoginModal() {
    document.getElementById('login-modal').style.display = 'none';
}


window.onclick = function(event) {
    const modal = document.getElementById('login-modal');
    if (event.target === modal) {
        hideLoginModal();
    }
}


function handleLogin() {
    localStorage.setItem('isLoggedIn', 'true');
    hideLoginModal();
    updateUI();
}

function handleLogout() {
    localStorage.setItem('isLoggedIn', 'false');
    updateUI();
}

function updateUI() {
    const isLoggedIn = localStorage.getItem('isLoggedIn') === 'true';

    const loggedOutNav = document.getElementById('logged-out-nav');
    const loggedInNav = document.getElementById('logged-in-nav');
    const loggedOutViews = document.querySelectorAll('.logged-out-view');
    const loggedInViews = document.querySelectorAll('.logged-in-view');

    if (isLoggedIn) {
        if(loggedOutNav) loggedOutNav.style.display = 'none';
        if(loggedInNav) loggedInNav.style.display = 'flex';
        loggedOutViews.forEach(el => el.style.display = 'none');
        loggedInViews.forEach(el => el.style.display = 'block');
    } else {
        if(loggedOutNav) loggedOutNav.style.display = 'block';
        if(loggedInNav) loggedInNav.style.display = 'none';
        loggedOutViews.forEach(el => el.style.display = 'flex');
        loggedInViews.forEach(el => el.style.display = 'none');
    }
}

document.addEventListener("DOMContentLoaded", () => {
    updateUI();
});