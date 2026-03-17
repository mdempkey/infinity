using Infinity.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class StringsController(IStringService stringService) : ControllerBase
{
    [HttpGet("reverse/{input}")]
    public IActionResult Reverse(string input)
    {
        return Ok(stringService.Reverse(input));
    }
    
    [HttpGet("reverseWords/{input}")]
    public IActionResult ReverseWords(string input)
    {
        return Ok(stringService.ReverseWords(input));
    }
}
