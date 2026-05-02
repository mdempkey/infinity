# Sign-Up & Login UI Design

**Date:** 2026-04-30  
**Branch:** `userdb/login`  
**Scope:** Frontend auth modals, header auth state, client-side JWT storage

---

## Overview

Wire up the existing sign-up and login UI stubs to the `AuthController` endpoints on `Infinity.WebApplication`. The register endpoint (`POST /api/auth/register`) and login endpoint (`POST /api/auth/login`) are already implemented and return a JWT token + username on success. This spec covers the frontend only — no backend changes required.

---

## Files Changed

| File | Change |
|---|---|
| `Views/Shared/_SignUpModal.cshtml` | New — sign-up form with username, email, password |
| `Views/Shared/_LoginModal.cshtml` | Fix email → username field; wire `fetch` to API |
| `Views/Shared/_Header.cshtml` | Add logged-in state (username + Log Out); include sign-up modal partial; wire Log Out |
| `Views/Shared/_Layout.cshtml` | Add `<script src="/js/auth.js">` |
| `wwwroot/js/auth.js` | New — token storage, header state management, init on page load |

---

## `auth.js` Module

Single JS file at `wwwroot/js/auth.js`. Three responsibilities:

### Token management
- `saveAuth(token, username)` — writes token and username to `localStorage`
- `clearAuth()` — removes token and username from `localStorage`
- `getToken()` — returns stored token or `null`
- `getUsername()` — returns stored username or `null`

### Header state
- `updateHeader()` — reads `localStorage` and toggles header between two states:
  - **Logged-out:** shows "Log In" and "Sign Up" buttons, hides user info
  - **Logged-in:** shows username `<span>` and "Log Out" button, hides auth buttons
  - Toggled via Bootstrap's `d-none` class; both states present in the HTML at all times

### Page init
- `initAuth()` — called on `DOMContentLoaded`; calls `updateHeader()` to restore header state from any token already in `localStorage` (i.e. returning users see their logged-in state immediately on page load)

---

## Sign-Up Modal (`_SignUpModal.cshtml`)

Separate modal from the login modal (`id="signup-modal"`).

**Fields:** username, email, password

**On submit:**
1. POST to `/api/auth/register` with `{ username, email, password }` as JSON
2. Password is sent plain-text over HTTPS — BCrypt hashing is handled server-side in `UserService.RegisterAsync()` before DB write
3. On success (200): call `saveAuth(token, username)`, call `updateHeader()`, close modal
4. On error (400): display the API error message inline below the form (e.g. "Username or email already in use.")

The register endpoint returns an `AuthResponse` containing the token and username directly, so no second login request is needed after registration.

---

## Login Modal (`_LoginModal.cshtml`)

**Fix:** replace the existing email field with a username field — `LoginRequest` takes `{ username, password }`, not email.

**On submit:**
1. POST to `/api/auth/login` with `{ username, password }` as JSON
2. On success (200): call `saveAuth(token, username)`, call `updateHeader()`, close modal
3. On error (401): display "Invalid username or password." inline below the form

---

## Header (`_Header.cshtml`)

Two nav states, both rendered in HTML, toggled by `updateHeader()`:

**Logged-out state (default):**
```
[Log In]  [Sign Up]
```
- "Log In" opens `#login-modal`
- "Sign Up" opens `#signup-modal`

**Logged-in state:**
```
<username>  [Log Out]
```
- Username displayed as a static `<span>`
- "Log Out" calls `clearAuth()` then `updateHeader()`; no server-side request needed — JWT is stateless and logout is fully client-side

Both `_LoginModal` and `_SignUpModal` partials are included at the bottom of `_Header.cshtml`.

---

## Auth Flow Summary

```
Register:  form submit → POST /api/auth/register → saveAuth(token, username) → updateHeader() → close modal
Login:     form submit → POST /api/auth/login    → saveAuth(token, username) → updateHeader() → close modal
Logout:    button click → clearAuth() → updateHeader()
Page load: DOMContentLoaded → initAuth() → updateHeader()
```

---

## Out of Scope

- Token expiry / refresh — JWT expiry is set server-side (`Jwt:ExpiryHours`); expired tokens are not handled client-side in this phase
- Protected routes / redirect on logout
- "Remember me" or persistent session options
- Any backend changes
