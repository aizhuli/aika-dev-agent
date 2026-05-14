using Aica.Tools.Impl;
using Xunit;

namespace aica.Tests.Tools;

public class WriteFileToolTests : ToolTestBase
{
    private readonly WriteFileTool _tool = new();

    [Fact]
    public async Task CreatesNewFile()
    {
        var result = await _tool.ExecuteAsync(Args(("path", "new.txt"), ("content", "hello")), Config);
        Assert.False(result.IsError);
        Assert.Equal("hello", File.ReadAllText(Path.Combine(TempDir, "new.txt")));
    }

    [Fact]
    public async Task OverwritesExistingFile()
    {
        CreateFile("existing.txt", "old");
        var result = await _tool.ExecuteAsync(Args(("path", "existing.txt"), ("content", "new")), Config);
        Assert.False(result.IsError);
        Assert.Equal("new", File.ReadAllText(Path.Combine(TempDir, "existing.txt")));
    }

    [Fact]
    public async Task CreatesParentDirectories()
    {
        var result = await _tool.ExecuteAsync(Args(("path", "a/b/c.txt"), ("content", "deep")), Config);
        Assert.False(result.IsError);
        Assert.True(File.Exists(Path.Combine(TempDir, "a", "b", "c.txt")));
    }

    [Fact]
    public async Task RejectsPathOutsideWorkdir()
    {
        var result = await _tool.ExecuteAsync(Args(("path", "../escape.txt"), ("content", "x")), Config);
        Assert.True(result.IsError);
    }
}
