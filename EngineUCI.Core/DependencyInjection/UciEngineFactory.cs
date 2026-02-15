using System.Collections.ObjectModel;
using EngineUCI.Core.Engine;

namespace EngineUCI.Core.DependencyInjection;

public class UciEngineFactory : IUciEngineFactory
{
    private SemaphoreSlim PoolSemaphore { get; init; }
    private ReadOnlyDictionary<string, Func<IUciEngine>> Registrations { get; init; }

    public UciEngineFactory(UciEngineFactorySettings settings)
    {
        PoolSemaphore = new(settings.MaxPoolSize, settings.MaxPoolSize);
        Registrations = settings.GetRegistrations();
    }

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

public class UciEngineFactorySettings
{
    public int MaxPoolSize { get; set; } = 16;
    private readonly Dictionary<string, Func<IUciEngine>> Registrations = new();

    public ReadOnlyDictionary<string, Func<IUciEngine>> GetRegistrations() => Registrations.AsReadOnly();

    public void RegisterNamedEngine(string name, Func<IUciEngine> factoryFunc) => Registrations.TryAdd(name, factoryFunc);
}
