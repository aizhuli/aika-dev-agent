using Aica.Tools.Impl;
using Xunit;

namespace aica.Tests.Tools;

public class ExecuteCommandToolTests : ToolTestBase
{
    private readonly ExecuteCommandTool _tool = new();

    [Fact]
    public async Task CapturesStdout()
    {
        var cmd = OperatingSystem.IsWindows() ? "echo hello" : "echo hello";
        var result = await _tool.ExecuteAsync(Args(("command", cmd)), Config);
        Assert.False(result.IsError);
        Assert.Contains("hello", result.Content);
    }

    [Fact]
    public async Task ReportsNonZeroExitCode()
    {
        var cmd = OperatingSystem.IsWindows() ? "exit 1" : "exit 1";
        var result = await _tool.ExecuteAsync(Args(("command", cmd)), Config);
        Assert.True(result.IsError);
        Assert.Contains("exit code: 1", result.Content);
    }

    [Fact]
    public async Task RunsInWorkingDirectory()
    {
        CreateFile("marker.txt", "found");
        var cmd = OperatingSystem.IsWindows() ? "dir marker.txt" : "ls marker.txt";
        var result = await _tool.ExecuteAsync(Args(("command", cmd)), Config);
        Assert.False(result.IsError);
        Assert.Contains("marker.txt", result.Content);
    }

    [Fact]
    public async Task TimesOutLongRunningCommand()
    {
        var cmd = OperatingSystem.IsWindows() ? "ping -n 10 127.0.0.1" : "sleep 10";
        var result = await _tool.ExecuteAsync(
            Args(("command", cmd), ("timeout_seconds", 1)), Config);
        Assert.True(result.IsError);
        Assert.Contains("timed out", result.Content, StringComparison.OrdinalIgnoreCase);
    }
}
