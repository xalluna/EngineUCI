# EngineUCI.Core

A .NET library for communicating with Universal Chess Interface (UCI) compliant chess engines. Provides a clean, async/await-friendly API for engine communication, position setup, move calculation, position evaluation, PGN parsing, and PGN-to-LAN conversion.

## Features

- **UCI Protocol Compliance**: Full implementation of the Universal Chess Interface protocol
- **Async/Await Support**: All operations are asynchronous with proper cancellation support
- **Thread-Safe**: Built-in locking mechanisms ensure safe concurrent operations
- **Dependency Injection**: Built-in factory pattern with Microsoft.Extensions.DependencyInjection support
- **Engine Pooling**: Semaphore-based pooling with configurable limits to manage multiple engine instances
- **Multi-PV Support**: Request and consume multiple principal variations per search
- **PGN Parsing**: Parse single or multiple PGN games from text or files
- **PGN to LAN Conversion**: Convert PGN algebraic notation to Long Algebraic Notation (LAN) for engine input

## Installation

```bash
dotnet add package EngineUCI.Core
```

## Quick Start

```csharp
using EngineUCI.Core.Engine;
using EngineUCI.Core.Engine.Evaluations;

var engine = new UciEngine(@"C:\engines\stockfish.exe");
engine.Start();

await engine.EnsureInitializedAsync();
await engine.WaitIsReady();

await engine.SetPositionAsync(); // Standard starting position

string bestMove = await engine.GetBestMoveAsync();
Console.WriteLine($"Best move: {bestMove}"); // e.g., "e2e4"

engine.Dispose();
```

## Engine Usage

### Setting Positions

```csharp
// Standard starting position
await engine.SetPositionAsync();

// Starting position with moves
await engine.SetPositionAsync("e2e4 e7e5 g1f3");

// FEN string
await engine.SetPositionAsync("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", "");

// FEN string with additional moves
await engine.SetPositionAsync("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", "e2e4 e7e5");
```

### Getting the Best Move

```csharp
// Default depth (20)
string move = await engine.GetBestMoveAsync();

// Custom depth
string move = await engine.GetBestMoveAsync(depth: 15);

// Time limit
string move = await engine.GetBestMoveAsync(TimeSpan.FromSeconds(5));

// With cancellation
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
string move = await engine.GetBestMoveAsync(20, cts.Token);
```

### Evaluating a Position

`EvaluateAsync` returns an `EvaluationCollection` — an ordered, enumerable collection of `Evaluation` records, one per principal variation.

```csharp
EvaluationCollection result = await engine.EvaluateAsync(depth: 20);

// Access the best line
Evaluation best = result.BestEvaluation;
Console.WriteLine($"Score: {best.Score} cp at depth {best.Depth}");

// Iterate all lines
foreach (Evaluation eval in result)
    Console.WriteLine($"Rank {eval.Rank}: {eval.Score}");
```

Each `Evaluation` record has three fields:

| Field   | Type     | Description |
|---------|----------|-------------|
| `Depth` | `int`    | Search depth in plies |
| `Rank`  | `int`    | Principal variation rank (1 = best) |
| `Score` | `string` | Score in centipawns or mate notation (e.g., `"34"`, `"mate 3"`) |

### Multi-PV (Multiple Lines)

Call `SetMultiPvAsync` before searching to request multiple principal variations. The returned `EvaluationCollection` will contain one `Evaluation` per PV, ordered by rank.

```csharp
await engine.SetMultiPvAsync(3); // Request top 3 lines

await engine.SetPositionAsync("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", "");

EvaluationCollection result = await engine.EvaluateAsync(depth: 20);

foreach (Evaluation eval in result)
    Console.WriteLine($"Line {eval.Rank}: {eval.Score} cp");

// Reset to single PV
await engine.SetMultiPvAsync(1);
```

## PGN Parsing

`PgnParser` converts PGN text into structured `PgnGame` objects. It handles the Seven Tag Roster headers, move text, comments, variations, and NAGs.

```csharp
using EngineUCI.Core.Parsing;

var parser = new PgnParser();

// Parse a single game
PgnGame game = parser.ParseGame("""
    [Event "World Championship"]
    [White "Kasparov"]
    [Black "Deep Blue"]
    [Result "1-0"]

    1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 1-0
    """);

Console.WriteLine(game.Headers["White"]); // Kasparov
Console.WriteLine(game.Moves[0]);         // e4
Console.WriteLine(game.Result);           // 1-0

// Parse multiple games from a file or string
List<PgnGame> games = parser.ParseMultipleGames(File.ReadAllText("games.pgn"));
Console.WriteLine($"Loaded {games.Count} games");
```

`PgnGame` properties:

| Property  | Type                        | Description |
|-----------|-----------------------------|-------------|
| `Headers` | `Dictionary<string, string>`| PGN tag pairs |
| `Moves`   | `List<string>`              | Clean move list (no annotations or symbols) |
| `Result`  | `string`                    | `"1-0"`, `"0-1"`, `"1/2-1/2"`, or `"*"` |

