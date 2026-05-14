# Select Pokémon Singer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect the Pokémon dropdown in the navbar to the `pitchamon.js` audio engine so selecting a Pokémon hot-swaps the cry instrument mid-song without restarting playback.

**Architecture:** Add `GET /api/pokemon/{name}/cry` to `WebApplication.PokemonController` as a same-origin proxy that streams cry audio from the Pokémon API container (reusing the existing `PokemonApi` HttpClient with bearer auth). Add a `pokemon:selected` event listener inside `_Header.cshtml`'s existing IIFE that calls `window.Pitchamon.swapCry()` with the proxy URL and updates `#pokemonSoundStatus`. Remove the dead audio-player script block from `Index.cshtml`.

**Tech Stack:** C# / ASP.NET Core MVC, NUnit, JavaScript (ES2017+), Tone.js

---

## File Structure

| File | Change | Responsibility |
|------|--------|----------------|
| `src/Infinity.WebApplication/Controllers/PokemonController.cs` | Modify | Add `GET /api/pokemon/{name}/cry` proxy action |
| `src/Infinity.WebApplication.Tests/Controllers/PokemonControllerTests.cs` | Create | NUnit tests for `GetCry` action |
| `src/Infinity.WebApplication/Views/Shared/_Header.cshtml` | Modify | Add `pokemon:selected` event listener with status feedback |
| `src/Infinity.WebApplication/Views/Home/Index.cshtml` | Modify | Remove dead `playBtn`/`audioPlayer` script block |

---

### Task 1: Add `GetCry` to `PokemonController` — test first, then implement

**Files:**
- Modify: `src/Infinity.WebApplication/Controllers/PokemonController.cs`
- Create: `src/Infinity.WebApplication.Tests/Controllers/PokemonControllerTests.cs`

- [ ] **Step 1: Create the test file with fakes and failing tests**

Create `src/Infinity.WebApplication.Tests/Controllers/PokemonControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using Infinity.WebApplication.Controllers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Infinity.WebApplication.Tests.Controllers;

internal sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(response);
}

internal sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}

public class PokemonControllerTests
{
    private static PokemonController CreateSut(HttpResponseMessage upstreamResponse)
    {
        var handler = new FakeHttpMessageHandler(upstreamResponse);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        return new PokemonController(new FakeHttpClientFactory(client));
    }

    [Test]
    public async Task GetCry_ReturnsFileResult_WhenUpstreamSucceeds()
    {
        var upstream = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };
        upstream.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

        var result = await CreateSut(upstream).GetCry("pikachu", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<FileStreamResult>());
    }

    [Test]
    public async Task GetCry_ContentTypeMatchesUpstream()
    {
        var upstream = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };
        upstream.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/ogg");

        var result = await CreateSut(upstream).GetCry("pikachu", CancellationToken.None) as FileStreamResult;

        Assert.That(result!.ContentType, Is.EqualTo("audio/ogg"));
    }

    [Test]
    public async Task GetCry_Returns404_WhenUpstreamReturns404()
    {
        var result = await CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound))
            .GetCry("missingmon", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetCry_Returns502_WhenUpstreamReturnsError()
    {
        var result = await CreateSut(new HttpResponseMessage(HttpStatusCode.InternalServerError))
            .GetCry("pikachu", CancellationToken.None) as StatusCodeResult;

        Assert.That(result?.StatusCode, Is.EqualTo(502));
    }
}
```

- [ ] **Step 2: Run the tests — expect build failure**

```bash
cd src && dotnet test Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "PokemonControllerTests"
```

Expected: build error — `GetCry` method does not exist on `PokemonController`.

- [ ] **Step 3: Add `GetCry` to `PokemonController`**

Open `src/Infinity.WebApplication/Controllers/PokemonController.cs` and add this action inside the class, after the existing `GetAll` action:

```csharp
[HttpGet("{name}/cry")]
public async Task<IActionResult> GetCry(string name, CancellationToken cancellationToken)
{
    var client = httpClientFactory.CreateClient("PokemonApi");
    var response = await client.GetAsync($"pokemon/{name}/cry", cancellationToken);

    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        return NotFound();

    if (!response.IsSuccessStatusCode)
        return StatusCode(502);

    var contentType = response.Content.Headers.ContentType?.MediaType ?? "audio/wav";
    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    return File(stream, contentType);
}
```

