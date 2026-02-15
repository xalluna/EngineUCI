using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Lock = EngineUCI.Core.Locking.Lock;

namespace EngineUCI.Core.Engine;

/// <summary>
/// Implements a Universal Chess Interface (UCI) compliant chess engine wrapper.
/// Manages communication with an external chess engine process through standard input/output.
/// </summary>
public class UciEngine : IUciEngine
{
    /// <summary>
    /// Gets a value indicating whether the UCI engine has been successfully initialized.
    /// </summary>
    /// <value>
    /// <c>true</c> if the engine responded with "uciok" after initialization; otherwise, <c>false</c>.
    /// </value>
    public bool IsInitialized { get; private set; }

    public bool IsDisposed { get; private set; }

    public event EventHandler? OnDispose;

    /// <summary>
    /// The external chess engine process.
    /// </summary>
    private Process Process { get; init; }

    /// <summary>
    /// Synchronizes read operations from the engine process.
    /// </summary>
    private readonly Lock _processReadLock = new();

    /// <summary>
    /// Synchronizes write operations to the engine process.
    /// </summary>
    private readonly Lock _processWriteLock = new();

    /// <summary>
    /// Synchronizes "isready" command operations.
    /// </summary>
    private readonly Lock _readyLock = new();

    /// <summary>
    /// Synchronizes "bestmove" read/write operations.
    /// </summary>
    private readonly Lock _bestMoveLock = new();

    /// <summary>
    /// Synchronizes evaluation operations to prevent concurrent evaluations.
    /// </summary>
    private readonly Lock _evaluationLock = new();

    /// <summary>
    /// Task completion source for engine initialization.
    /// </summary>
    private TaskCompletionSource<bool> IsInitializedTcs { get; set; } = new();

    /// <summary>
    /// Task completion source for engine ready state.
    /// </summary>
    private TaskCompletionSource<bool> IsReadyTcs { get; set; } = new();

    /// <summary>
    /// Task completion source for best move calculations.
    /// </summary>
    private TaskCompletionSource<string> BestMoveTcs { get; set; } = new();

    /// <summary>
    /// Task completion source for position evaluations.
    /// </summary>
    private TaskCompletionSource<string> EvaluationTcs { get; set; } = new();

