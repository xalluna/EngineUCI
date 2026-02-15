using EngineUCI.Core.Engine;

namespace EngineUCI.Core.DependencyInjection;

/// <summary>
/// Defines a factory interface for creating and managing UCI chess engine instances with pooling support.
/// Provides both synchronous and asynchronous methods for retrieving registered engines by name,
/// with automatic resource pooling to control concurrent engine usage.
/// </summary>
/// <remarks>
/// The factory uses a semaphore-based pooling mechanism to limit the number of concurrent engine instances,
/// preventing resource exhaustion while providing efficient access to UCI engines. Engines are automatically
/// returned to the pool when disposed.
/// </remarks>
public interface IUciEngineFactory
{
    /// <summary>
    /// Synchronously retrieves a registered UCI engine instance by name from the factory pool.
    /// This method will block until a pool slot becomes available if the maximum pool size has been reached.
    /// </summary>
    /// <param name="name">The registered name of the UCI engine to retrieve.</param>
    /// <returns>
    /// A UCI engine instance that implements <see cref="IUciEngine"/>. The engine is automatically
    /// enrolled in pool management and will be returned to the pool when disposed.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no engine factory is registered with the specified <paramref name="name"/>.
    /// </exception>
    /// <remarks>
    /// This method will block the calling thread if all pool slots are currently in use. For non-blocking
    /// behavior, use <see cref="GetEngineAsync(string)"/> instead. The returned engine must be disposed
    /// to return the pool slot for reuse.
    /// </remarks>
    IUciEngine GetEngine(string name);

    /// <summary>
    /// Asynchronously retrieves a registered UCI engine instance by name from the factory pool.
    /// This method will await until a pool slot becomes available if the maximum pool size has been reached.
    /// </summary>
    /// <param name="name">The registered name of the UCI engine to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a UCI engine instance
    /// that implements <see cref="IUciEngine"/>. The engine is automatically enrolled in pool management
    /// and will be returned to the pool when disposed.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no engine factory is registered with the specified <paramref name="name"/>.
    /// </exception>
    /// <remarks>
    /// This method provides non-blocking asynchronous access to the engine pool. If all pool slots are
    /// currently in use, the method will await until a slot becomes available. The returned engine must
    /// be disposed to return the pool slot for reuse.
    /// </remarks>
    Task<IUciEngine> GetEngineAsync(string name);
}