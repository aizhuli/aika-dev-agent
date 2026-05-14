using System.Text.Json.Nodes;

namespace Aica.Tools.Impl;

public class DeleteFileTool : ITool
{
    public string Name => "delete_file";
    public string Description => "Delete a file or an empty directory.";

    public JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Path to the file or empty directory (relative to working directory)" }
        },
        ["required"] = new JsonArray { "path" }
    };

    public Task<ToolResult> ExecuteAsync(JsonNode? input, AppConfig config)
    {
        var path = input?["path"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(ToolResult.Error("'path' is required."));

        string fullPath;
        try { fullPath = PathSandbox.Resolve(config.WorkingDirectory, path); }
        catch (UnauthorizedAccessException ex) { return Task.FromResult(ToolResult.Error(ex.Message)); }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(ToolResult.Ok($"Deleted file: {path}"));
        }

        if (Directory.Exists(fullPath))
        {
            if (Directory.EnumerateFileSystemEntries(fullPath).Any())
                return Task.FromResult(ToolResult.Error($"Directory is not empty: {path}"));
            Directory.Delete(fullPath);
            return Task.FromResult(ToolResult.Ok($"Deleted directory: {path}"));
        }

        return Task.FromResult(ToolResult.Error($"Not found: {path}"));
    }
}
