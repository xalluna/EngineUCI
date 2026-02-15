using Microsoft.Extensions.DependencyInjection;

namespace EngineUCI.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void UseUciEngineFactory(this IServiceCollection serviceCollection, Action<UciEngineFactorySettings> settingsAction)
    {
        var settings = new UciEngineFactorySettings();
        settingsAction?.Invoke(settings);

        serviceCollection.AddSingleton<IUciEngineFactory>(new UciEngineFactory(settings));
    }
}