using Aica.Tools.Impl;
using Xunit;

namespace aica.Tests.Tools;

public class ReadFileToolTests : ToolTestBase
{
    private readonly ReadFileTool _tool = new();

    [Fact]
    public async Task ReadsFileWithLineNumbers()
    {
        CreateFile("hello.txt", "line1\nline2\nline3");
        var result = await _tool.ExecuteAsync(Args(("path", "hello.txt")), Config);
        Assert.False(result.IsError);
        Assert.Contains("line1", result.Content);
        Assert.Contains("1\t", result.Content);
    }

    [Fact]
    public async Task ReturnsErrorForMissingFile()
    {
        var result = await _tool.ExecuteAsync(Args(("path", "ghost.txt")), Config);
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RespectsOffsetAndLimit()
    {
        CreateFile("multi.txt", "a\nb\nc\nd\ne");
        var result = await _tool.ExecuteAsync(Args(("path", "multi.txt"), ("offset", 2), ("limit", 2)), Config);
        Assert.False(result.IsError);
        Assert.Contains("b", result.Content);
        Assert.Contains("c", result.Content);
        Assert.DoesNotContain("a", result.Content);
        Assert.DoesNotContain("d", result.Content);
    }

    [Fact]
    public async Task RejectsPathOutsideWorkdir()
    {
        var result = await _tool.ExecuteAsync(Args(("path", "../../etc/passwd")), Config);
        Assert.True(result.IsError);
        Assert.Contains("outside", result.Content, StringComparison.OrdinalIgnoreCase);
    }
}
