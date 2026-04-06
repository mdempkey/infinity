using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApi.Controllers;

public interface IAttractionsController
{
    public Task<ActionResult<IEnumerable<Attraction>>> GetAttractions();
    public Task<ActionResult<Attraction>> GetAttraction(string id);
    public Task<ActionResult<Attraction>> AddAttraction(Attraction attraction);
    public Task<IActionResult> EditAttraction(string id, Attraction attraction);
    public Task<IActionResult> DeleteAttraction(string id);
}