- [ ] **Step 4: Run the tests — expect all passing**

```bash
cd src && dotnet test Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "PokemonControllerTests"
```

Expected:
```
Passed! - Failed: 0, Passed: 4, Skipped: 0
```

- [ ] **Step 5: Run the full test suite to check for regressions**

```bash
cd src && dotnet test
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Infinity.WebApplication/Controllers/PokemonController.cs \
        src/Infinity.WebApplication.Tests/Controllers/PokemonControllerTests.cs
git commit -m "feat: add GetCry proxy endpoint to PokemonController"
```

---

### Task 2: Wire `pokemon:selected` event in `_Header.cshtml`

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_Header.cshtml`

- [ ] **Step 1: Add the event listener inside the DOMContentLoaded callback**

Open `src/Infinity.WebApplication/Views/Shared/_Header.cshtml`.

Find the `selector.addEventListener("change", () => { ... });` block near the bottom of the `DOMContentLoaded` callback. It ends with `});`. Add the following immediately after that `});`, still inside the `DOMContentLoaded` callback (before its closing `});`):

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

The tail of the `DOMContentLoaded` callback should now read:

```js
                selector.addEventListener("change", () => {
                    const selectedOption = selector.options[selector.selectedIndex];

                    if (!selectedOption || !selectedOption.value) {
                        return;
                    }

                    const selectedPokemon = {
                        id: Number(selectedOption.dataset.id),
                        name: selectedOption.value,
                        cry: selectedOption.dataset.cry
                    };

                    setStatus(`Selected ${selectedOption.textContent}`);

                    window.dispatchEvent(new CustomEvent("pokemon:selected", {
                        detail: selectedPokemon
                    }));
                });

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
            });
        })();
```

- [ ] **Step 2: Build the project to verify no Razor syntax errors**

```bash
cd src && dotnet build Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_Header.cshtml
git commit -m "feat: wire pokemon:selected event to Pitchamon.swapCry"
```

---

### Task 3: Remove dead audio-player code from `Index.cshtml`

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Home/Index.cshtml`

- [ ] **Step 1: Delete the dead `playBtn`/`audioPlayer` block**

Open `src/Infinity.WebApplication/Views/Home/Index.cshtml`.

Inside the `@section Scripts { <script> ... </script> }` block, find and delete only the following block (leave the `attractionSort`, magic cursor, and `pokemonSelector` dance animation code above it intact):

```js
        const playBtn = document.getElementById('playSongBtn');
        const selector = document.getElementById('pokemonSelector');
        const audioPlayer = document.getElementById('starWarsAudio');

        if (playBtn && selector && audioPlayer) {
            playBtn.addEventListener('click', function() {
                const selectedOption = selector.options[selector.selectedIndex];

                if (!selectedOption.value) {
                    alert("Please select a Pokémon first!");
                    return;
                }

                const songType = selectedOption.getAttribute('data-song');

                const songDirectory = {
                    'cantina': '/audio/cantina-band.mp3',
                    'imperial': '/audio/imperial-march.mp3',
                    'main_theme': '/audio/star-wars-theme.mp3'
                };

                if (songDirectory[songType]) {
                    audioPlayer.src = songDirectory[songType];
                    audioPlayer.currentTime = 0;

                    audioPlayer.play()
                        .then(() => console.log("Now playing Star Wars audio for: " + selectedOption.value))
                        .catch(error => console.error("Error playing audio. Make sure the audio files exist in wwwroot/audio:", error));
                }
            });
        }
```

- [ ] **Step 2: Build and run tests to verify nothing broke**

```bash
cd src && dotnet build Infinity.WebApplication/Infinity.WebApplication.csproj && dotnet test
```

Expected: `Build succeeded.` and all tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Views/Home/Index.cshtml
git commit -m "chore: remove dead audio-player script from Index.cshtml"
```
