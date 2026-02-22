using Microsoft.Extensions.DependencyInjection;

namespace EngineUCI.Core.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to integrate UCI engine factory
/// services with Microsoft.Extensions.DependencyInjection container.
/// </summary>
/// <remarks>
/// This static class contains extension methods that simplify the registration of UCI engine factory
/// services in the dependency injection container. The extensions handle the configuration of
/// <see cref="UciEngineFactorySettings"/> and register the <see cref="IUciEngineFactory"/> as a singleton
/// service for efficient resource management and consistent engine access throughout the application.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the UCI engine factory as a singleton service in the dependency injection container
    /// with the specified configuration settings.
    /// </summary>
    /// <param name="serviceCollection">
    /// The <see cref="IServiceCollection"/> to add the UCI engine factory services to.
    /// Cannot be null.
    /// </param>
    /// <param name="settingsAction">
    /// An optional configuration action that allows customization of <see cref="UciEngineFactorySettings"/>.
    /// This action can be used to set the maximum pool size and register named engine factories.
    /// If null, default settings will be used.
    /// </param>
    /// <remarks>
    /// This extension method performs the following operations:
    /// 1. Creates a new <see cref="UciEngineFactorySettings"/> instance with default values
    /// 2. Invokes the provided configuration action to customize the settings (if provided)
    /// 3. Registers the settings as a singleton so the DI container can inject them
    /// 4. Registers <see cref="UciEngineFactory"/> as a singleton <see cref="IUciEngineFactory"/> service
    ///
    /// The factory is registered as a singleton to ensure efficient resource management and
    /// consistent pool behavior across the application. All components that depend on
    /// <see cref="IUciEngineFactory"/> will receive the same factory instance.
    ///
    /// If the application has registered an <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/>
    /// (e.g. via <c>services.AddLogging(...)</c>), the DI container automatically injects it into
    /// <see cref="UciEngineFactory"/>, enabling structured logging with no additional configuration.
    /// Log verbosity is controlled through standard log-level configuration rather than a flag on settings.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.UseUciEngineFactory(settings =>
    /// {
    ///     settings.MaxPoolSize = 8;
    ///     settings.RegisterNamedEngine("stockfish", () => new UciEngine("stockfish.exe"));
    ///     settings.RegisterNamedEngine("komodo", () => new UciEngine("komodo.exe"));
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceCollection"/> is null.
    /// </exception>
    public static void UseUciEngineFactory(this IServiceCollection serviceCollection, Action<UciEngineFactorySettings> settingsAction)
    {
        var settings = new UciEngineFactorySettings();
        settingsAction?.Invoke(settings);

        serviceCollection.AddSingleton(settings);
        serviceCollection.AddSingleton<IUciEngineFactory, UciEngineFactory>();
    }
}