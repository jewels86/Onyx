namespace Shared;

public interface IPluginModule
{
    public string Name { get; }
    public Task ExecuteAsync();
}