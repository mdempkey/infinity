# Sign-Up & Login UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire up the sign-up and login modals to the existing `AuthController` API endpoints, with client-side JWT storage and a header that reflects the user's auth state.

**Architecture:** A new `auth.js` module manages JWT storage in `localStorage` and controls the header's logged-in/logged-out state. Two separate modals (`_LoginModal`, `_SignUpModal`) each `fetch` their respective API endpoint and call into `auth.js` on success. The header renders both nav states in HTML at all times and toggles visibility via Bootstrap's `d-none` class.

**Tech Stack:** ASP.NET Core 10 MVC (Razor views), Bootstrap 5, vanilla JS (`fetch`, `localStorage`)

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `src/Infinity.WebApplication/wwwroot/js/auth.js` | Create | Token storage, header state, page-load init |
| `src/Infinity.WebApplication/Views/Shared/_Layout.cshtml` | Modify | Add `auth.js` script tag |
| `src/Infinity.WebApplication/Views/Shared/_Header.cshtml` | Modify | Both nav states + include `_SignUpModal` partial |
| `src/Infinity.WebApplication/Views/Shared/_SignUpModal.cshtml` | Create | Sign-up form wired to `POST /api/auth/register` |
| `src/Infinity.WebApplication/Views/Shared/_LoginModal.cshtml` | Modify | Fix email→username; wire to `POST /api/auth/login` |

---

### Task 1: Create `auth.js`

**Files:**
- Create: `src/Infinity.WebApplication/wwwroot/js/auth.js`

- [ ] **Step 1: Create the `wwwroot/js/` directory and `auth.js`**

```javascript
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

function getUsername() {
    return localStorage.getItem(AUTH_USERNAME_KEY);
}

function updateHeader() {
    const loggedOut = document.getElementById('nav-logged-out');
    const loggedIn = document.getElementById('nav-logged-in');
    const usernameDisplay = document.getElementById('nav-username');

    if (getToken()) {
        loggedOut.classList.add('d-none');
        loggedIn.classList.remove('d-none');
        if (usernameDisplay) usernameDisplay.textContent = getUsername();
    } else {
        loggedOut.classList.remove('d-none');
        loggedIn.classList.add('d-none');
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
```

- [ ] **Step 2: Verify the file exists**

```bash
ls src/Infinity.WebApplication/wwwroot/js/auth.js
```

Expected: file listed with no error.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/wwwroot/js/auth.js
git commit -m "feat: add auth.js for JWT storage and header state"
```

---

### Task 2: Add `auth.js` to layout

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_Layout.cshtml`

The script must load after Bootstrap (which is already at the bottom of `<body>`) so Bootstrap's modal API is available, and before page-specific scripts so `saveAuth`/`updateHeader` are defined when modal scripts register their event listeners.

- [ ] **Step 1: Add `auth.js` script tag after the Bootstrap bundle line**

Find this line in `_Layout.cshtml`:
```html
<script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
```

Add the new line immediately after it so the block reads:
```html
<script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
<script src="~/js/auth.js"></script>
```

- [ ] **Step 2: Build and verify no compile errors**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_Layout.cshtml
git commit -m "feat: load auth.js in layout"
```

---

### Task 3: Update `_Header.cshtml`

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_Header.cshtml`

Replace the entire file with the two-state nav and both modal partials.

- [ ] **Step 1: Replace `_Header.cshtml` with the following**

```html
<header class="site-navbar navbar navbar-expand sticky-top px-4 px-md-5 py-3 border-bottom border-secondary-subtle">
    <a class="site-title navbar-brand p-0 m-0" asp-controller="Home" asp-action="Index" aria-label="Infinity Site home">
        <span class="site-title__name">STAR WARS</span>
        <span class="site-title__tag">Themed Attractions</span>
    </a>
    <nav class="ms-auto d-flex gap-3 align-items-center justify-content-end">
        <div id="nav-logged-out" class="d-flex gap-3 align-items-center">
            <button type="button" class="btn btn-link btn-sm p-0" onclick="showLoginModal()">Log In</button>
            <button type="button" class="btn btn-link btn-sm p-0" onclick="showSignUpModal()">Sign Up</button>
        </div>
        <div id="nav-logged-in" class="d-none d-flex gap-3 align-items-center">
            <span id="nav-username" class="text-secondary btn-sm"></span>
            <button type="button" class="btn btn-link btn-sm p-0" onclick="logout()">Log Out</button>
        </div>
    </nav>
</header>

<partial name="_LoginModal" />
<partial name="_SignUpModal" />
```

- [ ] **Step 2: Build and verify no compile errors**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded` with 0 errors. (Note: this will fail at runtime until `_SignUpModal.cshtml` is created in Task 4 — that is expected.)

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_Header.cshtml
git commit -m "feat: add logged-in/logged-out header states"
```

---

### Task 4: Create `_SignUpModal.cshtml`

**Files:**
- Create: `src/Infinity.WebApplication/Views/Shared/_SignUpModal.cshtml`

- [ ] **Step 1: Create `_SignUpModal.cshtml`**

