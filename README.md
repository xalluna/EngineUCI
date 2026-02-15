# EngineUCI.Core

A .NET library for communicating with Universal Chess Interface (UCI) compliant chess engines. This library provides a clean, async/await-friendly API for engine communication, position setup, move calculation, and position evaluation.

## Features

- **UCI Protocol Compliance**: Full implementation of the Universal Chess Interface protocol
- **Async/Await Support**: All operations are asynchronous with proper cancellation support
- **Thread-Safe**: Built-in locking mechanisms ensure safe concurrent operations
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
