# Voidwell.Common.Swagger

[![NuGet](https://img.shields.io/nuget/v/Voidwell.Common.Swagger.svg?style=for-the-badge)](https://www.nuget.org/packages/Voidwell.Common.Swagger/)

Swagger / OpenAPI setup for APIs, with optional bearer authentication.

## Installation

```shell
dotnet add package Voidwell.Common.Swagger
```

## Usage

```csharp
builder.Services.AddApiSwagger(builder.Configuration);

var app = builder.Build();
app.UseApiSwagger();
```

The document title is the configured `ApplicationName`, falling back to the entry assembly name. It is built on [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore).

Bearer authentication is optional: the scheme is defined once, and only operations that require authorization (`[Authorize]` without `[AllowAnonymous]`) are marked with the security requirement and documented with `401` and `403` responses. Anonymous endpoints can be called from the Swagger UI without a token.

## License

[MIT](../../LICENSE)
