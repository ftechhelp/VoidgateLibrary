# VoidgateLibrary

A lightweight C# client for the Voidgate HTTP API. It wraps the API endpoints for health checks, ad‑hoc command execution, and running scripts, handling JSON serialization (snake_case payloads) and error mapping.

## Requirements

- .NET SDK compatible with `net10.0` (the library and tests target `net10.0`).
- A running Voidgate server (defaults to `http://127.0.0.1:5000`).

## Solution Layout

```
VoidgateLibrary.sln
VoidgateLibrary/            # Class library
  VoidgateClient.cs         # Main HTTP client
  VoidgateClientOptions.cs  # BaseUrl, Password, Timeout
  VoidgateApiException.cs   # API error surface
  Models/                   # Request/response DTOs
VoidgateLibrary.Tests/      # xUnit tests + stub HTTP handler
```

## Install / Use

You can consume this library via a project reference or by packing a NuGet package.

### Option 1: Project reference (recommended during development)

1) Add the existing project to your solution (if needed):

```bash
# From your solution directory
dotnet sln add ./VoidgateLibrary/VoidgateLibrary.csproj
```

2) Add a project reference from your app/test project:

```bash
# From your app project directory
dotnet add reference ../VoidgateLibrary/VoidgateLibrary.csproj
```

### Option 2: Pack and install as a NuGet package

```bash
# Create a package
dotnet pack ./VoidgateLibrary/VoidgateLibrary.csproj -c Release -o ./nupkgs

# Consume from a local source (replace VERSION accordingly)
dotnet add package VoidgateLibrary --version <VERSION> --source ./nupkgs
```

> Note: No public NuGet feed is configured in this repo.

## Quick Start

```csharp
using VoidgateLibrary;
using VoidgateLibrary.Models;

// Option A: Construct with baseUrl/password
using var client = new VoidgateClient(
    baseUrl: "http://127.0.0.1:5000",
    password: "your-secret",
    timeout: TimeSpan.FromSeconds(30));

// Option B: Supply your own HttpClient + options
var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5000") };
using var client2 = new VoidgateClient(http, new VoidgateClientOptions
{
    Password = "your-secret",
    Timeout = TimeSpan.FromSeconds(30)
});

// Health
var health = await client.GetHealthAsync();
Console.WriteLine($"Status: {health.Status}");

// Execute a command
var exec = await client.ExecuteAsync("ls -la");
if (exec.Success)
{
    Console.WriteLine(exec.Stdout);
}
else
{
    Console.Error.WriteLine(exec.Stderr);
}

// Run a script with args and working directory
var run = await client.RunScriptAsync(
    scriptPath: "/abs/script.sh",
    args: new[] { "--flag", "value" },
    workingDir: "/abs/dir");
```

## API Overview

- `GetHealthAsync()` → `HealthResponse { Status }`
- `ExecuteAsync(string command, string? passwordOverride = null)`
- `ExecuteAsync(ExecuteRequest)` where `ExecuteRequest { password, command }`
- `RunScriptAsync(string scriptPath, IEnumerable<string>? args = null, string? workingDir = null, string? passwordOverride = null)`
- `RunScriptAsync(RunScriptRequest)` where `RunScriptRequest { password, script_path, args?, working_dir? }`

Responses:
- `ExecuteResponse` and `RunScriptResponse`: `{ stdout, stderr, return_code, success }` (+ script metadata for `RunScriptResponse`).

Errors:
- Non‑2xx responses throw `VoidgateApiException` with `StatusCode` and raw `ResponseBody` (if available).
- If no password is resolvable, the client throws `InvalidOperationException` with guidance to set it.

## Configuration

- `VoidgateClientOptions`:
  - `BaseUrl` (default: `http://127.0.0.1:5000`)
  - `Password` (required by server; can be passed per call as overrides)
  - `Timeout` (applies to the underlying `HttpClient`)

Password resolution order (first non‑empty wins):
1. Per‑call override (e.g., `ExecuteAsync(command, passwordOverride)`)
2. `VoidgateClientOptions.Password`
3. Otherwise: throws `InvalidOperationException`

## Development

Build and run tests:

```bash
# From repo root
dotnet build

dotnet test
```

Formatting/nullable: The project enables implicit usings and nullable reference types.

## Notes

- JSON payloads use snake_case field names to match the Voidgate server.
- If you target a different TFM in your app, ensure compatibility with `net10.0` or adjust the library's `TargetFramework` in `VoidgateLibrary.csproj` as needed.
