using System.Collections.ObjectModel;
using EngineUCI.Core.Engine;

namespace EngineUCI.Core.DependencyInjection;

/// <summary>
/// Implements a thread-safe UCI engine factory with semaphore-based pooling for managing concurrent engine access.
/// This class provides controlled access to registered UCI engine instances, preventing resource exhaustion
/// by limiting the maximum number of concurrent engines through a semaphore-based pool management system.
/// </summary>
/// <remarks>
/// The factory uses a <see cref="SemaphoreSlim"/> to control the maximum number of concurrent engine instances.
/// When an engine is requested, the factory waits for an available pool slot, creates the engine using the
/// registered factory function, and automatically handles pool cleanup when the engine is disposed.
/// This design ensures efficient resource utilization while preventing system overload from too many
/// concurrent engine processes.
/// </remarks>
public class UciEngineFactory : IUciEngineFactory
{
    /// <summary>
    /// Gets the semaphore that controls access to the engine pool, limiting concurrent engine instances.
    /// </summary>
    /// <value>
    /// A <see cref="SemaphoreSlim"/> initialized with the maximum pool size as both initial and maximum count,
    /// ensuring controlled access to engine resources.
    /// </value>
    private SemaphoreSlim PoolSemaphore { get; init; }

    /// <summary>
    /// Gets the read-only dictionary containing registered engine factory functions mapped by name.
    /// </summary>
    /// <value>
    /// A <see cref="ReadOnlyDictionary{String, Func}"/> where keys are engine names and values are
    /// factory functions that create <see cref="IUciEngine"/> instances.
    /// </value>
    private ReadOnlyDictionary<string, Func<IUciEngine>> Registrations { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UciEngineFactory"/> class with the specified settings.
    /// </summary>
    /// <param name="settings">
    /// The configuration settings that define the maximum pool size and registered engine factories.
    /// Cannot be null.
    /// </param>
    /// <remarks>
    /// The constructor creates a semaphore with both initial and maximum counts set to the configured
    /// pool size, ensuring that the factory can immediately handle up to the maximum number of
    /// concurrent engines without blocking.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="settings"/> is null.
    /// </exception>
    public UciEngineFactory(UciEngineFactorySettings settings)
    {
        PoolSemaphore = new(settings.MaxPoolSize, settings.MaxPoolSize);
        Registrations = settings.GetRegistrations();
    }

    /// <summary>
    /// Synchronously retrieves a registered UCI engine instance by name from the factory pool.
    /// This method will block the calling thread until a pool slot becomes available if the maximum pool size has been reached.
    /// </summary>
    /// <param name="name">The registered name of the UCI engine to retrieve. Cannot be null or empty.</param>
    /// <returns>
    /// A UCI engine instance that implements <see cref="IUciEngine"/>. The engine is automatically
    /// enrolled in pool management and will be returned to the pool when disposed via the OnDispose event.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no engine factory is registered with the specified <paramref name="name"/>.
    /// The pool semaphore is properly released before throwing the exception.
    /// </exception>
    /// <remarks>
    /// This method follows these steps:
    /// 1. Acquires a semaphore slot (blocking if all slots are in use)
    /// 2. Looks up the engine factory function by name
    /// 3. Creates the engine instance using the factory function
    /// 4. Registers an OnDispose event handler to automatically release the semaphore slot
    /// 5. Returns the configured engine instance
    ///
    /// The calling thread will be blocked if all pool slots are currently in use. For non-blocking
    /// behavior, use <see cref="GetEngineAsync(string)" /> instead.
    /// </remarks>
    public IUciEngine GetEngine(string name)
    {
        PoolSemaphore.Wait();

        var hasFactory = Registrations.TryGetValue(name, out var uciEngineFactory);

        if (!hasFactory || uciEngineFactory is null)
        {
            PoolSemaphore.Release();
            throw new KeyNotFoundException($"{nameof(UciEngineFactory)} does not have a registration for \'{name}\'");
        }

        var engine = uciEngineFactory();
        engine.OnDispose += (_, __) => PoolSemaphore.Release();

        return engine;
    }

    /// <summary>
    /// Asynchronously retrieves a registered UCI engine instance by name from the factory pool.
    /// This method will await until a pool slot becomes available if the maximum pool size has been reached.
    /// </summary>
    /// <param name="name">The registered name of the UCI engine to retrieve. Cannot be null or empty.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a UCI engine instance
    /// that implements <see cref="IUciEngine"/>. The engine is automatically enrolled in pool management
    /// and will be returned to the pool when disposed via the OnDispose event.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no engine factory is registered with the specified <paramref name="name"/>.
    /// The pool semaphore is properly released before throwing the exception.
    /// </exception>
    /// <remarks>
    /// This method follows these steps:
    /// 1. Asynchronously acquires a semaphore slot (awaiting if all slots are in use)
    /// 2. Looks up the engine factory function by name
    /// 3. Creates the engine instance using the factory function
    /// 4. Registers an OnDispose event handler to automatically release the semaphore slot
    /// 5. Returns the configured engine instance
    ///
    /// This method provides non-blocking asynchronous access to the engine pool. If all pool slots are
    /// currently in use, the method will await until a slot becomes available, allowing other operations
    /// to continue on the calling thread.
    /// </remarks>
    public async Task<IUciEngine> GetEngineAsync(string name)
    {
        await PoolSemaphore.WaitAsync();

        var hasFactory = Registrations.TryGetValue(name, out var uciEngineFactory);

        if (!hasFactory || uciEngineFactory is null)
        {
            PoolSemaphore.Release();
            throw new KeyNotFoundException($"{nameof(UciEngineFactory)} does not have a registration for \'{name}\'");
        }

        var engine = uciEngineFactory();
        engine.OnDispose += (_, __) => PoolSemaphore.Release();

        return engine;
    }
}

/// <summary>
/// Provides configuration settings for the <see cref="UciEngineFactory"/>, including pool size limits
/// and engine factory registrations. This class manages the registration of named engine factory functions
/// and defines the maximum number of concurrent engine instances allowed.
/// </summary>
/// <remarks>
/// This class serves as a builder pattern for configuring the UCI engine factory. It maintains a dictionary
/// of engine factory functions that can be registered by name, and provides thread-safe access to these
/// registrations through a read-only view. The MaxPoolSize property controls resource usage by limiting
/// the number of engines that can be active simultaneously.
/// </remarks>
public class UciEngineFactorySettings
{
    /// <summary>
    /// Gets or sets the maximum number of concurrent UCI engine instances allowed by the factory pool.
    /// </summary>
    /// <value>
    /// The maximum pool size as an integer. Default value is 16.
    /// </value>
    /// <remarks>
    /// This property controls how many engine instances can be active simultaneously. Setting this value
    /// too high may cause resource exhaustion, while setting it too low may cause unnecessary blocking.
    /// The value should be chosen based on available system resources and expected concurrent usage patterns.
    /// </remarks>
    public int MaxPoolSize { get; set; } = 16;
    /// <summary>
    /// Contains the internal dictionary of registered engine factory functions mapped by name.
    /// </summary>
    /// <remarks>
    /// This dictionary stores the mapping between engine names and their corresponding factory functions.
    /// Access to this dictionary is controlled through the public methods to ensure thread safety
    /// and prevent direct modification after factory creation.
    /// </remarks>
    private readonly Dictionary<string, Func<IUciEngine>> Registrations = new();

