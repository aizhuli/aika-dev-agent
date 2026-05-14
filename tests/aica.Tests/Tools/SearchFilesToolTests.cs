using Aica.Tools.Impl;
using Xunit;

namespace aica.Tests.Tools;

public class SearchFilesToolTests : ToolTestBase
{
    private readonly SearchFilesTool _tool = new();

    [Fact]
    public async Task FindsFilesByGlob()
    {
        CreateFile("src/Foo.cs", "x");
        CreateFile("src/Bar.cs", "x");
        CreateFile("README.md", "x");
        var result = await _tool.ExecuteAsync(Args(("pattern", "**/*.cs")), Config);
        Assert.False(result.IsError);
        Assert.Contains("Foo.cs", result.Content);
        Assert.Contains("Bar.cs", result.Content);
        Assert.DoesNotContain("README.md", result.Content);
    }

    [Fact]
    public async Task ReturnsNoMatchMessage()
    {
        var result = await _tool.ExecuteAsync(Args(("pattern", "**/*.nonexistent")), Config);
        Assert.False(result.IsError);
        Assert.Contains("No files matched", result.Content);
    }

    [Fact]
    public async Task SearchesInSubdirectory()
    {
        CreateFile("a/deep.txt", "x");
        CreateFile("b/other.txt", "x");
        var result = await _tool.ExecuteAsync(Args(("pattern", "**/*.txt"), ("path", "a")), Config);
        Assert.False(result.IsError);
        Assert.Contains("deep.txt", result.Content);
        Assert.DoesNotContain("other.txt", result.Content);
    }
}
