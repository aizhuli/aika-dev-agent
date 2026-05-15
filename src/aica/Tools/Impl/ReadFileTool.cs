using System.Text;
using System.Text.Json.Nodes;

namespace Aica.Tools.Impl;

public class ReadFileTool : ITool
{
    public string Name => "read_file";
    public string Description => "Read the contents of a file. Returns contents with line numbers. Use offset and limit to read a specific range.";

    public JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Path to the file (relative to working directory)" },
            ["offset"] = new JsonObject { ["type"] = "integer", ["description"] = "Line number to start reading from (1-based, optional)" },
            ["limit"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum number of lines to read (optional)" }
        },
        ["required"] = new JsonArray { "path" }
    };

    public async Task<ToolResult> ExecuteAsync(JsonNode? input, AppConfig config)
    {
        var path = input?["path"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(path))
            return ToolResult.Error("'path' is required.");

        string fullPath;
        try { fullPath = PathSandbox.Resolve(config.WorkingDirectory, path); }
        catch (UnauthorizedAccessException ex) { return ToolResult.Error(ex.Message); }

        if (!File.Exists(fullPath))
            return ToolResult.Error($"File not found: {path}");

        const long MaxBytes = 10 * 1024 * 1024; // 10 MB
        var fileInfo = new FileInfo(fullPath);
        if (fileInfo.Length > MaxBytes)
            return ToolResult.Error($"File too large ({fileInfo.Length / 1024 / 1024}MB). Use offset/limit to read specific sections.");

        var offset = input?["offset"]?.GetValue<int>() ?? 1;
        var limit = input?["limit"]?.GetValue<int>();

        var lines = await File.ReadAllLinesAsync(fullPath);
        var startIndex = Math.Max(0, offset - 1);
        var slice = limit.HasValue
            ? lines.Skip(startIndex).Take(limit.Value)
            : lines.Skip(startIndex);

        var sb = new StringBuilder();
        var lineNum = startIndex + 1;
        foreach (var line in slice)
            sb.AppendLine($"{lineNum++,6}\t{line}");

        return ToolResult.Ok(sb.Length > 0 ? sb.ToString() : "(empty file)");
    }
}
