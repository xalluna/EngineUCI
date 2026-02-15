# EngineUCI.Core

A .NET library for communicating with Universal Chess Interface (UCI) compliant chess engines. This library provides a clean, async/await-friendly API for engine communication, position setup, move calculation, and position evaluation.

## Features

- **UCI Protocol Compliance**: Full implementation of the Universal Chess Interface protocol
- **Async/Await Support**: All operations are asynchronous with proper cancellation support
- **Thread-Safe**: Built-in locking mechanisms ensure safe concurrent operations
- **Dependency Injection**: Built-in factory pattern with Microsoft.Extensions.DependencyInjection support
- **Engine Pooling**: Resource pooling with configurable limits to manage multiple engine instances
- **Easy to Use**: Simple, intuitive API for common chess engine operations
- **Comprehensive**: Support for position setup (FEN/startpos), move calculation, evaluation, and engine management

## Installation

Add the EngineUCI.Core project to your solution or install via NuGet (when published):

```bash
dotnet add package EngineUCI.Core
```

## Quick Start

### Basic Usage

```csharp
using EngineUCI.Core.Engine;

// Create and start the engine
var engine = new UciEngine(@"C:\path\to\your\engine.exe");
engine.Start();

// Initialize the engine
await engine.EnsureInitializedAsync();

// Wait for engine to be ready
await engine.WaitIsReady();

// Set up the starting position
await engine.SetPositionAsync(); // Uses standard starting position

// Get the best move with default depth (20 plies)
string bestMove = await engine.GetBestMoveAsync();
Console.WriteLine($"Best move: {bestMove}"); // e.g., "e2e4"

// Clean up
engine.Dispose();
```

### Advanced Usage

#### Setting Custom Positions

```csharp
// Set position from FEN notation
string fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";
await engine.SetPositionAsync(fen, "");

// Set position from starting position with moves
await engine.SetPositionAsync("e2e4 e7e5 g1f3");

// Set position from FEN with additional moves
string fenPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
await engine.SetPositionAsync(fenPosition, "e2e4 e7e5");
```

#### Getting Best Moves

```csharp
// Get best move with custom search depth
string bestMove = await engine.GetBestMoveAsync(depth: 15);

// Get best move with time limit
string bestMove = await engine.GetBestMoveAsync(TimeSpan.FromSeconds(5));

// Get best move with cancellation support
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    string bestMove = await engine.GetBestMoveAsync(20, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Move calculation was cancelled");
}
```

#### Position Evaluation

```csharp
// Evaluate current position with default depth
string evaluation = await engine.EvaluateAsync();
Console.WriteLine($"Position evaluation: {evaluation} centipawns");

// Evaluate with custom depth
string evaluation = await engine.EvaluateAsync(depth: 18);

// Evaluate with time limit
string evaluation = await engine.EvaluateAsync(TimeSpan.FromSeconds(3));
```

## Dependency Injection & Engine Pooling

EngineUCI.Core provides built-in dependency injection support with engine pooling capabilities. This allows you to manage multiple engine instances efficiently in applications using Microsoft.Extensions.DependencyInjection.

### Overview

The dependency injection system includes:

- **IUciEngineFactory**: Factory interface for creating and managing engine instances
- **UciEngineFactory**: Implementation with built-in resource pooling using semaphores
- **Engine Pooling**: Configurable pool size to limit concurrent engine usage
- **Named Registrations**: Register multiple engine types with different configurations
- **Automatic Resource Management**: Engines automatically release pool resources on disposal

### Basic Setup

#### ASP.NET Core Integration

```csharp
using EngineUCI.Core.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configure the engine factory with dependency injection
builder.Services.UseUciEngineFactory(settings =>
{
    // Configure pool size (default: 16)
    settings.MaxPoolSize = 10;

    // Register named engines
    settings.RegisterNamedEngine("stockfish", () => new UciEngine(@"C:\engines\stockfish.exe"));
    settings.RegisterNamedEngine("komodo", () => new UciEngine(@"C:\engines\komodo.exe"));
});

var app = builder.Build();
```

#### Console Application with DI

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EngineUCI.Core.DependencyInjection;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.UseUciEngineFactory(settings =>
        {
            settings.MaxPoolSize = 5;
            settings.RegisterNamedEngine("stockfish", () => new UciEngine(@"C:\engines\stockfish.exe"));
        });
    })
    .Build();

