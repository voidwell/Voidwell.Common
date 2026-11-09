namespace Voidwell.Common.State.Tests;

public class TestService : StatefulHostedService
{
    public int StartCount { get; private set; }
    public int StopCount { get; private set; }
    public bool StartedUp { get; private set; }
    public bool FailStart { get; set; }

    public override Task OnApplicationStartup(CancellationToken cancellationToken)
    {
        StartedUp = true;
        return Task.CompletedTask;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (FailStart)
        {
            throw new InvalidOperationException("Start failed");
        }

        // Starting before startup preparation finished would be a bug in the manager
        Assert.True(StartedUp);
        StartCount++;
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        StopCount++;
        return Task.CompletedTask;
    }

    public override Task<object?> GetStatusAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<object?>("details");
    }
}

public class DefaultEnabledService : TestService
{
    public override bool DefaultEnabled => true;
}

public interface ICustomService
{
}

public class CustomService : TestService, ICustomService
{
}
