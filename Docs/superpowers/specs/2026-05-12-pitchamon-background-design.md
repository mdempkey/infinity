# Pitchamon Background Audio — Design Spec

**Date:** 2026-05-12
**Branch:** feature/pitchamon-background

---

## Overview

Play the Star Wars theme on page load using Pokémon cries as the instrument. Notes from a MIDI file are pitch-shifted at runtime so the active cry plays every melody note at the correct pitch. The song plays automatically on first user interaction, loops from measure 4 (B-flat triplet) after the intro, and is architected to support future hot-swapping of the active Pokémon cry without restarting or pausing the song.

---

## Architecture

### Data Layer — Server-side MIDI Parsing

**Package:** Add `Melanchall.DryWetMidi` to `Infinity.WebApplication.csproj`.

**MIDI file location:** `Audio/starwars.mid` inside the `Infinity.WebApplication` project (not in `wwwroot` — the browser never fetches the raw file).

**New controller:** `AudioController` in `Infinity.WebApplication/Controllers/AudioController.cs`

**Endpoint:** `GET /audio/notes`

Reads `starwars.mid`, converts MIDI ticks to real seconds using DryWetMidi's tempo map, finds the timestamp of the first note in measure 4 (the B-flat triplet) for `loopStart`, and returns:

```json
{
  "notes": [
    { "midi": 62, "time": 0.0, "duration": 0.4 },
    { "midi": 67, "time": 0.45, "duration": 0.4 }
  ],
  "loopStart": 3.85,
  "totalDuration": 92.0
}
```

- `midi` — MIDI note number (0–127)
- `time` — note start time in seconds from track beginning
- `duration` — note duration in seconds
- `loopStart` — timestamp in seconds of the first note at measure 4
- `totalDuration` — total track length in seconds

The browser receives a plain note array — no MIDI parsing needed client-side.

### Audio Engine — Client-side (`pitchamon.js`)

**Tone.js:** Tone.js 15.1.22 UMD build placed at `wwwroot/lib/tone/tone.min.js`, loaded via `<script>` tag. No npm or build pipeline required.

**Pokémon cry file:** `wwwroot/audio/cry-default.wav`. The browser fetches, decodes, and analyzes this file at startup.

**Module:** `wwwroot/js/pitchamon.js` — self-contained, self-initializing.

#### Module-level State

```js
let activeSampler = null;   // Tone.Sampler instance for the active cry
```

#### Initialization Sequence

Steps 1 and 2 run in parallel via `Promise.all`:

1. `fetch('/audio/notes')` → note array + `loopStart` + `totalDuration`
2. Fetch and decode `cry-default.wav` → run autocorrelation pitch detection → get `baseNote`

Then:

3. Create `Tone.Sampler` registered at `baseNote`, pointing to the cry URL
4. Wait for Sampler to finish loading (`Tone.loaded()`)
5. Build `Tone.Part` from the note array
6. Configure Transport loop
7. Attach one-time user-gesture listener → start playback

### Pitch Detection (Autocorrelation)

Runs once per cry load. Detects the cry's fundamental frequency so Tone.Sampler knows what pitch the sample is "at."

1. Decode the `.wav` into an `AudioBuffer` via `Tone.context.decodeAudioData`
2. Extract the first ~0.2 seconds of the mono channel (the attack, where pitch is clearest)
3. Slide a lag value across a range covering ~80 Hz to ~2000 Hz and compute autocorrelation at each lag
4. The lag with highest correlation is the fundamental period in samples
5. Convert: `frequency = sampleRate / lag`
6. Convert to nearest MIDI note: `note = Math.round(12 * Math.log2(frequency / 440) + 69)`

Result is the `baseNote` used to register the sample in Tone.Sampler.

### Pitch Shifting

`Tone.Sampler` handles all pitch shifting internally. When initialized with:

```js
new Tone.Sampler({ [baseNoteString]: cryUrl }).toDestination()
```

Calling `sampler.triggerAttackRelease(targetNote, duration, time)` automatically shifts the cry from `baseNote` to `targetNote`. No manual `playbackRate` math required.

### Playback & Loop

```js
Tone.Transport.loop = true;
Tone.Transport.loopStart = loopStart;   // seconds — first note of measure 4
Tone.Transport.loopEnd = totalDuration; // seconds — end of track
```

A `Tone.Part` is built from the note array. Each event callback calls:

```js
activeSampler.triggerAttackRelease(
  Tone.Frequency(midiNote, "midi").toNote(),
  duration,
  time
);
```

On the first pass, Transport plays from 0 through `totalDuration` — the intro (measures 1–3) plays once. On every subsequent loop, Transport jumps to `loopStart` and replays from measure 4 onward. Part events before `loopStart` are not re-triggered on loops.

### Autoplay

Browsers block audio without a user gesture. The module attaches a one-time `click`/`keydown` listener to `document`. On first interaction:

```js
await Tone.start();
Tone.Transport.start();
```

No visible UI element or button is required — users will naturally interact with the globe or a park card within seconds of opening the page.

### Hot-swap (future feature)

The `activeSampler` module-level reference enables seamless instrument swapping without restarting the song:

1. Fetch new cry `.wav`
2. Decode and run autocorrelation → detect new `baseNote`
3. Create new `Tone.Sampler` registered at new `baseNote`
4. Await `Tone.loaded()`
5. Atomically replace `activeSampler` reference
6. Dispose old Sampler after a short delay (~2 seconds) to let any in-progress notes finish

The `Tone.Part` callback always reads `activeSampler` at call time, so the very next note after the swap uses the new cry. No Transport restart, no gap.

---

## File Layout

```
Infinity.WebApplication/
  Audio/
    starwars.mid                  ← server reads this, browser never fetches it
  Controllers/
    AudioController.cs            ← new: GET /audio/notes
  wwwroot/
    lib/tone/
      tone.min.js                 ← Tone.js UMD build
    audio/
      cry-default.wav             ← default Pokémon cry
    js/
      pitchamon.js                ← audio engine module
```

---

## Integration

Two script tags added to `Views/Shared/_Layout.cshtml` before `</body>`:

```html
<script src="~/lib/tone/tone.min.js"></script>
<script src="~/js/pitchamon.js"></script>
```

No other views require changes. `pitchamon.js` self-initializes on load.

---

## Out of Scope (this feature)

- Dropdown UI for selecting a Pokémon cry (hot-swap trigger)
- Volume control
- Mobile-specific audio behavior
