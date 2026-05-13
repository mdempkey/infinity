# Pitchamon Background Audio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Play the Star Wars theme on page load using a Pokémon cry as the instrument, with pitch-detection-driven Tone.js Sampler playback and an intro-once loop.

**Architecture:** A C# `AudioController` parses `Audio/starwars.mid` via DryWetMidi at startup, caches the note array + loop timestamps, and serves them as JSON from `GET /audio/notes`. A self-contained `pitchamon.js` module fetches that data, decodes `cry-default.wav`, runs autocorrelation to detect the cry's base pitch, creates a `Tone.Sampler`, and uses `Tone.Transport` + `Tone.Part` to schedule playback — starting on the first user gesture.

**Tech Stack:** C# / ASP.NET Core MVC, Melanchall.DryWetMidi, Tone.js 15.1.22 (UMD), Web Audio API, NUnit

---

## File Structure

| File | Status | Responsibility |
|------|--------|----------------|
| `src/Infinity.WebApplication/Infinity.WebApplication.csproj` | Modify | Add DryWetMidi package + MIDI content item |
| `src/Infinity.WebApplication/Models/AudioDtos.cs` | Create | `NoteDto` and `AudioNotesResponse` records |
| `src/Infinity.WebApplication/Services/AudioService/IAudioService.cs` | Create | Service interface |
| `src/Infinity.WebApplication/Services/AudioService/AudioService.cs` | Create | MIDI parsing + response caching |
| `src/Infinity.WebApplication/Controllers/AudioController.cs` | Create | `GET /audio/notes` endpoint |
| `src/Infinity.WebApplication/Program.cs` | Modify | Register `IAudioService` |
| `src/Infinity.WebApplication.Tests/Stubs/StubAudioService.cs` | Create | Hardcoded test double for `IAudioService` |
| `src/Infinity.WebApplication.Tests/Controllers/AudioControllerTests.cs` | Create | NUnit tests for `AudioController` |
| `src/Infinity.WebApplication/wwwroot/js/pitchamon.js` | Create | Full client-side audio engine |
| `src/Infinity.WebApplication/Views/Shared/_Layout.cshtml` | Modify | Add Tone.js + pitchamon.js script tags |

---

### Task 1: Add DryWetMidi package and configure MIDI file as a content item

**Files:**
- Modify: `src/Infinity.WebApplication/Infinity.WebApplication.csproj`

- [ ] **Step 1: Add the NuGet package**

```bash
cd src/Infinity.WebApplication && dotnet add package Melanchall.DryWetMidi
```

Expected: package added, version printed (e.g. `7.x.x`).

- [ ] **Step 2: Add a content item so the MIDI file is published to the output directory**

Open `src/Infinity.WebApplication/Infinity.WebApplication.csproj` and add inside `<Project>`:

```xml
<ItemGroup>
    <Content Include="Audio\starwars.mid">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
</ItemGroup>
```

- [ ] **Step 3: Verify the project builds**

```bash
cd src && dotnet build Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/Infinity.WebApplication/Infinity.WebApplication.csproj
git commit -m "chore: add DryWetMidi package and configure MIDI file as content"
```

---

### Task 2: DTOs and IAudioService interface

**Files:**
- Create: `src/Infinity.WebApplication/Models/AudioDtos.cs`
- Create: `src/Infinity.WebApplication/Services/AudioService/IAudioService.cs`

- [ ] **Step 1: Create the DTOs**

Create `src/Infinity.WebApplication/Models/AudioDtos.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Infinity.WebApplication.Models;

public record NoteDto(
    [property: JsonPropertyName("midi")] int Midi,
    [property: JsonPropertyName("time")] double Time,
    [property: JsonPropertyName("duration")] double Duration
);

public record AudioNotesResponse(
    [property: JsonPropertyName("notes")] IEnumerable<NoteDto> Notes,
    [property: JsonPropertyName("loopStart")] double LoopStart,
    [property: JsonPropertyName("totalDuration")] double TotalDuration
);
```

- [ ] **Step 2: Create the interface**

Create `src/Infinity.WebApplication/Services/AudioService/IAudioService.cs`:

```csharp
using Infinity.WebApplication.Models;

namespace Infinity.WebApplication.Services.AudioService;

public interface IAudioService
{
    Task<AudioNotesResponse> GetNotesAsync();
}
```

- [ ] **Step 3: Verify the project builds**

