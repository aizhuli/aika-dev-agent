using Aica.Tools.Impl;
using Xunit;

namespace aica.Tests.Tools;

public class GrepSearchToolTests : ToolTestBase
{
    private readonly GrepSearchTool _tool = new();

    [Fact]
    public async Task FindsMatchingLines()
    {
        CreateFile("file.cs", "public class Foo\n{\n    public void Bar() {}\n}");
        var result = await _tool.ExecuteAsync(Args(("pattern", "public")), Config);
        Assert.False(result.IsError);
        Assert.Contains("file.cs", result.Content);
        Assert.Contains("public class Foo", result.Content);
    }

    [Fact]
    public async Task ReturnsNoMatchMessage()
    {
        CreateFile("empty.txt", "nothing here");
        var result = await _tool.ExecuteAsync(Args(("pattern", "xyzzy")), Config);
        Assert.False(result.IsError);
        Assert.Contains("No matches", result.Content);
    }

    [Fact]
    public async Task SupportsCaseInsensitive()
    {
        CreateFile("mixed.txt", "Hello World");
        var result = await _tool.ExecuteAsync(
            Args(("pattern", "hello"), ("case_insensitive", true)), Config);
        Assert.False(result.IsError);
        Assert.Contains("Hello World", result.Content);
    }

    [Fact]
    public async Task ReturnsErrorForInvalidRegex()
    {
        var result = await _tool.ExecuteAsync(Args(("pattern", "[")), Config);
        Assert.True(result.IsError);
        Assert.Contains("Invalid regex", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IncludesLineNumbers()
    {
        CreateFile("code.txt", "a\nfind me\nc");
        var result = await _tool.ExecuteAsync(Args(("pattern", "find me")), Config);
        Assert.False(result.IsError);
        Assert.Contains(":2:", result.Content);
    }
}
