using Aica.Tools.Impl;
using Xunit;

namespace aica.Tests.Tools;

public class ListDirectoryToolTests : ToolTestBase
{
    private readonly ListDirectoryTool _tool = new();

    [Fact]
    public async Task ListsFilesAndDirs()
    {
        CreateFile("a.txt");
        Directory.CreateDirectory(Path.Combine(TempDir, "subdir"));
        var result = await _tool.ExecuteAsync(Args(), Config);
        Assert.False(result.IsError);
        Assert.Contains("a.txt", result.Content);
        Assert.Contains("subdir", result.Content);
    }

    [Fact]
    public async Task ReturnsEmptyMessageForEmptyDir()
    {
        var result = await _tool.ExecuteAsync(Args(), Config);
        Assert.False(result.IsError);
        Assert.Contains("empty", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListsSubdirectory()
    {
        Directory.CreateDirectory(Path.Combine(TempDir, "sub"));
        CreateFile("sub/file.txt", "x");
        var result = await _tool.ExecuteAsync(Args(("path", "sub")), Config);
        Assert.False(result.IsError);
        Assert.Contains("file.txt", result.Content);
    }

    [Fact]
    public async Task ReturnsErrorForMissingDir()
    {
        var result = await _tool.ExecuteAsync(Args(("path", "ghost")), Config);
        Assert.True(result.IsError);
    }
}