```bash
cd src && dotnet build Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/Infinity.WebApplication/Models/AudioDtos.cs \
        src/Infinity.WebApplication/Services/AudioService/IAudioService.cs
git commit -m "feat: add AudioDtos and IAudioService interface"
```

---

### Task 3: AudioController — test first, then implement

**Files:**
- Create: `src/Infinity.WebApplication.Tests/Stubs/StubAudioService.cs`
- Create: `src/Infinity.WebApplication.Tests/Controllers/AudioControllerTests.cs`
- Create: `src/Infinity.WebApplication/Controllers/AudioController.cs`

- [ ] **Step 1: Create the test stub**

Create `src/Infinity.WebApplication.Tests/Stubs/StubAudioService.cs`:

```csharp
using Infinity.WebApplication.Models;
using Infinity.WebApplication.Services.AudioService;

namespace Infinity.WebApplication.Tests.Stubs;

public class StubAudioService : IAudioService
{
    public Task<AudioNotesResponse> GetNotesAsync() =>
        Task.FromResult(new AudioNotesResponse(
            Notes: new[] { new NoteDto(62, 0.0, 0.4), new NoteDto(67, 0.45, 0.4) },
            LoopStart: 3.85,
            TotalDuration: 10.0
        ));
}
```

- [ ] **Step 2: Write the failing tests**

Create `src/Infinity.WebApplication.Tests/Controllers/AudioControllerTests.cs`:

```csharp
using Infinity.WebApplication.Controllers;
using Infinity.WebApplication.Models;
using Infinity.WebApplication.Tests.Stubs;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApplication.Tests.Controllers;

public class AudioControllerTests
{
    private AudioController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new AudioController(new StubAudioService());
    }

    [Test]
    public async Task GetNotes_ReturnsOk()
    {
        var result = await _sut.GetNotes();
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetNotes_ReturnsAudioNotesResponse()
    {
        var result = await _sut.GetNotes();
        var ok = result as OkObjectResult;
        Assert.That(ok!.Value, Is.InstanceOf<AudioNotesResponse>());
    }

    [Test]
    public async Task GetNotes_ReturnsNonEmptyNotes()
    {
        var result = await _sut.GetNotes();
        var ok = result as OkObjectResult;
        var response = ok!.Value as AudioNotesResponse;
        Assert.That(response!.Notes, Is.Not.Empty);
    }

    [Test]
    public async Task GetNotes_ReturnsPositiveLoopStart()
    {
        var result = await _sut.GetNotes();
        var ok = result as OkObjectResult;
        var response = ok!.Value as AudioNotesResponse;
        Assert.That(response!.LoopStart, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetNotes_ReturnsTotalDurationGreaterThanLoopStart()
    {
        var result = await _sut.GetNotes();
        var ok = result as OkObjectResult;
        var response = ok!.Value as AudioNotesResponse;
        Assert.That(response!.TotalDuration, Is.GreaterThan(response.LoopStart));
    }
}
```

- [ ] **Step 3: Run the tests — expect failure (AudioController does not exist yet)**

```bash
cd src && dotnet test Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "AudioControllerTests"
```

Expected: build error — `AudioController` not found.

- [ ] **Step 4: Implement the controller**

Create `src/Infinity.WebApplication/Controllers/AudioController.cs`:

```csharp
using Infinity.WebApplication.Services.AudioService;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApplication.Controllers;

[ApiController]
[Route("audio")]
public class AudioController : ControllerBase
{
    private readonly IAudioService _audioService;

    public AudioController(IAudioService audioService)
    {
        _audioService = audioService;
    }

    [HttpGet("notes")]
    public async Task<IActionResult> GetNotes()
    {
        var response = await _audioService.GetNotesAsync();
        return Ok(response);
    }
}
```

- [ ] **Step 5: Run the tests — expect all passing**

```bash
cd src && dotnet test Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "AudioControllerTests"
```

Expected:
```
Passed! - Failed: 0, Passed: 5, Skipped: 0
```

- [ ] **Step 6: Commit**

```bash
git add src/Infinity.WebApplication/Controllers/AudioController.cs \
        src/Infinity.WebApplication.Tests/Stubs/StubAudioService.cs \
        src/Infinity.WebApplication.Tests/Controllers/AudioControllerTests.cs
git commit -m "feat: add AudioController with tests"
```

---

### Task 4: AudioService implementation and DI registration

**Files:**
- Create: `src/Infinity.WebApplication/Services/AudioService/AudioService.cs`
- Modify: `src/Infinity.WebApplication/Program.cs`

