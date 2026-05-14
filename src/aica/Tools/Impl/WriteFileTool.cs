using System.Text.Json.Nodes;

namespace Aica.Tools.Impl;

public class WriteFileTool : ITool
{
    public string Name => "write_file";
    public string Description => "Create a new file or fully overwrite an existing one. Prefer edit_file for modifying existing files.";

    public JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Path to the file (relative to working directory)" },
            ["content"] = new JsonObject { ["type"] = "string", ["description"] = "Content to write" }
        },
        ["required"] = new JsonArray { "path", "content" }
    };

    public async Task<ToolResult> ExecuteAsync(JsonNode? input, AppConfig config)
    {
        var path = input?["path"]?.GetValue<string>();
        var content = input?["content"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(path)) return ToolResult.Error("'path' is required.");
        if (content is null) return ToolResult.Error("'content' is required.");

        string fullPath;
        try { fullPath = PathSandbox.Resolve(config.WorkingDirectory, path); }
        catch (UnauthorizedAccessException ex) { return ToolResult.Error(ex.Message); }

        var dir = Path.GetDirectoryName(fullPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(fullPath, content);
        return ToolResult.Ok($"Written: {path}");
    }
}
