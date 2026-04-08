using Microsoft.AspNetCore.Mvc;
using Infinity.WebApplication.Services.Home;

namespace Infinity.WebApplication.Controllers;

public class HomeController : Controller
{
    private readonly IHomePageContentService _homePageContentService;

    public HomeController(IHomePageContentService homePageContentService)
    {
        _homePageContentService = homePageContentService;
    }

    public IActionResult Index()
    {
        return View(_homePageContentService.BuildHomeIndexViewModel());
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