- [ ] **Step 1: Implement AudioService**

Create `src/Infinity.WebApplication/Services/AudioService/AudioService.cs`:

```csharp
using Infinity.WebApplication.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace Infinity.WebApplication.Services.AudioService;

public class AudioService : IAudioService
{
    private readonly AudioNotesResponse _cached;

    public AudioService(IWebHostEnvironment env)
    {
        var midiPath = Path.Combine(env.ContentRootPath, "Audio", "starwars.mid");
        _cached = ParseMidi(midiPath);
    }

    public Task<AudioNotesResponse> GetNotesAsync() => Task.FromResult(_cached);

    private static AudioNotesResponse ParseMidi(string midiPath)
    {
        var midiFile = MidiFile.Read(midiPath);
        var tempoMap = midiFile.GetTempoMap();

        var notes = midiFile.GetNotes()
            .OrderBy(n => n.Time)
            .Select(n =>
            {
                var time = n.TimeAs<MetricTimeSpan>(tempoMap);
                var length = n.LengthAs<MetricTimeSpan>(tempoMap);
                return new NoteDto(
                    Midi: n.NoteNumber,
                    Time: time.TotalMicroseconds / 1_000_000.0,
                    Duration: length.TotalMicroseconds / 1_000_000.0
                );
            })
            .ToList();

        var measure4Start = new BarBeatTicksTimeSpan(3, 0, 0);
        var loopStartSpan = TimeConverter.ConvertTo<MetricTimeSpan>(measure4Start, tempoMap);
        var loopStart = loopStartSpan.TotalMicroseconds / 1_000_000.0;
        var totalDuration = notes.Max(n => n.Time + n.Duration);

        return new AudioNotesResponse(notes, loopStart, totalDuration);
    }
}
```

- [ ] **Step 2: Register AudioService in DI**

In `src/Infinity.WebApplication/Program.cs`, add after the existing `AddScoped<IReviewService, ReviewService>()` line:

```csharp
builder.Services.AddSingleton<IAudioService, AudioService>();
```

Also add the using at the top of the file:

```csharp
using Infinity.WebApplication.Services.AudioService;
```

- [ ] **Step 3: Build the project**

```bash
cd src && dotnet build Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Run the app and verify the endpoint manually**

```bash
cd src && dotnet run --project Infinity.WebApplication/Infinity.WebApplication.csproj
```

Open `http://localhost:5000/audio/notes` (or whichever port is shown).

Expected response shape:
```json
{
  "notes": [
    { "midi": 67, "time": 0.0, "duration": 0.45 },
    ...
  ],
  "loopStart": 3.85,
  "totalDuration": 92.0
}
```

Verify: `notes` is a non-empty array, `loopStart` is a positive number, `totalDuration` is greater than `loopStart`.

> **If the MIDI file has multiple tracks and returns duplicate notes:** change `midiFile.GetNotes()` to `midiFile.GetTrackChunks().First().GetNotes()` to read only the first track.

- [ ] **Step 5: Commit**

```bash
git add src/Infinity.WebApplication/Services/AudioService/AudioService.cs \
        src/Infinity.WebApplication/Program.cs
git commit -m "feat: implement AudioService with MIDI parsing and DI registration"
```

---

### Task 5: pitchamon.js — audio engine

**Files:**
- Create: `src/Infinity.WebApplication/wwwroot/js/pitchamon.js`

- [ ] **Step 1: Create the module**

Create `src/Infinity.WebApplication/wwwroot/js/pitchamon.js`:

