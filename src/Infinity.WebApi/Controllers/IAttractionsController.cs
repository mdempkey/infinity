using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApi.Controllers;

public interface IAttractionsController
{
    public Task<ActionResult<IEnumerable<Attraction>>> GetAttractions();
    public Task<ActionResult<Attraction>> GetAttraction(string id);
}