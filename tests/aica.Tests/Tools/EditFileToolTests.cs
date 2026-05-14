using Aica.Tools.Impl;
using Xunit;

namespace aica.Tests.Tools;

public class EditFileToolTests : ToolTestBase
{
    private readonly EditFileTool _tool = new();

    [Fact]
    public async Task ReplacesExactString()
    {
        CreateFile("code.cs", "var x = 1;\nvar y = 2;");
        var result = await _tool.ExecuteAsync(
            Args(("path", "code.cs"), ("old_string", "var x = 1;"), ("new_string", "var x = 42;")), Config);
        Assert.False(result.IsError);
        Assert.Contains("var x = 42;", File.ReadAllText(Path.Combine(TempDir, "code.cs")));
    }

    [Fact]
    public async Task ReturnsErrorWhenStringNotFound()
    {
        CreateFile("code.cs", "hello");
        var result = await _tool.ExecuteAsync(
            Args(("path", "code.cs"), ("old_string", "world"), ("new_string", "!")), Config);
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReturnsErrorWhenMultipleMatches()
    {
        CreateFile("dup.cs", "foo\nfoo");
        var result = await _tool.ExecuteAsync(
            Args(("path", "dup.cs"), ("old_string", "foo"), ("new_string", "bar")), Config);
        Assert.True(result.IsError);
        Assert.Contains("2", result.Content);
    }

    [Fact]
    public async Task ReturnsErrorForMissingFile()
    {
        var result = await _tool.ExecuteAsync(
            Args(("path", "nope.txt"), ("old_string", "a"), ("new_string", "b")), Config);
        Assert.True(result.IsError);
    }
}