## PGN to LAN Conversion

`PgnToLanConverter` converts PGN algebraic notation (e.g., `"Nf3"`) into Long Algebraic Notation (e.g., `"g1f3"`) suitable for UCI engine input. It maintains an internal board state across successive calls.

```csharp
using EngineUCI.Core.Conversions;

var converter = new PgnToLanConverter();

string lan1 = converter.ConvertMove("e4");    // "e2e4"
string lan2 = converter.ConvertMove("e5");    // "e7e5"
string lan3 = converter.ConvertMove("Nf3");   // "g1f3"
string lan4 = converter.ConvertMove("O-O");   // "e1g1"
string lan5 = converter.ConvertMove("e8=Q");  // "e7e8=Q"

// Reset board for a new game
converter.Reset();
```

Supported move types:

| PGN Input   | LAN Output  | Description |
|-------------|-------------|-------------|
| `e4`        | `e2e4`      | Pawn push |
| `exd5`      | `e4d5`      | Pawn capture |
| `Nf3`       | `g1f3`      | Piece move |
| `Ngf3`      | `g1f3`      | Disambiguated piece move |
| `O-O`       | `e1g1`      | Kingside castling |
| `O-O-O`     | `e1c1`      | Queenside castling |
| `e8=Q`      | `e7e8=Q`    | Promotion |

`ConvertMove` throws `ArgumentException` for null/empty input and `InvalidMoveException` if no legal piece can reach the destination square.

### PGN to Engine Pipeline

A common pattern is to parse a PGN game, convert its moves to LAN, and feed them to the engine:

```csharp
using EngineUCI.Core.Engine;
using EngineUCI.Core.Parsing;
using EngineUCI.Core.Conversions;

var parser    = new PgnParser();
var converter = new PgnToLanConverter();
var engine    = new UciEngine(@"C:\engines\stockfish.exe");

// Parse PGN
PgnGame game = parser.ParseGame(pgnText);

// Convert moves to LAN
string lanMoves = string.Join(" ", game.Moves.Select(m => converter.ConvertMove(m)));

// Feed to engine
engine.Start();
await engine.EnsureInitializedAsync();
await engine.WaitIsReady();
await engine.SetNewGameAsync();
await engine.SetPositionAsync(lanMoves);

EvaluationCollection result = await engine.EvaluateAsync(depth: 20);
Console.WriteLine($"Final position: {result.BestEvaluation.Score} cp");
```

## Dependency Injection & Engine Pooling

### ASP.NET Core Setup

```csharp
using EngineUCI.Core.DependencyInjection;

builder.Services.UseUciEngineFactory(settings =>
{
    settings.MaxPoolSize = 10;
    settings.RegisterNamedEngine("stockfish", () => new UciEngine(@"C:\engines\stockfish.exe"));
    settings.RegisterNamedEngine("komodo",    () => new UciEngine(@"C:\engines\komodo.exe"));
});
```

### Console Application with DI

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

var factory = host.Services.GetRequiredService<IUciEngineFactory>();
```

### Using the Factory

```csharp
// Async (preferred — waits for a pool slot if needed)
using var engine = await factory.GetEngineAsync("stockfish");

// Sync
using var engine = factory.GetEngine("stockfish");
```

The engine factory uses `SemaphoreSlim` internally. When `MaxPoolSize` engines are in use, `GetEngineAsync` awaits until a slot is released. Disposing the engine automatically returns its pool slot.

### In a Controller

```csharp
[ApiController]
[Route("api/chess")]
public class ChessController : ControllerBase
{
    private readonly IUciEngineFactory _engineFactory;