// Get the factory from DI
var engineFactory = host.Services.GetRequiredService<IUciEngineFactory>();
```

### Advanced Configuration

#### Multiple Engine Types with Custom Settings

```csharp
builder.Services.UseUciEngineFactory(settings =>
{
    settings.MaxPoolSize = 20; // Allow up to 20 concurrent engines

    // High-performance Stockfish for analysis
    settings.RegisterNamedEngine("stockfish-analysis", () =>
    {
        var engine = new UciEngine(@"C:\engines\stockfish.exe");
        // Engine will be configured when retrieved
        return engine;
    });

    // Quick evaluation engine with different settings
    settings.RegisterNamedEngine("stockfish-quick", () =>
        new UciEngine(@"C:\engines\stockfish.exe"));

    // Different engine entirely
    settings.RegisterNamedEngine("komodo", () =>
        new UciEngine(@"C:\engines\komodo.exe"));
});
```

### Usage Patterns

#### In Controllers (ASP.NET Core)

```csharp
[ApiController]
[Route("api/[controller]")]
public class ChessAnalysisController : ControllerBase
{
    private readonly IUciEngineFactory _engineFactory;

    public ChessAnalysisController(IUciEngineFactory engineFactory)
    {
        _engineFactory = engineFactory;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzePosition([FromBody] AnalysisRequest request)
    {
        // Get engine from pool - automatically managed
        using var engine = await _engineFactory.GetEngineAsync("stockfish-analysis");

        try
        {
            engine.Start();
            await engine.EnsureInitializedAsync();
            await engine.WaitIsReady();

            await engine.SetPositionAsync(request.Fen, request.Moves);

            var bestMove = await engine.GetBestMoveAsync(request.Depth);
            var evaluation = await engine.EvaluateAsync(request.Depth);

            return Ok(new { BestMove = bestMove, Evaluation = evaluation });
        }
        catch (KeyNotFoundException)
        {
            return BadRequest("Engine 'stockfish-analysis' is not registered");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Engine error: {ex.Message}");
        }
        // Engine automatically disposed and pool resource released
    }
}
```

#### In Services

```csharp
public class ChessAnalysisService
{
    private readonly IUciEngineFactory _engineFactory;

    public ChessAnalysisService(IUciEngineFactory engineFactory)
    {
        _engineFactory = engineFactory;
    }

    public async Task<AnalysisResult> AnalyzePositionAsync(string engineName, string fen, int depth = 20)
    {
        using var engine = await _engineFactory.GetEngineAsync(engineName);

        engine.Start();
        await engine.EnsureInitializedAsync();
        await engine.SetPositionAsync(fen, "");

        var bestMoveTask = engine.GetBestMoveAsync(depth);
        var evaluationTask = engine.EvaluateAsync(depth);

        await Task.WhenAll(bestMoveTask, evaluationTask);

        return new AnalysisResult
        {
            BestMove = bestMoveTask.Result,
            Evaluation = evaluationTask.Result,
            EngineName = engineName
        };
    }
}
```

### Resource Management & Pool Behavior

#### Pool Limits

The engine factory uses a `SemaphoreSlim` to limit concurrent engine usage:

```csharp
// Configure pool size
settings.MaxPoolSize = 8; // Maximum 8 concurrent engines

// When pool is full:
var engine1 = await factory.GetEngineAsync("stockfish"); // Gets engine immediately
var engine2 = await factory.GetEngineAsync("stockfish"); // Gets engine immediately
// ... 8 engines acquired ...
var engine9 = await factory.GetEngineAsync("stockfish"); // Waits until an engine is disposed
```

#### Automatic Resource Release

Engines automatically release their pool slot when disposed:

```csharp
// Pool resource is automatically released when engine is disposed
using (var engine = await factory.GetEngineAsync("stockfish"))
{
    // Use engine...
} // Pool slot automatically released here

// Or with explicit disposal
var engine = await factory.GetEngineAsync("stockfish");
try
{
    // Use engine...
}
finally
{
    engine.Dispose(); // Pool slot released
}
```

### Error Handling

```csharp
try
{
    using var engine = await engineFactory.GetEngineAsync("nonexistent-engine");
    // ... use engine
}
catch (KeyNotFoundException ex)
{
    // Handle unregistered engine name
    _logger.LogError("Engine not found: {Message}", ex.Message);
    return BadRequest("Requested engine is not available");
}
catch (ObjectDisposedException)
{
    // Handle factory disposal (during shutdown)
    return StatusCode(503, "Service is shutting down");
}
```

### Best Practices

1. **Always Use `using` Statements**: Ensures proper disposal and pool resource release
2. **Configure Appropriate Pool Sizes**: Balance between resource usage and concurrent capacity
3. **Register Engines at Startup**: Avoid runtime registration for better performance
4. **Handle Missing Engines**: Always catch `KeyNotFoundException` for robustness
5. **Use Async Methods**: Prefer `GetEngineAsync()` to avoid blocking threads
6. **Monitor Pool Usage**: Consider logging or metrics to track pool utilization

### Complete Example

Here's a complete ASP.NET Core Web API example using dependency injection:

```csharp
using EngineUCI.Core.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers();
builder.Services.UseUciEngineFactory(settings =>
{
    settings.MaxPoolSize = 10;
    settings.RegisterNamedEngine("stockfish", () => new UciEngine(@"C:\engines\stockfish.exe"));
    settings.RegisterNamedEngine("komodo", () => new UciEngine(@"C:\engines\komodo.exe"));
});

var app = builder.Build();

app.MapControllers();
app.Run();

[ApiController]
[Route("api/chess")]
public class ChessController : ControllerBase
{
    private readonly IUciEngineFactory _engineFactory;

