using Aica.Tools.Impl;
using Xunit;

namespace aica.Tests.Tools;

public class DeleteFileToolTests : ToolTestBase
{
    private readonly DeleteFileTool _tool = new();

    [Fact]
    public async Task DeletesExistingFile()
    {
        CreateFile("bye.txt");
        var result = await _tool.ExecuteAsync(Args(("path", "bye.txt")), Config);
        Assert.False(result.IsError);
        Assert.False(File.Exists(Path.Combine(TempDir, "bye.txt")));
    }

    [Fact]
    public async Task DeletesEmptyDirectory()
    {
        var dir = Path.Combine(TempDir, "emptydir");
        Directory.CreateDirectory(dir);
        var result = await _tool.ExecuteAsync(Args(("path", "emptydir")), Config);
        Assert.False(result.IsError);
        Assert.False(Directory.Exists(dir));
    }

    [Fact]
    public async Task RejectsNonEmptyDirectory()
    {
        CreateFile("nonempty/file.txt");
        var result = await _tool.ExecuteAsync(Args(("path", "nonempty")), Config);
        Assert.True(result.IsError);
        Assert.Contains("not empty", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReturnsErrorForMissingPath()
    {
        var result = await _tool.ExecuteAsync(Args(("path", "ghost.txt")), Config);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task RejectsPathOutsideWorkdir()
    {
        var result = await _tool.ExecuteAsync(Args(("path", "../outside.txt")), Config);
        Assert.True(result.IsError);
    }
}
