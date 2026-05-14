using System.Text.Json.Nodes;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Aica.Tools.Impl;

public class SearchFilesTool : ITool
{
    public string Name => "search_files";
    public string Description => "Find files by glob pattern (e.g. '**/*.cs', 'src/**/*.json').";

    public JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["pattern"] = new JsonObject { ["type"] = "string", ["description"] = "Glob pattern to match files against" },
            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Directory to search in (relative to working directory, defaults to working directory)" }
        },
        ["required"] = new JsonArray { "pattern" }
    };

    public Task<ToolResult> ExecuteAsync(JsonNode? input, AppConfig config)
    {
        var pattern = input?["pattern"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(pattern)) return Task.FromResult(ToolResult.Error("'pattern' is required."));

        var path = input?["path"]?.GetValue<string>() ?? ".";

        string fullPath;
        try { fullPath = PathSandbox.Resolve(config.WorkingDirectory, path); }
        catch (UnauthorizedAccessException ex) { return Task.FromResult(ToolResult.Error(ex.Message)); }

        if (!Directory.Exists(fullPath))
            return Task.FromResult(ToolResult.Error($"Directory not found: {path}"));

        var matcher = new Matcher();
        matcher.AddInclude(pattern);

        var results = matcher.GetResultsInFullPath(fullPath)
            .Select(p => Path.GetRelativePath(config.WorkingDirectory, p))
            .Order()
            .ToList();

        return Task.FromResult(results.Count == 0
            ? ToolResult.Ok("No files matched.")
            : ToolResult.Ok(string.Join('\n', results)));
    }
}