    /// <summary>
    /// Manages the state of ongoing position evaluations.
    /// </summary>
    private readonly UciEvaluationState EvaluationState = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="UciEngine"/> class.
    /// </summary>
    /// <param name="executablePath">The file path to the UCI chess engine executable.</param>
    /// <exception cref="ArgumentException">Thrown when executablePath is null or empty.</exception>
    public UciEngine(string executablePath)
    {
        Process = new Process();

        Process.StartInfo.FileName = executablePath;
        Process.StartInfo.UseShellExecute = false;
        Process.StartInfo.RedirectStandardInput = true;
        Process.StartInfo.RedirectStandardOutput = true;
        Process.StartInfo.RedirectStandardError = true;
        Process.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Starts the UCI engine process and begins monitoring for responses.
    /// Sets up event handlers for processing engine output and starts asynchronous output reading.
    /// </summary>
    /// <remarks>
    /// This method must be called before any UCI commands can be sent to the engine.
    /// The method starts the external process and configures handlers for various UCI responses.
    /// </remarks>
    public void Start()
    {
        Process.OutputDataReceived += new DataReceivedEventHandler(async (sender, e) =>
        {
            using var _readyLock = await _processReadLock.AcquireAsync();

            if (string.IsNullOrEmpty(e.Data)) return;

            switch (e.Data)
            {
                case UciTokens.Responses.UciOk: HandleInitialization(); break;
                case UciTokens.Responses.ReadyOk: await HandleIsReadyAsync(); break;
            }

            if (e.Data.Contains(UciTokens.Responses.Info)) await HandleInfoReceivedAsync(e.Data);
            if (e.Data.Contains(UciTokens.Responses.BestMove)) await HandleBestMoveAsync(e.Data);
            if (e.Data.Contains(UciTokens.Responses.BestMove) && EvaluationState.Active) await HandleSearchEndAsync();
        });

        Process.Start();
        Process.BeginOutputReadLine();
    }

    /// <summary>
    /// Asynchronously sends a command to the UCI engine.
    /// </summary>
    /// <param name="command">The UCI command to send to the engine.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    /// <remarks>
    /// This method is thread-safe and ensures exclusive access to the engine's standard input.
    /// </remarks>
    protected async Task SendAsync(string command, CancellationToken cancellationToken = default)
    {
        using var writeLock = await _processWriteLock.AcquireAsync(cancellationToken);

        await Process.StandardInput.WriteLineAsync(command);
        await Process.StandardInput.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously requests the best move from the engine using a specified search depth.
    /// </summary>
    /// <param name="depth">The maximum search depth in plies (half-moves). Default is 20.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the best move
    /// in algebraic notation (e.g., "e2e4", "g1f3").
    /// </returns>
    /// <remarks>
    /// This method sends a "go depth [depth]" command to the engine and waits for the "bestmove" response.
    /// The operation can be cancelled using the provided cancellation token.
    /// </remarks>
    public async Task<string> GetBestMoveAsync(int depth = 20, CancellationToken cancellationToken = default)
    {
        using (await _bestMoveLock.AcquireAsync(cancellationToken))
        {
            BestMoveTcs = new();

            var command = $"{UciTokens.Commands.Go} {UciTokens.Go.Depth} {depth}";
            await SendAsync(command, cancellationToken);
        }

        using var tokenRegistration = cancellationToken.Register(() => BestMoveTcs.TrySetCanceled(cancellationToken));

        return await BestMoveTcs.Task;
    }

    /// <summary>
    /// Asynchronously requests the best move from the engine using a specified time limit.
    /// </summary>
    /// <param name="timeSpan">The maximum time allowed for the engine to calculate the best move.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the best move
    /// in algebraic notation (e.g., "e2e4", "g1f3").
    /// </returns>
    /// <remarks>
    /// This method sends a "go movetime [milliseconds]" command to the engine and waits for the "bestmove" response.
    /// The time limit is converted to milliseconds for the UCI protocol.
    /// </remarks>
    public async Task<string> GetBestMoveAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        using (await _bestMoveLock.AcquireAsync(cancellationToken))
        {
            BestMoveTcs = new();

            var command = $"{UciTokens.Commands.Go} {UciTokens.Go.MoveTime} {timeSpan.Milliseconds}";
            await SendAsync(command, cancellationToken);
        }

        using var tokenRegistration = cancellationToken.Register(() => BestMoveTcs.TrySetCanceled(cancellationToken));

        return await BestMoveTcs.Task;
    }

    /// <summary>
    /// Asynchronously evaluates the current position using a specified search depth.
    /// </summary>
    /// <param name="depth">The maximum search depth in plies (half-moves). Default is 20.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the position
    /// evaluation in centipawns (e.g., "150", "-75").
    /// </returns>
    /// <remarks>
    /// This method collects evaluation information during the search process and returns
    /// the final evaluation at the specified depth. Only one evaluation can run at a time.
    /// </remarks>
    public async Task<string> EvaluateAsync(int depth = 20, CancellationToken cancellationToken = default)
    {
        using (await _evaluationLock.AcquireAsync(cancellationToken))
        {
            EvaluationTcs = new();

            EvaluationState.Reset();
            EvaluationState.Active = true;

            var command = $"{UciTokens.Commands.Go} {UciTokens.Go.Depth} {depth}";
            await SendAsync(command, cancellationToken);
        }

        using var tokenRegistration = cancellationToken.Register(() => EvaluationTcs.TrySetCanceled(cancellationToken));

        return await EvaluationTcs.Task;
    }

    /// <summary>
    /// Asynchronously evaluates the current position using a specified time limit.
    /// </summary>
    /// <param name="timeSpan">The maximum time allowed for the engine to evaluate the position.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the position
    /// evaluation in centipawns (e.g., "150", "-75").
    /// </returns>
    /// <remarks>
    /// This method collects evaluation information during the search process and returns
    /// the final evaluation. The time limit is converted to milliseconds for the UCI protocol.
    /// </remarks>
    public async Task<string> EvaluateAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        using (await _evaluationLock.AcquireAsync(cancellationToken))
        {
            EvaluationTcs = new();

            EvaluationState.Reset();
            EvaluationState.Active = true;

            var command = $"{UciTokens.Commands.Go} {UciTokens.Go.MoveTime} {timeSpan.Milliseconds}";
            await SendAsync(command, cancellationToken);
        }

        using var tokenRegistration = cancellationToken.Register(() => EvaluationTcs.TrySetCanceled(cancellationToken));

        return await EvaluationTcs.Task;
    }