    public ChessController(IUciEngineFactory engineFactory) =>
        _engineFactory = engineFactory;

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalysisRequest request)
    {
        try
        {
            using var engine = await _engineFactory.GetEngineAsync("stockfish");

            engine.Start();
            await engine.EnsureInitializedAsync();
            await engine.WaitIsReady();
            await engine.SetPositionAsync(request.Fen, request.Moves);

            var result = await engine.EvaluateAsync(request.Depth);
            return Ok(new { BestMove = await engine.GetBestMoveAsync(request.Depth), Score = result.BestEvaluation.Score });
        }
        catch (KeyNotFoundException)  { return BadRequest("Engine not registered."); }
        catch (ObjectDisposedException) { return StatusCode(503, "Service is shutting down."); }
    }
}
```

### Error Handling

```csharp
try
{
    using var engine = await factory.GetEngineAsync("nonexistent");
}
catch (KeyNotFoundException)
{
    // Engine name was not registered
}
catch (ObjectDisposedException)
{
    // Factory was disposed (e.g., during application shutdown)
}
```

## API Reference

### `IUciEngine`

```
Namespace: EngineUCI.Core.Engine
```

| Member | Description |
|--------|-------------|
| `bool IsInitialized` | Whether the engine has been successfully initialized |
| `bool IsDisposed` | Whether the engine has been disposed |
| `event EventHandler? OnDispose` | Raised just before the engine is disposed |
| `void Start()` | Starts the engine process |
| `Task EnsureInitializedAsync(CancellationToken)` | Waits for initialization or throws `UciEngineInitializationException` |
| `Task<bool> WaitIsInitialized(CancellationToken)` | Waits for initialization; returns `false` on timeout |
| `Task<bool> WaitIsReady(CancellationToken)` | Waits for `readyok`; returns `false` on timeout |
| `Task SetNewGameAsync(CancellationToken)` | Sends `ucinewgame` |
| `Task SetPositionAsync(string moves, CancellationToken)` | Sets position from startpos with moves |
| `Task SetPositionAsync(string fen, string moves, CancellationToken)` | Sets position from FEN with moves |
| `Task SetMultiPvAsync(int multiPvMode, CancellationToken)` | Sets the MultiPV option (default: 1) |
| `Task<string> GetBestMoveAsync(int depth, CancellationToken)` | Returns best move at given depth |
| `Task<string> GetBestMoveAsync(TimeSpan, CancellationToken)` | Returns best move within time limit |
| `Task<EvaluationCollection> EvaluateAsync(int depth, CancellationToken)` | Evaluates position at given depth |
| `Task<EvaluationCollection> EvaluateAsync(TimeSpan, CancellationToken)` | Evaluates position within time limit |

### `EvaluationCollection`

```
Namespace: EngineUCI.Core.Engine.Evaluations
```

Ordered, enumerable collection of `Evaluation` results returned by `EvaluateAsync`. Implements `IEnumerable<Evaluation>`.

| Member | Description |
|--------|-------------|
| `Evaluation BestEvaluation` | The rank-1 (best) evaluation |
| `IEnumerator<Evaluation> GetEnumerator()` | Iterates evaluations ordered by rank ascending |

### `Evaluation`

```
Namespace: EngineUCI.Core.Engine.Evaluations
public record Evaluation(int Depth, int Rank, string Score)
```

### `PgnParser`

```
Namespace: EngineUCI.Core.Parsing
```

| Member | Description |
|--------|-------------|
| `PgnGame ParseGame(string pgnText)` | Parses a single PGN game |
| `List<PgnGame> ParseMultipleGames(string pgnText)` | Parses all games in a PGN string |

### `PgnToLanConverter`

```
Namespace: EngineUCI.Core.Conversions
```

| Member | Description |
|--------|-------------|
| `string ConvertMove(string pgnMove)` | Converts PGN move to LAN; updates internal board state |
| `void Reset()` | Resets board to starting position |

### `IUciEngineFactory`

```
Namespace: EngineUCI.Core.DependencyInjection
```

| Member | Description |
|--------|-------------|
| `IUciEngine GetEngine(string name)` | Synchronously retrieves a pooled engine by name |
| `Task<IUciEngine> GetEngineAsync(string name)` | Asynchronously retrieves a pooled engine by name |

### `UciEngineFactorySettings`

```
Namespace: EngineUCI.Core.DependencyInjection
```

| Member | Description |
|--------|-------------|
| `int MaxPoolSize` | Maximum concurrent engines (default: 16) |
| `void RegisterNamedEngine(string name, Func<IUciEngine> factory)` | Registers a named engine factory |
| `ReadOnlyDictionary<string, Func<IUciEngine>> GetRegistrations()` | Returns all registered factories |

## UCI Protocol Overview

The Universal Chess Interface (UCI) is an open communication protocol between chess GUIs and chess engines.

**Commands (GUI → Engine):** `uci`, `isready`, `ucinewgame`, `position`, `go`, `stop`, `setoption`, `quit`

**Responses (Engine → GUI):** `uciok`, `readyok`, `bestmove`, `info`, `id`

**Position formats:**
- FEN: `rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1`
- Moves: LAN notation like `e2e4 e7e5 g1f3`

## Engine Compatibility

Works with any UCI-compliant chess engine, including Stockfish, Komodo, Leela Chess Zero, and others.

## Requirements

- .NET 10.0 or higher
- A UCI-compliant chess engine executable

## Best Practices

1. **Always use `using` statements** — ensures proper disposal and pool slot release
2. **Call `Start()` then `EnsureInitializedAsync()`** before any engine commands
3. **Call `WaitIsReady()`** after position changes if precise synchronization is needed
4. **Call `Reset()` on `PgnToLanConverter`** when starting a new game
5. **Set Multi-PV before searching** — `SetMultiPvAsync` takes effect on the next `go` command
6. **Handle `KeyNotFoundException`** when using named engines from the factory
7. **Configure pool size appropriately** for your concurrency requirements

## License

This project is licensed under the MIT License — see the LICENSE file for details.

## Support

For issues, questions, or contributions, open an issue in the GitHub repository.
