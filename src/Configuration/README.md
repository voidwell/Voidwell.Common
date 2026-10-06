# Voidwell.Common.Configuration

[![NuGet](https://img.shields.io/nuget/v/Voidwell.Common.Configuration.svg?style=for-the-badge)](https://www.nuget.org/packages/Voidwell.Common.Configuration/)

Configuration helpers, such as reading the application name.

## Installation

```shell
dotnet add package Voidwell.Common.Configuration
```

## Usage

```csharp
var applicationName = configuration.GetApplicationName();
```

Returns the configured `ApplicationName`, falling back to the entry assembly name when it isn't set.

## License

[MIT](../../LICENSE)
