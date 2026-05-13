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
