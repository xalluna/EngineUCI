using EngineUCI.Core.Engine;

namespace EngineUCI.Core.DependencyInjection;

public interface IUciEngineFactory
{
    IUciEngine GetEngine(string name);
    Task<IUciEngine> GetEngineAsync(string name);
}