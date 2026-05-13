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
