const AUTH_TOKEN_KEY = 'auth_token';
const AUTH_USERNAME_KEY = 'auth_username';

function saveAuth(token, username) {
    localStorage.setItem(AUTH_TOKEN_KEY, token);
    localStorage.setItem(AUTH_USERNAME_KEY, username);
}

function clearAuth() {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(AUTH_USERNAME_KEY);
}

function getToken() {
    return localStorage.getItem(AUTH_TOKEN_KEY);
}

function isLoggedIn() {
    return localStorage.getItem(AUTH_TOKEN_KEY) !== null;
}

function getUsername() {
    return localStorage.getItem(AUTH_USERNAME_KEY);
}

function updateHeader() {
    const loggedOut = document.getElementById('nav-logged-out');
    const loggedIn = document.getElementById('nav-logged-in');
    const usernameDisplay = document.getElementById('nav-username');

    if (!loggedOut || !loggedIn) return;

    if (isLoggedIn()) {
        loggedOut.classList.add('d-none');
        loggedOut.classList.remove('d-flex');
        loggedIn.classList.remove('d-none');
        loggedIn.classList.add('d-flex');
        if (usernameDisplay) usernameDisplay.textContent = "👋 " + getUsername();
    } else {
        loggedOut.classList.remove('d-none');
        loggedOut.classList.add('d-flex');
        loggedIn.classList.add('d-none');
        loggedIn.classList.remove('d-flex');
    }
}

function logout() {
    clearAuth();
    updateHeader();
}

function initAuth() {
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', updateHeader);
    } else {
        updateHeader();
    }
}

initAuth();