```javascript
(function () {
    'use strict';

    let activeSampler = null;
    let activePart = null;

    // Autocorrelation pitch detection.
    // Returns the nearest MIDI note number for the dominant frequency in audioBuffer.
    function detectPitch(audioBuffer) {
        const sampleRate = audioBuffer.sampleRate;
        const channelData = audioBuffer.getChannelData(0);
        const sampleCount = Math.min(Math.floor(sampleRate * 0.2), channelData.length);

        // Search range: 80 Hz (maxLag) to 2000 Hz (minLag)
        const minLag = Math.floor(sampleRate / 2000);
        const maxLag = Math.floor(sampleRate / 80);

        let bestLag = minLag;
        let bestCorrelation = -Infinity;

        for (let lag = minLag; lag <= maxLag; lag++) {
            let sum = 0;
            const limit = sampleCount - lag;
            for (let i = 0; i < limit; i++) {
                sum += channelData[i] * channelData[i + lag];
            }
            if (sum > bestCorrelation) {
                bestCorrelation = sum;
                bestLag = lag;
            }
        }

        const frequency = sampleRate / bestLag;
        return Math.round(12 * Math.log2(frequency / 440) + 69);
    }

    // Fetches a cry .wav, decodes it, detects its pitch, and returns a ready Tone.Sampler.
    async function createSampler(url) {
        const response = await fetch(url);
        const arrayBuffer = await response.arrayBuffer();
        const audioBuffer = await Tone.context.decodeAudioData(arrayBuffer);
        const baseNote = detectPitch(audioBuffer);
        const baseNoteStr = Tone.Frequency(baseNote, 'midi').toNote();

        const sampler = new Tone.Sampler({ [baseNoteStr]: url }).toDestination();
        await Tone.loaded();
        return sampler;
    }

    // Builds a Tone.Part from the note array returned by /audio/notes.
    function buildPart(notes) {
        if (activePart) {
            activePart.dispose();
        }
        const events = notes.map(n => [n.time, { midi: n.midi, duration: n.duration }]);
        activePart = new Tone.Part((time, value) => {
            if (activeSampler) {
                activeSampler.triggerAttackRelease(
                    Tone.Frequency(value.midi, 'midi').toNote(),
                    value.duration,
                    time
                );
            }
        }, events);
        activePart.start(0);
    }

    async function init() {
        const [notesData, sampler] = await Promise.all([
            fetch('/audio/notes').then(r => r.json()),
            createSampler('/audio/cry-default.wav')
        ]);

        activeSampler = sampler;
        buildPart(notesData.notes);

        Tone.Transport.loop = true;
        Tone.Transport.loopStart = notesData.loopStart;
        Tone.Transport.loopEnd = notesData.totalDuration;

        const startAudio = async () => {
            await Tone.start();
            Tone.Transport.start();
            document.removeEventListener('click', startAudio);
            document.removeEventListener('keydown', startAudio);
        };
        document.addEventListener('click', startAudio);
        document.addEventListener('keydown', startAudio);
    }

    // Public API — exposed for the future hot-swap dropdown feature.
    window.Pitchamon = {
        swapCry: async function (url) {
            const newSampler = await createSampler(url);
            const old = activeSampler;
            activeSampler = newSampler;
            if (old) setTimeout(() => old.dispose(), 2000);
        }
    };

    init().catch(err => console.error('[Pitchamon] init failed:', err));
})();
```

- [ ] **Step 2: Commit**

```bash
git add src/Infinity.WebApplication/wwwroot/js/pitchamon.js
git commit -m "feat: add pitchamon.js audio engine"
```

---

### Task 6: Wire up _Layout.cshtml and end-to-end test

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Add script tags to the layout**

In `src/Infinity.WebApplication/Views/Shared/_Layout.cshtml`, add these two lines immediately before `</body>` (after the existing Bootstrap and auth.js script tags):

```html
<script src="~/lib/tone/tone.min.js"></script>
<script src="~/js/pitchamon.js"></script>
```

The bottom of `<body>` should look like:

```html
<script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
<script src="~/js/auth.js"></script>
<partial name="_StarRatingScript" />
@await RenderSectionAsync("Scripts", required: false)
<script src="~/lib/tone/tone.min.js"></script>
<script src="~/js/pitchamon.js"></script>
</body>
```

- [ ] **Step 2: Run the app**

```bash
cd src && dotnet run --project Infinity.WebApplication/Infinity.WebApplication.csproj
```

- [ ] **Step 3: Open the browser and test**

Open `http://localhost:5000` (or the port shown). Open DevTools → Console.

Verify before clicking:
- No console errors on load
- `[Pitchamon] init failed` does NOT appear

Click anywhere on the page. Verify:
- The Star Wars theme starts playing using the Pokémon cry sound
- Audio continues looping (the intro plays once, then the main theme loops from measure 4)

- [ ] **Step 4: Verify the loop**

Let the song play to completion. Verify:
- Playback jumps back to measure 4 (B-flat triplet) rather than the very beginning
- The intro does NOT replay on loop

- [ ] **Step 5: Run all tests to check for regressions**

```bash
cd src && dotnet test
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_Layout.cshtml
git commit -m "feat: wire up Tone.js and pitchamon.js in layout"
```
