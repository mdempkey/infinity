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