```html
<div class="modal fade" id="signup-modal" tabindex="-1" aria-labelledby="signup-modal-title" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <button type="button" class="modal-close btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            <div class="modal-header">
                <h5 class="modal-title text-primary" id="signup-modal-title">Create an Account</h5>
            </div>
            <div class="modal-body">
                <p class="text-secondary mb-3">Join to rate attractions and write reviews.</p>
                <form id="signup-form" class="d-grid gap-2">
                    <input type="text" id="signup-username" placeholder="Username" class="form-control" required />
                    <input type="email" id="signup-email" placeholder="Email" class="form-control" required />
                    <input type="password" id="signup-password" placeholder="Password" class="form-control" required />
                    <p id="signup-error" class="text-danger small mb-0 d-none"></p>
                    <button type="submit" class="btn btn-primary w-100 mt-2">Sign Up</button>
                </form>
            </div>
        </div>
    </div>
</div>

<script>
    let signupModalInstance = null;

    function showSignUpModal() {
        const modalElement = document.getElementById("signup-modal");
        if (!modalElement || !window.bootstrap) return;
        signupModalInstance ??= window.bootstrap.Modal.getOrCreateInstance(modalElement);
        signupModalInstance.show();
    }

    function hideSignUpModal() {
        signupModalInstance?.hide();
    }

    document.getElementById("signup-form").addEventListener("submit", async (e) => {
        e.preventDefault();
        const username = document.getElementById("signup-username").value;
        const email = document.getElementById("signup-email").value;
        const password = document.getElementById("signup-password").value;
        const errorEl = document.getElementById("signup-error");

        errorEl.classList.add("d-none");

        const res = await fetch("/api/auth/register", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, email, password })
        });

        if (res.ok) {
            const data = await res.json();
            saveAuth(data.token, data.username);
            updateHeader();
            hideSignUpModal();
        } else {
            const text = await res.text();
            errorEl.textContent = text || "Registration failed. Please try again.";
            errorEl.classList.remove("d-none");
        }
    });
</script>
```

- [ ] **Step 2: Build and verify no compile errors**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_SignUpModal.cshtml
git commit -m "feat: add sign-up modal wired to /api/auth/register"
```

---

### Task 5: Fix `_LoginModal.cshtml`

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_LoginModal.cshtml`

Two changes: swap the email field for a username field, and replace the no-op `onsubmit` with a `fetch` to `/api/auth/login`.

- [ ] **Step 1: Replace `_LoginModal.cshtml` with the following**

```html
<div class="modal fade" id="login-modal" tabindex="-1" aria-labelledby="login-modal-title" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <button type="button" class="modal-close btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            <div class="modal-header">
                <h5 class="modal-title text-primary" id="login-modal-title">Welcome to Infinity Site</h5>
            </div>
            <div class="modal-body">
                <p class="text-secondary mb-3">Log in to rate attractions and write reviews.</p>
                <form id="login-form" class="d-grid gap-2">
                    <input type="text" id="login-username" placeholder="Username" class="form-control" required />
                    <input type="password" id="login-password" placeholder="Password" class="form-control" required />
                    <p id="login-error" class="text-danger small mb-0 d-none"></p>
                    <button type="submit" class="btn btn-primary w-100 mt-2">Log In</button>
                </form>
            </div>
        </div>
    </div>
</div>

<script>
    let loginModalInstance = null;

    function showLoginModal() {
        const modalElement = document.getElementById("login-modal");
        if (!modalElement || !window.bootstrap) return;
        loginModalInstance ??= window.bootstrap.Modal.getOrCreateInstance(modalElement);
        loginModalInstance.show();
    }

    function hideLoginModal() {
        loginModalInstance?.hide();
    }

    document.getElementById("login-form").addEventListener("submit", async (e) => {
        e.preventDefault();
        const username = document.getElementById("login-username").value;
        const password = document.getElementById("login-password").value;
        const errorEl = document.getElementById("login-error");

        errorEl.classList.add("d-none");

        const res = await fetch("/api/auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password })
        });

        if (res.ok) {
            const data = await res.json();
            saveAuth(data.token, data.username);
            updateHeader();
            hideLoginModal();
        } else {
            errorEl.textContent = "Invalid username or password.";
            errorEl.classList.remove("d-none");
        }
    });
</script>
```

- [ ] **Step 2: Build and verify no compile errors**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Manual smoke test**

Start the app:
```bash
dotnet run --project src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Verify each of the following in the browser:

| Scenario | Expected |
|---|---|
| Page load (no token in `localStorage`) | Header shows "Log In" and "Sign Up" buttons |
| Click "Sign Up", fill in username/email/password, submit | Modal closes; header shows username + "Log Out" |
| Refresh the page | Header still shows username + "Log Out" (token persisted) |
| Click "Log Out" | Header returns to "Log In" + "Sign Up" |
| Click "Log In", submit valid credentials | Modal closes; header shows username + "Log Out" |
| Click "Log In", submit wrong credentials | Modal stays open; red error "Invalid username or password." appears |
| Click "Sign Up", submit a username/email already taken | Modal stays open; red error "Username or email already in use." appears |

- [ ] **Step 4: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_LoginModal.cshtml
git commit -m "fix: wire login modal to /api/auth/login, use username field"
```
