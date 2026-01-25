using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Local_Service_Manager.Tests;

public class SmokeTests : IClassFixture<WebApplicationFactory<Local_Service_Manager.Program>>
{
    private readonly WebApplicationFactory<Local_Service_Manager.Program> _factory;

    public SmokeTests(WebApplicationFactory<Local_Service_Manager.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Public_Services_Page_Returns_Ok()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/ServicePublic/Index");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Admin_Area_Requires_Login()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/Admin/Dashboard");
        Assert.True(resp.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Api_Services_Returns_Ok()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/api/services?onlyActive=true");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
