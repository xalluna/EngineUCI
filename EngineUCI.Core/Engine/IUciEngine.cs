using EngineUCI.Core.Engine.Evaluations;

namespace EngineUCI.Core.Engine;

/// <summary>
/// Defines the interface for a Universal Chess Interface (UCI) compliant chess engine.
/// Provides methods for engine communication, position setup, move calculation, and evaluation.
/// </summary>
public interface IUciEngine : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the UCI engine has been successfully initialized.
    /// </summary>
    /// <value>
    /// <c>true</c> if the engine is initialized and ready to accept commands; otherwise, <c>false</c>.
    /// </value>
    public bool IsInitialized { get; }

    public bool IsDisposed { get; }

    public event EventHandler? OnDispose;

    /// <summary>
    /// Starts the UCI engine process and begins monitoring for responses.
    /// This method must be called before any other engine operations.
    /// </summary>
    void Start();

    /// <summary>
    /// Asynchronously requests the best move from the engine using a specified search depth.
    /// </summary>
    /// <param name="depth">The maximum search depth in plies (half-moves). Default is 20.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the best move
    /// in algebraic notation (e.g., "e2e4", "g1f3").
    /// </returns>
    Task<string> GetBestMoveAsync(int depth = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously requests the best move from the engine using a specified time limit.
    /// </summary>
    /// <param name="timeSpan">The maximum time allowed for the engine to calculate the best move.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the best move
    /// in algebraic notation (e.g., "e2e4", "g1f3").
    /// </returns>
    Task<string> GetBestMoveAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously evaluates the current position using a specified search depth.
    /// </summary>
    /// <param name="depth">The maximum search depth in plies (half-moves). Default is 20.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the position
    /// evaluation in centipawns (e.g., "150", "-75").
    /// </returns>
    Task<EvaluationCollection> EvaluateAsync(int depth = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously evaluates the current position using a specified time limit.
    /// </summary>
    /// <param name="timeSpan">The maximum time allowed for the engine to evaluate the position.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the position
    /// evaluation in centipawns (e.g., "150", "-75").
    /// </returns>
    Task<EvaluationCollection> EvaluateAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously signals the engine to start a new game, resetting internal state.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetNewGameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets the board position using a FEN string and optional move list.
    /// </summary>
    /// <param name="fen">The FEN (Forsyth-Edwards Notation) string representing the board position.</param>
    /// <param name="moves">A space-separated list of moves in algebraic notation to apply after the FEN position.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when the FEN string is null or empty.</exception>
    Task SetPositionAsync(string fen, string moves, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets the board position to the starting position with optional moves.
    /// </summary>
    /// <param name="moves">A space-separated list of moves in algebraic notation to apply from the starting position.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetPositionAsync(string moves = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets the number of principal variations (Multi-PV) the engine should calculate.
    /// </summary>
    /// <param name="multiPvMode">The number of principal variations to calculate. Default is 1 (single best line).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetMultiPvAsync(int multiPvMode = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously waits for the engine to complete initialization and respond with "uciok".
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is <c>true</c> if
    /// initialization was successful; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> WaitIsInitialized(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously waits for the engine to signal that it is ready to receive commands.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is <c>true</c> if
    /// the engine is ready; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> WaitIsReady(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously ensures that the engine is initialized, throwing an exception if initialization fails.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="UciEngineInitializationException">Thrown when the engine fails to initialize.</exception>
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
}