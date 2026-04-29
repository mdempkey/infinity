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

    public async Task<IActionResult> Index()
    {
        return View(await _indexContentService.BuildIndexViewModelAsync());
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
