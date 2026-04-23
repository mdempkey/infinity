using Infinity.WebApplication.ViewModels.Home;

namespace Infinity.WebApplication.Services.Home;

public interface IIndexContentService
{
    Task<IndexViewModel> BuildIndexViewModelAsync();
}
