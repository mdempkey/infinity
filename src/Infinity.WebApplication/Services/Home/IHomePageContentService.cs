using Infinity.WebApplication.ViewModels.Home;

namespace Infinity.WebApplication.Services.Home;

public interface IHomePageContentService
{
    HomeIndexViewModel BuildHomeIndexViewModel();
}