    /// <summary>
    /// Asynchronously signals the engine to start a new game, resetting internal state.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method sends the "ucinewgame" command to the engine, which should reset
    /// its internal state including hash tables and other position-dependent data.
    /// </remarks>
    public Task SetNewGameAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync(UciTokens.Commands.UciNewGame, cancellationToken);
    }

    /// <summary>
    /// Asynchronously sets the board position using a FEN string and optional move list.
    /// </summary>
    /// <param name="fen">The FEN (Forsyth-Edwards Notation) string representing the board position.</param>
    /// <param name="moves">A space-separated list of moves in algebraic notation to apply after the FEN position.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when the FEN string is null or empty.</exception>
    /// <remarks>
    /// This method sends a "position fen [fen] moves [moves]" command to the engine.
    /// If no moves are provided, only the FEN position is set.
    /// </remarks>
    public async Task SetPositionAsync(string fen, string moves, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fen))
            throw new ArgumentException("FEN string must have a value");

        var commandBuilder = new StringBuilder(UciTokens.Commands.Position)
            .Append(' ')
            .Append(UciTokens.Position.Fen)
            .Append(' ')
            .Append(fen);

        if (!string.IsNullOrEmpty(moves))
        {
            commandBuilder
                .Append(' ')
                .Append(UciTokens.Position.Moves)
                .Append(' ')
                .Append(moves);
        }

        await SendAsync(commandBuilder.ToString(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously sets the board position to the starting position with optional moves.
    /// </summary>
    /// <param name="moves">A space-separated list of moves in algebraic notation to apply from the starting position.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method sends a "position startpos moves [moves]" command to the engine.
    /// If no moves are provided, the position is set to the standard chess starting position.
    /// </remarks>
    public async Task SetPositionAsync(string moves = "", CancellationToken cancellationToken = default)
    {
        var commandBuilder = new StringBuilder(UciTokens.Commands.Position)
        .Append(' ')
        .Append(UciTokens.Position.StartPos);

        if (!string.IsNullOrEmpty(moves))
        {
            commandBuilder
                .Append(' ')
                .Append(UciTokens.Position.Moves)
                .Append(' ')
                .Append(moves);
        }

        await SendAsync(commandBuilder.ToString(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously waits for the engine to complete initialization and respond with "uciok".
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is <c>true</c> if
    /// initialization was successful; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method sends the "uci" command to the engine and waits for the "uciok" response.
    /// If the operation is cancelled, returns <c>false</c> instead of throwing an exception.
    /// </remarks>
    public Task<bool> WaitIsInitialized(CancellationToken cancellationToken = default) => SafeCancellationScopeAsync(async () =>
    {
        await SendAsync(UciTokens.Commands.Uci, cancellationToken);

        using var tokenRegistration = cancellationToken.Register(() => IsInitializedTcs.TrySetCanceled(cancellationToken));
        IsInitialized = await IsInitializedTcs.Task;

        return IsInitialized;
    },
    defaultValue: false);

    /// <summary>
    /// Asynchronously waits for the engine to signal that it is ready to receive commands.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is <c>true</c> if
    /// the engine is ready; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method sends the "isready" command to the engine and waits for the "readyok" response.
    /// If the operation is cancelled, returns <c>false</c> instead of throwing an exception.
    /// </remarks>
    public Task<bool> WaitIsReady(CancellationToken cancellationToken = default) => SafeCancellationScopeAsync(async () =>
    {
        using (await _readyLock.AcquireAsync(cancellationToken))
        {
            IsReadyTcs = new();
            await SendAsync(UciTokens.Commands.IsReady, cancellationToken);
        }

        if (cancellationToken != default)
        {
            using var tokenRegistration = cancellationToken.Register(() => IsReadyTcs.TrySetCanceled(cancellationToken));
        }

        return await IsReadyTcs.Task;
    },
    defaultValue: false);

    /// <summary>
    /// Asynchronously ensures that the engine is initialized, throwing an exception if initialization fails.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="UciEngineInitializationException">Thrown when the engine fails to initialize.</exception>
    /// <remarks>
    /// This is a convenience method that calls <see cref="WaitIsInitialized"/> and throws an exception
    /// if the initialization is not successful.
    /// </remarks>
    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        var isInitializationSuccessful = await WaitIsInitialized(cancellationToken);

        if (!isInitializationSuccessful) throw new UciEngineInitializationException();
    }

    /// <summary>
    /// Handles the "uciok" response from the engine, marking initialization as complete.
    /// </summary>
    private void HandleInitialization()
    {
        IsInitializedTcs.TrySetResult(true);
    }

    /// <summary>
    /// Handles the "readyok" response from the engine, signaling that the engine is ready.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleIsReadyAsync()
    {
        using var readyLock = await _readyLock.AcquireAsync();
        IsReadyTcs.TrySetResult(true);
    }

    /// <summary>
    /// Handles the "bestmove" response from the engine, extracting and returning the best move.
    /// </summary>
    /// <param name="data">The raw response data from the engine containing the bestmove.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleBestMoveAsync(string data)
    {
        using var bestMoveLock = await _bestMoveLock.AcquireAsync();

        BestMoveTcs.TrySetResult(BestMove.Parse(data));
    }

    /// <summary>
    /// Handles the completion of a search operation during evaluation, returning the final evaluation.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleSearchEndAsync()
    {
        using var evaluationLock = await _evaluationLock.AcquireAsync();
        EvaluationTcs.TrySetResult(EvaluationState.Values[EvaluationState.MaxDepth]);
        EvaluationState.Active = false;
    }

    /// <summary>
    /// Handles "info" responses from the engine during evaluation, collecting depth and score information.
    /// </summary>
    /// <param name="data">The raw info response data from the engine.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleInfoReceivedAsync(string data)
    {
        using var evaluationLock = await _evaluationLock.AcquireAsync();

        if (!EvaluationState.Active) return;

        var result = Evaluation.Parse(data);

        if (result is null) return;
        if (!EvaluationState.Values.TryAdd(result.Depth, result.Evaluation)) return;

        if (EvaluationState.MaxDepth < result.Depth) EvaluationState.MaxDepth = result.Depth;
    }

    /// <summary>
    /// Executes an asynchronous operation with safe cancellation handling.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <param name="defaultValue">The default value to return if the operation is cancelled.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. Returns the result of the function
    /// or the default value if cancelled.
    /// </returns>
    private async Task<T> SafeCancellationScopeAsync<T>(Func<Task<T>> func, T defaultValue)
    {
        try { return await func(); }
        catch (OperationCanceledException) { return defaultValue; }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="UciEngine"/>.
    /// </summary>
    /// <remarks>
    /// This method terminates the engine process and releases associated resources.
    /// After disposal, the engine instance should not be used.
    /// </remarks>
    public void Dispose()
    {
        Process.Dispose();
        IsDisposed = true;
        OnDispose?.Invoke(this, EventArgs.Empty);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Manages the state of ongoing position evaluations, tracking depth and score information.
/// </summary>
internal class UciEvaluationState
{
    /// <summary>
    /// Gets or sets the maximum search depth reached during the current evaluation.
    /// </summary>
    public int MaxDepth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an evaluation is currently active.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// A thread-safe dictionary that maps search depths to their corresponding evaluations.
    /// </summary>
    public ConcurrentDictionary<int, string> Values = new();

    /// <summary>
    /// Resets the evaluation state to its initial values.
    /// </summary>
    /// <remarks>
    /// This method clears all stored values and resets depth and active status.
    /// </remarks>
    public void Reset()
    {
        MaxDepth = 0;
        Active = false;
        Values.Clear();
    }
};

/// <summary>
/// The exception that is thrown when a UCI engine fails to initialize properly.
/// </summary>
/// <param name="message">The message that describes the error. Default is "Engine failed to initialize".</param>
public class UciEngineInitializationException(string message = "Engine failed to initialize") : Exception(message);