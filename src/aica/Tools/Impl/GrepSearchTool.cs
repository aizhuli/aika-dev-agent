using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Aica.Tools.Impl;

public class GrepSearchTool : ITool
{
    public string Name => "grep_search";
    public string Description => "Search file contents using a regex pattern. Returns matching lines with file path and line number.";

    public JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["pattern"] = new JsonObject { ["type"] = "string", ["description"] = "Regular expression to search for" },
            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Directory or file to search in (relative to working directory, defaults to working directory)" },
            ["glob"] = new JsonObject { ["type"] = "string", ["description"] = "Glob pattern to filter files (e.g. '*.cs'). Only used when path is a directory." },
            ["case_insensitive"] = new JsonObject { ["type"] = "boolean", ["description"] = "Case-insensitive matching (default false)" }
        },
        ["required"] = new JsonArray { "pattern" }
    };

    public async Task<ToolResult> ExecuteAsync(JsonNode? input, AppConfig config)
    {
        var pattern = input?["pattern"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(pattern)) return ToolResult.Error("'pattern' is required.");

        var path = input?["path"]?.GetValue<string>() ?? ".";
        var glob = input?["glob"]?.GetValue<string>() ?? "**/*";
        var caseInsensitive = input?["case_insensitive"]?.GetValue<bool>() ?? false;

        string fullPath;
        try { fullPath = PathSandbox.Resolve(config.WorkingDirectory, path); }
        catch (UnauthorizedAccessException ex) { return ToolResult.Error(ex.Message); }

        Regex regex;
        try
        {
            var options = RegexOptions.Compiled | (caseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None);
            regex = new Regex(pattern, options);
        }
        catch (RegexParseException ex)
        {
            return ToolResult.Error($"Invalid regex: {ex.Message}");
        }

        IEnumerable<string> files;
        if (File.Exists(fullPath))
        {
            files = [fullPath];
        }
        else if (Directory.Exists(fullPath))
        {
            var matcher = new Matcher();
            matcher.AddInclude(glob);
            files = matcher.GetResultsInFullPath(fullPath);
        }
        else
        {
            return ToolResult.Error($"Path not found: {path}");
        }

        var sb = new StringBuilder();
        var matchCount = 0;
        const int maxMatches = 200;

        foreach (var file in files.Order())
        {
            if (matchCount >= maxMatches) break;
            var lines = await File.ReadAllLinesAsync(file);
            var relativePath = Path.GetRelativePath(config.WorkingDirectory, file);

            for (var i = 0; i < lines.Length && matchCount < maxMatches; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    sb.AppendLine($"{relativePath}:{i + 1}: {lines[i]}");
                    matchCount++;
                }
            }
        }

        if (matchCount == 0) return ToolResult.Ok("No matches found.");
        if (matchCount >= maxMatches) sb.AppendLine($"... (truncated at {maxMatches} matches)");

        return ToolResult.Ok(sb.ToString().TrimEnd());
    }
}
