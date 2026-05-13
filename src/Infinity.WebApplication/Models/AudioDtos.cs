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
