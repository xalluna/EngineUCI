namespace EngineUCI.Core.Locking;

internal class Lock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public bool IsLocked => _semaphore.CurrentCount == 0;

    public IDisposable Acquire()
    {
        _semaphore.Wait();
        return new LockReleaser(_semaphore);
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new LockReleaser(_semaphore);
    }

    private class LockReleaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}