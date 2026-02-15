namespace EngineUCI.Core.Locking;

/// <summary>
/// Provides a lightweight, disposable locking mechanism using SemaphoreSlim.
/// Supports both synchronous and asynchronous locking operations with automatic release through IDisposable.
/// </summary>
/// <remarks>
/// This lock implementation uses a binary semaphore (initial count = 1, max count = 1) to provide
/// mutual exclusion. The lock is designed to be used with 'using' statements for automatic release.
/// Thread-safe and supports cancellation for async operations.
/// </remarks>
internal class Lock
{
    /// <summary>
    /// The underlying semaphore that provides the locking mechanism.
    /// Initialized with count=1 and maxCount=1 to act as a binary semaphore.
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Gets a value indicating whether the lock is currently held by another thread.
    /// </summary>
    /// <value>
    /// <c>true</c> if the lock is currently held (semaphore count is 0); otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This property provides a snapshot of the lock state at the time of access.
    /// The state may change immediately after reading due to concurrent operations.
    /// </remarks>
    public bool IsLocked => _semaphore.CurrentCount == 0;

    /// <summary>
    /// Synchronously acquires the lock, blocking the calling thread until the lock is available.
    /// </summary>
    /// <returns>
    /// An <see cref="IDisposable"/> that releases the lock when disposed.
    /// Use with a 'using' statement to ensure proper lock release.
    /// </returns>
    /// <remarks>
    /// This method will block the calling thread until the lock becomes available.
    /// The returned disposable must be properly disposed to release the lock.
    /// </remarks>
    /// <example>
    /// <code>
    /// using (lock.Acquire())
    /// {
    ///     // Critical section - lock is held
    /// }
    /// // Lock is automatically released here
    /// </code>
    /// </example>
    public IDisposable Acquire()
    {
        _semaphore.Wait();
        return new LockReleaser(_semaphore);
    }

    /// <summary>
    /// Asynchronously acquires the lock, allowing the operation to be cancelled.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the lock acquisition operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous lock acquisition operation.
    /// The task result is an <see cref="IDisposable"/> that releases the lock when disposed.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token.
    /// </exception>
    /// <remarks>
    /// This method allows the calling thread to yield while waiting for the lock,
    /// making it suitable for use in async/await scenarios. The lock can be cancelled
    /// if the cancellation token is signaled before acquisition completes.
    /// </remarks>
    /// <example>
    /// <code>
    /// using (await lock.AcquireAsync(cancellationToken))
    /// {
    ///     // Critical section - lock is held
    /// }
    /// // Lock is automatically released here
    /// </code>
    /// </example>
    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new LockReleaser(_semaphore);
    }

    /// <summary>
    /// Internal helper class that implements IDisposable to automatically release the semaphore.
    /// Used by both synchronous and asynchronous lock acquisition methods.
    /// </summary>
    /// <param name="semaphore">The semaphore to release when the lock releaser is disposed.</param>
    /// <remarks>
    /// This class ensures that the semaphore is properly released when the lock goes out of scope
    /// or is explicitly disposed, preventing deadlocks and resource leaks.
    /// </remarks>
    private class LockReleaser(SemaphoreSlim semaphore) : IDisposable
    {
        /// <summary>
        /// Releases the semaphore, making the lock available to other threads.
        /// </summary>
        /// <remarks>
        /// This method is called automatically when the LockReleaser is disposed,
        /// typically through a 'using' statement or explicit disposal.
        /// </remarks>
        public void Dispose() => semaphore.Release();
    }
}