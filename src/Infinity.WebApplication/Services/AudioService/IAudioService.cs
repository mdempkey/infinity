using Infinity.WebApplication.Models;

namespace Infinity.WebApplication.Services.AudioService;

public interface IAudioService
{
    Task<AudioNotesResponse> GetNotesAsync();
}
