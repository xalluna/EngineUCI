namespace EngineUCI.Core.Engine;

public interface IUciEngine : IDisposable
{
    public bool IsInitialized { get; }

    void Start();

    Task<string> GetBestMoveAsync(int depth = 20, CancellationToken cancellationToken = default);
    Task<string> GetBestMoveAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default);
    Task<string> EvaluateAsync(int depth = 20, CancellationToken cancellationToken = default);
    Task<string> EvaluateAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default);

    Task SetNewGameAsync(CancellationToken cancellationToken = default);
    Task SetPositionAsync(string fen, string moves, CancellationToken cancellationToken = default);
    Task SetPositionAsync(string moves = "", CancellationToken cancellationToken = default);

    Task<bool> WaitIsInitialized(CancellationToken cancellationToken = default);
    Task<bool> WaitIsReady(CancellationToken cancellationToken = default);
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
}