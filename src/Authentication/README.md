# Voidwell.Common.Authentication

[![NuGet](https://img.shields.io/nuget/v/Voidwell.Common.Authentication.svg?style=for-the-badge)](https://www.nuget.org/packages/Voidwell.Common.Authentication/)

Authenticated `HttpClient` support with client-credentials bearer tokens, and JWT / reference-token authentication for services.

## Installation

```shell
dotnet add package Voidwell.Common.Authentication
```

## Usage

### Authenticated `HttpClient`

Adds an OAuth2 client-credentials bearer token to every outgoing request and caches it until it is about to expire. If the server answers `401 Unauthorized`, the rejected token is discarded and the request is resent once with a fresh one.

```csharp
services.AddHttpClient<IMyClient, MyClient>(client => client.BaseAddress = new Uri("https://api.example.com"))
    .AddTokenHandler((sp, options) =>
    {
        options.TokenServiceAddress = "https://auth.example.com/connect/token";
        options.ClientId = "my-client";
        options.ClientSecret = "secret";
        options.ClientScopes = ["api.read"];
    });
```

The options are validated at startup. Options and the token cache are keyed by the HTTP client's name, so each client has its own token and the name must be unique.

### Service authentication

Validates incoming bearer tokens as JWTs, as opaque reference tokens (via OAuth2 token introspection), or both.

```csharp
builder.Services.AddAuthentication()
    .AddServiceAuthentication("service", options =>
    {
        options.Authority = "https://auth.example.com";
        options.SupportedTokens = SupportedTokens.Both;
        options.Audience = "my-api";          // JWT
        options.ClientId = "my-api";          // reference tokens
        options.ClientSecret = "secret";
        options.EnableCaching = true;
        options.CacheDuration = TimeSpan.FromMinutes(5);
    });
```

| `SupportedTokens` | Behavior |
| --- | --- |
| `Jwt` | JWT bearer validation only. |
| `Reference` | Introspection only. |
| `Both` | JWT bearer validation, with opaque tokens (bearer values without a `.`) forwarded to introspection. |

Introspection uses [Duende.AspNetCore.Authentication.OAuth2Introspection](https://github.com/duendesoftware/foss) and requires the authority's discovery document to advertise an `introspection_endpoint`. When `EnableCaching` is off, introspection results are not cached.

## License

[MIT](../../LICENSE)
