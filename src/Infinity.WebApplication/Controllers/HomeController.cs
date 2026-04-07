using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApplication.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Disneyland()
    {
        return View();
    }

    public IActionResult DisneylandParis()
    {
        return View();
    }
}
