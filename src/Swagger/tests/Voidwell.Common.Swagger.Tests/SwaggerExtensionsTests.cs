using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voidwell.Common.Swagger.Tests;

public class SwaggerExtensionsTests
{
    [Fact]
    public async Task Document_UsesTheApplicationNameAsTitle()
    {
        using var host = await StartAsync(new Dictionary<string, string?> { ["ApplicationName"] = "My.Api" });

        using var document = await GetDocumentAsync(host);

        var info = document.RootElement.GetProperty("info");
        Assert.Equal("My.Api", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Document_WithoutApplicationName_FallsBackToTheEntryAssemblyName()
    {
        using var host = await StartAsync();

        using var document = await GetDocumentAsync(host);

        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("info").GetProperty("title").GetString()));
    }

    [Fact]
    public async Task Document_DefinesTheBearerScheme()
    {
        using var host = await StartAsync();

        using var document = await GetDocumentAsync(host);

        var bearer = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
    }

    [Fact]
    public async Task Document_MarksOnlyAuthorizedOperationsWithSecurity()
    {
        using var host = await StartAsync();

        using var document = await GetDocumentAsync(host);

        var paths = document.RootElement.GetProperty("paths");
        Assert.False(paths.GetProperty("/test/public").GetProperty("get").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/test/opt-out").GetProperty("get").TryGetProperty("security", out _));
        Assert.True(paths.GetProperty("/test/secure").GetProperty("get").TryGetProperty("security", out var security));
        Assert.Equal(1, security.GetArrayLength());
        Assert.True(security[0].TryGetProperty("Bearer", out _));
    }

    [Fact]
    public async Task Document_AddsUnauthorizedAndForbiddenResponses_OnlyToAuthorizedOperations()
    {
        using var host = await StartAsync();

        using var document = await GetDocumentAsync(host);

        var paths = document.RootElement.GetProperty("paths");
        var secure = paths.GetProperty("/test/secure").GetProperty("get").GetProperty("responses");
        Assert.True(secure.TryGetProperty("401", out _));
        Assert.True(secure.TryGetProperty("403", out _));

        var anonymous = paths.GetProperty("/test/public").GetProperty("get").GetProperty("responses");
        Assert.False(anonymous.TryGetProperty("401", out _));
        Assert.False(anonymous.TryGetProperty("403", out _));
    }

    [Fact]
    public async Task UseApiSwagger_ServesTheUiForTheDocument()
    {
        using var host = await StartAsync(new Dictionary<string, string?> { ["ApplicationName"] = "My.Api" });
        using var client = host.GetTestClient();

        // The UI page loads its endpoint list and names from this script
        var script = await client.GetStringAsync("/swagger/index.js", TestContext.Current.CancellationToken);

        Assert.Contains("My.Api v1", script);
        Assert.Contains("/swagger/v1/swagger.json", script);
    }

    private static async Task<IHost> StartAsync(Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build();

        var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddRouting();
                    services.AddControllers().AddApplicationPart(typeof(TestController).Assembly);
                    services.AddAuthentication();
                    services.AddAuthorization();
                    services.AddApiSwagger(configuration);
                })
                .Configure(app =>
                {
                    app.UseApiSwagger();
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        return host;
    }

    private static async Task<JsonDocument> GetDocumentAsync(IHost host)
    {
        using var client = host.GetTestClient();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        return JsonDocument.Parse(json);
    }
}

[ApiController]
[Route("test")]
public class TestController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult Public() => Ok();

    [Authorize]
    [HttpGet("secure")]
    public IActionResult Secure() => Ok();

    [Authorize]
    [AllowAnonymous]
    [HttpGet("opt-out")]
    public IActionResult OptOut() => Ok();
}