    public ChessController(IUciEngineFactory engineFactory)
    {
        _engineFactory = engineFactory;
    }

    [HttpPost("move")]
    public async Task<ActionResult<string>> GetBestMove(
        [FromQuery] string engine = "stockfish",
        [FromBody] MoveRequest request = null)
    {
        try
        {
            using var uciEngine = await _engineFactory.GetEngineAsync(engine);

            uciEngine.Start();
            await uciEngine.EnsureInitializedAsync();
            await uciEngine.WaitIsReady();

            await uciEngine.SetPositionAsync(request?.Fen ?? "", request?.Moves ?? "");
            var bestMove = await uciEngine.GetBestMoveAsync(request?.Depth ?? 15);

            return Ok(bestMove);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Engine '{engine}' is not available");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}

public class MoveRequest
{
    public string? Fen { get; set; }
    public string? Moves { get; set; }
    public int Depth { get; set; } = 20;
}
```

### Complete Example

```csharp
using EngineUCI.Core.Engine;

class Program
{
    static async Task Main(string[] args)
    {
        // Path to your UCI-compatible chess engine
        string enginePath = @"C:\engines\stockfish\stockfish.exe";

        using var engine = new UciEngine(enginePath);

        try
        {
            // Start and initialize the engine
            engine.Start();
            await engine.EnsureInitializedAsync();
            await engine.WaitIsReady();

            Console.WriteLine("Engine initialized successfully");

            // Analyze a specific position
            string fen = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
            await engine.SetPositionAsync(fen, "");

            // Get evaluation and best move
            var evaluationTask = engine.EvaluateAsync(depth: 20);
            var bestMoveTask = engine.GetBestMoveAsync(depth: 20);

            await Task.WhenAll(evaluationTask, bestMoveTask);

            string evaluation = evaluationTask.Result;
            string bestMove = bestMoveTask.Result;

            Console.WriteLine($"Position: {fen}");
            Console.WriteLine($"Best move: {bestMove}");
            Console.WriteLine($"Evaluation: {evaluation} centipawns");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

## API Reference

### IUciEngine Interface

The main interface for UCI engine communication.

#### Properties

- `bool IsInitialized` - Gets whether the engine has been successfully initialized

#### Methods

- `void Start()` - Starts the engine process and begins monitoring responses
- `Task<string> GetBestMoveAsync(int depth, CancellationToken)` - Gets best move with specified depth
- `Task<string> GetBestMoveAsync(TimeSpan timeSpan, CancellationToken)` - Gets best move with time limit
- `Task<string> EvaluateAsync(int depth, CancellationToken)` - Evaluates position with specified depth
- `Task<string> EvaluateAsync(TimeSpan timeSpan, CancellationToken)` - Evaluates position with time limit
- `Task SetNewGameAsync(CancellationToken)` - Signals engine to start a new game
- `Task SetPositionAsync(string fen, string moves, CancellationToken)` - Sets position from FEN with moves
- `Task SetPositionAsync(string moves, CancellationToken)` - Sets position from startpos with moves
- `Task<bool> WaitIsInitialized(CancellationToken)` - Waits for engine initialization
- `Task<bool> WaitIsReady(CancellationToken)` - Waits for engine ready signal
- `Task EnsureInitializedAsync(CancellationToken)` - Ensures initialization or throws exception

### UciEngine Class

Main implementation of the UCI engine wrapper.

#### Constructor

```csharp
public UciEngine(string executablePath)
```

Creates a new UCI engine instance with the specified executable path.

#### Thread Safety

The `UciEngine` class is thread-safe. All public methods can be called concurrently from multiple threads. Internal locking mechanisms ensure proper synchronization of engine communication.

### IUciEngineFactory Interface

Factory interface for creating and managing UCI engine instances with pooling support.

#### Methods

- `IUciEngine GetEngine(string name)` - Synchronously retrieves a registered engine by name
- `Task<IUciEngine> GetEngineAsync(string name)` - Asynchronously retrieves a registered engine by name

### UciEngineFactorySettings Class

Configuration class for the engine factory.

#### Properties

- `int MaxPoolSize` - Maximum number of concurrent engines (default: 16)

#### Methods

- `void RegisterNamedEngine(string name, Func<IUciEngine> factoryFunc)` - Registers a named engine factory
- `ReadOnlyDictionary<string, Func<IUciEngine>> GetRegistrations()` - Gets all registered engine factories

### ServiceCollectionExtensions Class

Extension methods for Microsoft.Extensions.DependencyInjection integration.

#### Methods

- `void UseUciEngineFactory(this IServiceCollection, Action<UciEngineFactorySettings>)` - Configures and registers the engine factory as a singleton

## UCI Protocol Overview

The Universal Chess Interface (UCI) is a open communication protocol that enables chess GUIs to communicate with chess engines. Key concepts:

### Commands (GUI to Engine)

- `uci` - Initialize UCI mode
- `isready` - Synchronize engine
- `ucinewgame` - Start new game
- `position` - Set board position
- `go` - Start calculating
- `stop` - Stop calculating
- `quit` - Exit engine

### Responses (Engine to GUI)

- `uciok` - Confirms UCI mode
- `readyok` - Confirms ready state
- `bestmove` - Returns best move found
- `info` - Provides search information
- `id` - Engine identification

### Position Formats

- **FEN**: `rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1`
- **Startpos**: Standard starting position
- **Moves**: Algebraic notation like `e2e4 e7e5 g1f3`

## Engine Compatibility

This library works with any UCI-compliant chess engine, including:

- **Stockfish** - Open source engine
- **Komodo** - Commercial engine
- **Leela Chess Zero** - Neural network engine
- **And many others** - Any UCI-compliant engine

## Requirements

- .NET 10.0 or higher
- UCI-compliant chess engine executable

## Error Handling

The library provides specific exceptions for common error scenarios:

```csharp
try
{
    await engine.EnsureInitializedAsync();
}
catch (UciEngineInitializationException ex)
{
    // Handle engine initialization failure
    Console.WriteLine($"Engine failed to initialize: {ex.Message}");
}
catch (OperationCanceledException)
{
    // Handle cancellation
    Console.WriteLine("Operation was cancelled");
}
```

## Best Practices

1. **Always dispose engines**: Use `using` statements or call `Dispose()` explicitly
2. **Handle cancellation**: Provide cancellation tokens for long-running operations
3. **Initialize properly**: Call `Start()`, then `EnsureInitializedAsync()`
4. **Check ready state**: Use `WaitIsReady()` before sending commands
5. **Error handling**: Wrap operations in try-catch blocks
6. **Thread safety**: The library is thread-safe, but avoid unnecessary concurrent operations

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or contributions:

- Create an issue in the GitHub repository
- Check existing documentation and examples
- Review UCI protocol specification for engine compatibility questions
