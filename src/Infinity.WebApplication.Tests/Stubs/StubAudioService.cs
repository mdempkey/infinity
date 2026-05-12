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
