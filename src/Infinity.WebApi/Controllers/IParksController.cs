using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApi.Controllers;

public interface IParksController
{
    public Task<ActionResult<IEnumerable<Park>>> GetParks();
    public Task<ActionResult<Park>> GetPark(string id);
    public Task<ActionResult<Park>> AddPark(Park park);
    public Task<IActionResult> EditPark(string id, Park park);
    public Task<IActionResult> DeletePark(string id);
}