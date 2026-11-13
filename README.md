# Voidwell.Common

[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/voidwell/voidwell.common/build-test.yml?branch=main&style=for-the-badge)](https://github.com/voidwell/voidwell.common/actions/workflows/build-test.yml)
[![Latest Release](https://img.shields.io/github/v/release/voidwell/voidwell.common?style=for-the-badge)](https://github.com/voidwell/voidwell.common/releases/latest)
[![MIT License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

Shared libraries for Voidwell applications.

## Packages

| Package | Description |
| --- | --- |
| [Voidwell.Common.Authentication](src/Authentication) | Authenticated `HttpClient` support with client-credentials bearer tokens, and JWT / reference-token authentication for services. |
| [Voidwell.Common.Cache](src/Cache) | A FusionCache-based cache with optional Redis support and set-like lists. |
| [Voidwell.Common.Configuration](src/Configuration) | Configuration helpers and a runtime-editable application configuration store. |
| [Voidwell.Common.Configuration.EntityFramework](src/Configuration) | Keeps the application configuration store in an Entity Framework Core table. |
| [Voidwell.Common.Logging](src/Logging) | Serilog-based logging setup for host applications. |
| [Voidwell.Common.RateLimiting](src/RateLimiting) | Per-client rate limiting for APIs, built on the ASP.NET Core rate limiter. |
| [Voidwell.Common.State](src/State) | Hosted services that can be enabled and disabled at runtime. |
| [Voidwell.Common.Swagger](src/Swagger) | Swagger / OpenAPI setup for APIs |

## License

[MIT](LICENSE)
