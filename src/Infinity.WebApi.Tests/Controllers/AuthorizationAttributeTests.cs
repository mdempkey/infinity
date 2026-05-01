using System.Reflection;
using Infinity.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace Infinity.WebApi.Tests.Controllers;

public class AuthorizationAttributeTests
{
    [Theory]
    [InlineData(typeof(AttractionsController))]
    [InlineData(typeof(ParksController))]
    [InlineData(typeof(ImagesController))]
    public void Controller_HasAuthorizeAttribute(Type controllerType)
    {
        var attr = controllerType.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        Assert.NotNull(attr);
    }
}
