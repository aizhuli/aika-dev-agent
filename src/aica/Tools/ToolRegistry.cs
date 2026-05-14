using Aica.Tools.Impl;

namespace Aica.Tools;

public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools;

    public ToolRegistry()
    {
        var tools = new ITool[]
        {
            new ReadFileTool(),
            new WriteFileTool(),
            new EditFileTool(),
            new DeleteFileTool(),
            new ListDirectoryTool(),
            new SearchFilesTool(),
            new GrepSearchTool(),
            new ExecuteCommandTool()
        };
        _tools = tools.ToDictionary(t => t.Name);
    }

    public IReadOnlyCollection<ITool> All => _tools.Values;

    public ITool? Get(string name) => _tools.GetValueOrDefault(name);
}