    /// <summary>
    /// Returns a read-only view of the registered engine factory functions.
    /// </summary>
    /// <returns>
    /// A <see cref="ReadOnlyDictionary{String, Func}"/> containing all registered engine factories,
    /// where keys are engine names and values are factory functions that create <see cref="IUciEngine"/> instances.
    /// </returns>
    /// <remarks>
    /// This method provides thread-safe access to the registered factories without allowing external
    /// modification of the underlying collection. The returned dictionary reflects the current state
    /// of registrations at the time this method is called.
    /// </remarks>
    public ReadOnlyDictionary<string, Func<IUciEngine>> GetRegistrations() => Registrations.AsReadOnly();

    /// <summary>
    /// Registers a named engine factory function that can be used to create UCI engine instances.
    /// </summary>
    /// <param name="name">
    /// The unique name to associate with the engine factory. This name will be used to retrieve
    /// engines from the factory. Cannot be null or empty.
    /// </param>
    /// <param name="factoryFunc">
    /// A factory function that creates and returns a new <see cref="IUciEngine"/> instance.
    /// This function will be called each time an engine with the specified name is requested.
    /// Cannot be null.
    /// </param>
    /// <remarks>
    /// This method uses <see cref="Dictionary{TKey, TValue}.TryAdd"/> to prevent duplicate registrations.
    /// If an engine with the same name is already registered, the new registration will be silently
    /// ignored and the existing registration will remain active. This behavior prevents accidental
    /// overwrites of existing engine configurations.
    ///
    /// The factory function should create a new, uninitialized engine instance each time it is called.
    /// The factory is responsible for any engine-specific configuration that needs to occur before
    /// the engine is returned to the caller.
    /// </remarks>
    /// <example>
    /// <code>
    /// var settings = new UciEngineFactorySettings();
    /// settings.RegisterNamedEngine("stockfish", () => new UciEngine("stockfish.exe"));
    /// settings.RegisterNamedEngine("komodo", () => new UciEngine("komodo.exe"));
    /// </code>
    /// </example>
    public void RegisterNamedEngine(string name, Func<IUciEngine> factoryFunc) => Registrations.TryAdd(name, factoryFunc);
}
