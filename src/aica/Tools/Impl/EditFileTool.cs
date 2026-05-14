using System.Text.Json.Nodes;

namespace Aica.Tools.Impl;

public class EditFileTool : ITool
{
    public string Name => "edit_file";
    public string Description => "Replace an exact string in a file with a new string. The old_string must match exactly once in the file.";

    public JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Path to the file (relative to working directory)" },
            ["old_string"] = new JsonObject { ["type"] = "string", ["description"] = "Exact string to find and replace" },
            ["new_string"] = new JsonObject { ["type"] = "string", ["description"] = "Replacement string" }
        },
        ["required"] = new JsonArray { "path", "old_string", "new_string" }
    };

    public async Task<ToolResult> ExecuteAsync(JsonNode? input, AppConfig config)
    {
        var path = input?["path"]?.GetValue<string>();
        var oldString = input?["old_string"]?.GetValue<string>();
        var newString = input?["new_string"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(path)) return ToolResult.Error("'path' is required.");
        if (oldString is null) return ToolResult.Error("'old_string' is required.");
        if (newString is null) return ToolResult.Error("'new_string' is required.");

        string fullPath;
        try { fullPath = PathSandbox.Resolve(config.WorkingDirectory, path); }
        catch (UnauthorizedAccessException ex) { return ToolResult.Error(ex.Message); }

        if (!File.Exists(fullPath))
            return ToolResult.Error($"File not found: {path}");

        var content = await File.ReadAllTextAsync(fullPath);
        var count = CountOccurrences(content, oldString);

        if (count == 0)
            return ToolResult.Error($"'old_string' not found in {path}.");
        if (count > 1)
            return ToolResult.Error($"'old_string' matches {count} times in {path}. Provide more context to make it unique.");

        var updated = content.Replace(oldString, newString, StringComparison.Ordinal);
        await File.WriteAllTextAsync(fullPath, updated);
        return ToolResult.Ok($"Edited: {path}");
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
