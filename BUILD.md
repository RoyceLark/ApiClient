# Building CoreApiClient

This document explains how to build, test, and package CoreApiClient.

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 / VS Code / Rider (optional)

## Building the Solution

### Command Line

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Visual Studio

1. Open `CoreApiClient.sln`
2. Build > Build Solution (Ctrl+Shift+B)

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Running Examples

```bash
cd examples/CoreApiClient.Examples
dotnet run
```

The example application will demonstrate:
- Basic API client usage
- Configuration options
- Dependency injection
- Error handling
- Advanced features

## Creating NuGet Package

### Automatic (on Build)

The project is configured to create a NuGet package automatically when building in Release mode:

```bash
dotnet build -c Release
```

The package will be created in: `src/CoreApiClient/bin/Release/CoreApiClient.1.0.0.nupkg`

### Manual Packaging

```bash
cd src/CoreApiClient
dotnet pack -c Release
```

### Pack with Specific Version

```bash
dotnet pack -c Release /p:Version=1.0.1
```

## Publishing to NuGet

### Prerequisites

1. Create an account on [nuget.org](https://www.nuget.org/)
2. Generate an API key from your account settings

### Publish Command

```bash
dotnet nuget push src/CoreApiClient/bin/Release/CoreApiClient.1.0.0.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
```

### Publish Symbol Package

```bash
dotnet nuget push src/CoreApiClient/bin/Release/CoreApiClient.1.0.0.snupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
```

## Project Structure

```
CoreApiClient/
├── src/
│   └── CoreApiClient/              # Main library
│       ├── Attributes/             # HTTP attributes
│       ├── Configuration/          # Options and settings
│       ├── Exceptions/             # Custom exceptions
│       ├── Extensions/             # DI extensions
│       ├── Internal/               # Internal implementation
│       └── ApiClient.cs            # Main factory
├── tests/
│   └── CoreApiClient.Tests/        # Unit tests
├── examples/
│   └── CoreApiClient.Examples/     # Example usage
├── README.md                       # Documentation
├── LICENSE                         # MIT License
└── CoreApiClient.sln              # Solution file
```

## Code Quality

### Formatting

The project uses standard .NET formatting conventions. To format code:

```bash
dotnet format
```

### Analyzers

The project includes Roslyn analyzers to ensure code quality. Build warnings should be treated as errors in Release mode.

## Continuous Integration

Example GitHub Actions workflow (`.github/workflows/ci.yml`):

```yaml
name: CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Pack
      run: dotnet pack -c Release --no-build
```

## Version Management

The package version is defined in `CoreApiClient.csproj`:

```xml
<Version>1.0.0</Version>
```

### Semantic Versioning

Follow [Semantic Versioning 2.0.0](https://semver.org/):

- **MAJOR** version when you make incompatible API changes
- **MINOR** version when you add functionality in a backwards compatible manner
- **PATCH** version when you make backwards compatible bug fixes

### Updating Version

1. Update the `<Version>` tag in `CoreApiClient.csproj`
2. Update the `<PackageReleaseNotes>` with changes
3. Update README.md if needed
4. Commit changes
5. Create a git tag: `git tag v1.0.1`
6. Push tag: `git push origin v1.0.1`

## Documentation

### XML Documentation

The project generates XML documentation files automatically. Ensure all public APIs are documented with XML comments:

```csharp
/// <summary>
/// Creates an API client implementation from an interface.
/// </summary>
/// <typeparam name="T">The API interface type.</typeparam>
/// <param name="httpClient">The HttpClient to use for requests.</param>
/// <returns>An implementation of the API interface.</returns>
public static T Create<T>(HttpClient httpClient) where T : class
{
    // ...
}
```

## Troubleshooting

### Build Errors

**"The type or namespace name could not be found"**
- Run `dotnet restore` to restore NuGet packages

**"The SDK 'Microsoft.NET.Sdk' specified could not be found"**
- Ensure .NET 8.0 SDK is installed: `dotnet --version`

### Test Failures

Run tests with verbose output to see detailed error messages:

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Package Issues

**"Package already exists"**
- Increment the version number in the project file

**"Invalid API key"**
- Verify your NuGet API key is correct and has push permissions

## Additional Resources

- [.NET SDK Documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [NuGet Documentation](https://docs.microsoft.com/en-us/nuget/)
- [Creating NuGet Packages](https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package)
