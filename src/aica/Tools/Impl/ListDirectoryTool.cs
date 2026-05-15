using System.Text;
using System.Text.Json.Nodes;

namespace Aica.Tools.Impl;

public class ListDirectoryTool : ITool
{
    public string Name => "list_directory";
    public string Description => "List files and directories at a given path.";

    public JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Directory path (relative to working directory, defaults to working directory)" }
        },
        ["required"] = new JsonArray()
    };

    public Task<ToolResult> ExecuteAsync(JsonNode? input, AppConfig config)
    {
        var path = input?["path"]?.GetValue<string>() ?? ".";

        string fullPath;
        try { fullPath = PathSandbox.Resolve(config.WorkingDirectory, path); }
        catch (UnauthorizedAccessException ex) { return Task.FromResult(ToolResult.Error(ex.Message)); }

        if (!Directory.Exists(fullPath))
            return Task.FromResult(ToolResult.Error($"Directory not found: {path}"));

        const int MaxEntries = 500;
        var sb = new StringBuilder();
        var count = 0;

        foreach (var dir in Directory.EnumerateDirectories(fullPath).Order())
        {
            if (count++ >= MaxEntries) break;
            sb.AppendLine($"[dir]  {Path.GetFileName(dir)}/");
        }
        foreach (var file in Directory.EnumerateFiles(fullPath).Order())
        {
            if (count++ >= MaxEntries) { sb.AppendLine($"... (truncated at {MaxEntries} entries)"); break; }
            var info = new FileInfo(file);
            sb.AppendLine($"[file] {info.Name} ({FormatSize(info.Length)})");
        }

        return Task.FromResult(ToolResult.Ok(sb.Length > 0 ? sb.ToString().TrimEnd() : "(empty directory)"));
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes}B",
        < 1024 * 1024 => $"{bytes / 1024}KB",
        _ => $"{bytes / (1024 * 1024)}MB"
    };
}
