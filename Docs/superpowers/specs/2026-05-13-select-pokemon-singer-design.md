# Select Pokémon Singer — Design Spec

**Date:** 2026-05-13
**Branch:** feature/select-pokemon-singer

---

## Overview

Wire the Pokémon dropdown in the navbar to the `pitchamon.js` audio engine so that selecting a Pokémon hot-swaps the active cry instrument mid-song — no restart, no pause. The `pitchamon.js` module already exposes `window.Pitchamon.swapCry(url)` for this purpose; the dropdown already fires a `pokemon:selected` custom event on change. This feature connects those two pieces and adds a same-origin cry-proxy endpoint so the browser can fetch cries without hitting the Pokémon API container directly.

---

## Architecture

### 1. Server: Cry Proxy Endpoint

**File:** `src/Infinity.WebApplication/Controllers/PokemonController.cs`

Add a new action:

```
GET /api/pokemon/{name}/cry
```

- Creates a `PokemonApi` HttpClient (already registered in DI with bearer token auth)
- Calls `pokemon/{name}/cry` on the Pokémon API container
- Streams the response body back to the browser with `Content-Type: audio/wav`
- Returns `404` if upstream returns 404
- Returns `502 Bad Gateway` on any other failure

No new models or services needed. Follows the exact pattern of the existing `GetAll` action.

### 2. Client: Event Wiring

**File:** `src/Infinity.WebApplication/Views/Shared/_Header.cshtml`

Inside the existing `<script>` block, after the `selector.addEventListener("change", ...)` handler, add:

```js
window.addEventListener("pokemon:selected", async (event) => {
    const { name } = event.detail;
    setStatus(`Swapping to ${name}…`);
    try {
        await window.Pitchamon?.swapCry(`/api/pokemon/${name}/cry`);
        setStatus(`Now singing as ${name}!`);
    } catch {
        setStatus(`Could not load cry for ${name}.`);
    }
});
```

The `?.` guard makes this a no-op on pages where `pitchamon.js` is not loaded.

### 3. Cleanup: Dead Code Removal

**File:** `src/Infinity.WebApplication/Views/Home/Index.cshtml`

Remove the `@section Scripts` block that references `#playSongBtn`, `#pokemonSelector`, and `#starWarsAudio`. These elements no longer exist in the markup; the block references `data-song` attributes and a static `songDirectory` from a prior design iteration. The block is already inert — removing it keeps the codebase accurate.

---

## Data Flow

```
User selects Pokémon in dropdown
  → _Header.cshtml fires pokemon:selected { id, name, cry }
  → event listener calls Pitchamon.swapCry('/api/pokemon/{name}/cry')
  → pitchamon.js fetches /api/pokemon/{name}/cry (same-origin)
  → PokemonController proxies to Pokémon API container (bearer auth)
  → audio/wav stream returned to browser
  → pitchamon.js decodes, detects base pitch, builds new Sampler
  → activeSampler reference swapped atomically
  → next melody note plays with new cry, no Transport restart
```

---

## File Changes

| File | Change |
|------|--------|
| `src/Infinity.WebApplication/Controllers/PokemonController.cs` | Add `GET /api/pokemon/{name}/cry` action |
| `src/Infinity.WebApplication/Views/Shared/_Header.cshtml` | Add `pokemon:selected` event listener with status feedback |
| `src/Infinity.WebApplication/Views/Home/Index.cshtml` | Remove dead `@section Scripts` block |

---

## Out of Scope

- Volume control
- Visual indicator (sprite/image) for the active Pokémon
- Preloading cries for instant swap
