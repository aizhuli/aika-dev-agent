using System.Text.Json.Nodes;

namespace Aica.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonObject InputSchema { get; }
    Task<ToolResult> ExecuteAsync(JsonNode? input, AppConfig config);
}
