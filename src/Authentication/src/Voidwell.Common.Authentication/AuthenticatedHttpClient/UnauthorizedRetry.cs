using System.Net;
using Polly.Retry;

namespace Voidwell.Common.Authentication.AuthenticatedHttpClient;

/// <summary>Resends a request once, with a fresh token, when the server rejects the current one.</summary>
internal static class UnauthorizedRetry
{
    public static RetryStrategyOptions<HttpResponseMessage> CreateOptions(ITokenManager tokenManager)
    {
        return new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = 1,
            Delay = TimeSpan.Zero,
            ShouldHandle = args => ValueTask.FromResult(args.Outcome.Result is { StatusCode: HttpStatusCode.Unauthorized }),
            OnRetry = args =>
            {
                var response = args.Outcome.Result!;
                var rejectedToken = response.RequestMessage?.Headers.Authorization?.Parameter;
                if (rejectedToken is not null)
                {
                    tokenManager.Invalidate(rejectedToken);
                }

                response.Dispose();
                return default;
            }
        };
    }
}
