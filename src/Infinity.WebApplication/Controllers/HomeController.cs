using Microsoft.AspNetCore.Mvc;
using Infinity.WebApplication.Services.Home;

namespace Infinity.WebApplication.Controllers;

public class HomeController : Controller
{
    private readonly IIndexContentService _indexContentService;

    public HomeController(IIndexContentService indexContentService)
    {
        _indexContentService = indexContentService;
    }

    public IActionResult Index()
    {
        return View(_indexContentService.BuildIndexViewModel());
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
