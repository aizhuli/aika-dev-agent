using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;

namespace Aica.Tools.Impl;

public class ExecuteCommandTool : ITool
{
    public string Name => "execute_command";
    public string Description => "Run a shell command in the working directory and return its output (stdout, stderr, exit code).";

    public JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["command"] = new JsonObject { ["type"] = "string", ["description"] = "The shell command to execute" },
            ["timeout_seconds"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum seconds to wait (default 30, max 300)" }
        },
        ["required"] = new JsonArray { "command" }
    };

    public async Task<ToolResult> ExecuteAsync(JsonNode? input, AppConfig config)
    {
        var command = input?["command"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(command)) return ToolResult.Error("'command' is required.");

        var timeoutSeconds = Math.Min(input?["timeout_seconds"]?.GetValue<int>() ?? 30, 300);

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var shell = isWindows ? "cmd.exe" : "/bin/sh";
        var shellArgs = isWindows ? $"/c {command}" : $"-c \"{command}\"";

        var psi = new ProcessStartInfo(shell, shellArgs)
        {
            WorkingDirectory = config.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var completed = await Task.Run(() => process.WaitForExit(TimeSpan.FromSeconds(timeoutSeconds)));

        if (!completed)
        {
            process.Kill(entireProcessTree: true);
            return ToolResult.Error($"Command timed out after {timeoutSeconds}s.");
        }

        var result = new StringBuilder();
        if (stdout.Length > 0) result.Append(stdout);
        if (stderr.Length > 0) result.AppendLine($"\n[stderr]\n{stderr}");
        result.AppendLine($"\n[exit code: {process.ExitCode}]");

        var isError = process.ExitCode != 0;
        return new ToolResult(result.ToString().TrimEnd(), isError);
    }
}
