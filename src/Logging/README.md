# Voidwell.Common.Logging

[![NuGet](https://img.shields.io/nuget/v/Voidwell.Common.Logging.svg?style=for-the-badge)](https://www.nuget.org/packages/Voidwell.Common.Logging/)

Serilog-based logging setup for host applications.

## Installation

```shell
dotnet add package Voidwell.Common.Logging
```

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationLogging();
```

This replaces the default logging providers with a Serilog logger configured from the application's configuration (the `Serilog` section), enriched with the log context and an `Application` property taken from `ApplicationName`. Development logs to the console in a readable format; other environments log compact JSON.

There is also an `ILoggingBuilder` overload, `AddApplicationLogging(hostEnvironment, configuration)`, for setups that don't use a host application builder.

## License

[MIT](../../LICENSE)
