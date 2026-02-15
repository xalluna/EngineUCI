using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Lock = EngineUCI.Core.Locking.Lock;

namespace EngineUCI.Core.Engine;

public partial class UciEngine : IUciEngine
{
    public bool IsInitialized { get; private set; }

    private Process _process { get; init; }

    private readonly Lock _processReadLock = new();
    private readonly Lock _processWriteLock = new();
    private readonly Lock _readyLock = new();
    private readonly Lock _evaluationLock = new();

    private TaskCompletionSource<bool> _isInitializedTcs { get; set; } = new();
    private TaskCompletionSource<bool> _isReadyTcs { get; set; } = new();
    private TaskCompletionSource<string> _bestMoveTcs { get; set; } = new();
    private TaskCompletionSource<string> _evaluationTcs { get; set; } = new();

    private readonly UciEvaluationState EvaluationState = new();

    public UciEngine(string executablePath)
    {
        _process = new Process();

        _process.StartInfo.FileName = executablePath;
        _process.StartInfo.UseShellExecute = false;
        _process.StartInfo.RedirectStandardInput = true;
        _process.StartInfo.RedirectStandardOutput = true;
        _process.StartInfo.RedirectStandardError = true;
        _process.EnableRaisingEvents = true;
    }

    public void Start()
    {
        _process.OutputDataReceived += new DataReceivedEventHandler(async (sender, e) =>
        {
            using var _readyLock = await _processReadLock.AcquireAsync();

            if (string.IsNullOrEmpty(e.Data)) return;

            Console.WriteLine($"[{_process.Id}] {e.Data}");

            switch (e.Data)
            {
                case UciTokens.Responses.UciOk: HandleInitialization(); break;
                case UciTokens.Responses.ReadyOk: await HandleIsReadyAsync(); break;
            }

            if (e.Data.Contains(UciTokens.Responses.Info)) await HandleInfoReceivedAsync(e.Data);
            if (e.Data.Contains(UciTokens.Responses.BestMove)) HandleBestMove(e.Data);
            if (e.Data.Contains(UciTokens.Responses.BestMove) && EvaluationState.Active) await HandleSearchEndAsync();
        });

        _process.Start();
        _process.BeginOutputReadLine();
    }

    protected async Task SendAsync(string command, CancellationToken cancellationToken = default)
    {
        using var writeLock = await _processWriteLock.AcquireAsync(cancellationToken);

        await _process.StandardInput.WriteLineAsync(command);
        await _process.StandardInput.FlushAsync(cancellationToken);
    }

    public async Task<string> GetBestMoveAsync(int depth = 20, CancellationToken cancellationToken = default)
    {
        var command = $"{UciTokens.Commands.Go} {UciTokens.Go.Depth} {depth}";
        await SendAsync(command, cancellationToken);

        using var tokenRegistration = cancellationToken.Register(() => _bestMoveTcs.TrySetCanceled(cancellationToken));

        return await _bestMoveTcs.Task;
    }

    public async Task<string> GetBestMoveAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        var command = $"{UciTokens.Commands.Go} {UciTokens.Go.MoveTime} {timeSpan.Milliseconds}";
        await SendAsync(command, cancellationToken);

        using var tokenRegistration = cancellationToken.Register(() => _bestMoveTcs.TrySetCanceled(cancellationToken));

        return await _bestMoveTcs.Task;
    }

    public async Task<string> EvaluateAsync(int depth = 20, CancellationToken cancellationToken = default)
    {
        using (await _evaluationLock.AcquireAsync(cancellationToken))
        {
            EvaluationState.Reset();
            EvaluationState.Active = true;

            var command = $"{UciTokens.Commands.Go} {UciTokens.Go.Depth} {depth}";
            await SendAsync(command, cancellationToken);
        }

        using var tokenRegistration = cancellationToken.Register(() => _evaluationTcs.TrySetCanceled(cancellationToken));

        return await _evaluationTcs.Task;
    }

    public async Task<string> EvaluateAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        using (await _evaluationLock.AcquireAsync(cancellationToken))
        {
            EvaluationState.Reset();
            EvaluationState.Active = true;

            var command = $"{UciTokens.Commands.Go} {UciTokens.Go.MoveTime} {timeSpan.Milliseconds}";
            await SendAsync(command, cancellationToken);
        }

        using var tokenRegistration = cancellationToken.Register(() => _evaluationTcs.TrySetCanceled(cancellationToken));

        return await _evaluationTcs.Task;
    }

    public Task SetNewGameAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync(UciTokens.Commands.UciNewGame, cancellationToken);
    }

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

    public Task<bool> WaitIsInitialized(CancellationToken cancellationToken = default) => SafeCancellationScopeAsync(async () =>
    {
        await SendAsync(UciTokens.Commands.Uci, cancellationToken);

        using var tokenRegistration = cancellationToken.Register(() => _isInitializedTcs.TrySetCanceled(cancellationToken));
        IsInitialized = await _isInitializedTcs.Task;

        return IsInitialized;
    },
    defaultValue: false);

    public Task<bool> WaitIsReady(CancellationToken cancellationToken = default) => SafeCancellationScopeAsync(async () =>
    {
        using (await _readyLock.AcquireAsync(cancellationToken))
        {
            await SendAsync(UciTokens.Commands.IsReady, cancellationToken);
            _isReadyTcs = new();
        }

        if (cancellationToken != default)
        {
            using var tokenRegistration = cancellationToken.Register(() => _isReadyTcs.TrySetCanceled(cancellationToken));
        }

        return await _isReadyTcs.Task;
    },
    defaultValue: false);

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        var isInitializationSuccessful = await WaitIsInitialized(cancellationToken);

        if (!isInitializationSuccessful) throw new UciEngineInitializationException();
    }

    private void HandleInitialization()
    {
        _isInitializedTcs.TrySetResult(true);
    }

    private async Task HandleIsReadyAsync()
    {
        using var readyLock = await _readyLock.AcquireAsync();
        _isReadyTcs.TrySetResult(true);
    }

    private void HandleBestMove(string data)
    {
        _bestMoveTcs.TrySetResult(BestMove.Parse(data));
    }

    private async Task HandleSearchEndAsync()
    {
        using var evaluationLock = await _evaluationLock.AcquireAsync();
        _evaluationTcs.TrySetResult(EvaluationState.Values[EvaluationState.MaxDepth]);
        EvaluationState.Active = false;
    }

    private async Task HandleInfoReceivedAsync(string data)
    {
        using var evaluationLock = await _evaluationLock.AcquireAsync();

        if (!EvaluationState.Active) return;

        var result = Evaluation.Parse(data);

        if (result is null) return;
        if (!EvaluationState.Values.TryAdd(result.Depth, result.Evaluation)) return;

        if (EvaluationState.MaxDepth < result.Depth) EvaluationState.MaxDepth = result.Depth;
    }

    private async Task<T> SafeCancellationScopeAsync<T>(Func<Task<T>> func, T defaultValue)
    {
        try { return await func(); }
        catch (OperationCanceledException) { return defaultValue; }
    }

    public void Dispose()
    {
        _process.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal class UciEvaluationState
{
    public int MaxDepth { get; set; }
    public bool Active { get; set; }
    public ConcurrentDictionary<int, string> Values = new();

    public void Reset()
    {
        MaxDepth = 0;
        Active = false;
        Values.Clear();
    }
};

public class UciEngineInitializationException(string message = "Engine failed to initialize") : Exception(message);