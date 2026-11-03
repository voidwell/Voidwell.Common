using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Voidwell.Common.Logging.Tests;

[Collection("StaticLogger")]
public class HostApplicationBuilderExtensionsTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void AddApplicationLogging_CreatesWorkingLogger(string environmentName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = environmentName });

        var result = builder.AddApplicationLogging();
        using var host = builder.Build();

        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Test");
        var exception = Record.Exception(() => logger.LogInformation("Hello"));

        Assert.Same(builder, result);
        Assert.Null(exception);
    }

    [Fact]
    public void AddApplicationLogging_AssignsStaticLogger()
    {
        var before = Serilog.Log.Logger;
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Production" });

        builder.AddApplicationLogging();

        Assert.NotSame(before, Serilog.Log.Logger);
    }
}
