using Aica.Tools;
using Xunit;

namespace aica.Tests.Tools;

public class PathSandboxTests
{
    private readonly string _workDir;

    public PathSandboxTests()
    {
        _workDir = Path.Combine(Path.GetTempPath(), $"sandbox-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workDir);
    }

    [Fact]
    public void AllowsPathInsideWorkingDirectory()
    {
        var result = PathSandbox.Resolve(_workDir, "subdir/file.txt");
        Assert.StartsWith(_workDir, result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AllowsWorkingDirectoryItself()
    {
        var result = PathSandbox.Resolve(_workDir, ".");
        Assert.Equal(_workDir, result, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsParentDirectoryTraversal()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            PathSandbox.Resolve(_workDir, "../escape.txt"));
    }

    [Fact]
    public void RejectsAbsolutePathOutsideWorkingDirectory()
    {
        var outsidePath = Path.GetTempPath();
        Assert.Throws<UnauthorizedAccessException>(() =>
            PathSandbox.Resolve(_workDir, outsidePath));
    }

    [Fact]
    public void RejectsSiblingDirectoryWithSharedPrefix()
    {
        // e.g. workDir = "/tmp/foo", sibling = "/tmp/foobar" — must NOT be allowed
        var parentDir = Path.GetDirectoryName(_workDir)!;
        var workDirName = Path.GetFileName(_workDir);
        var siblingPath = Path.Combine(parentDir, workDirName + "evil", "secret.txt");

        Assert.Throws<UnauthorizedAccessException>(() =>
            PathSandbox.Resolve(_workDir, siblingPath));
    }

    [Fact]
    public void AllowsDeepNestedPath()
    {
        var result = PathSandbox.Resolve(_workDir, "a/b/c/d/file.txt");
        Assert.StartsWith(_workDir, result, StringComparison.OrdinalIgnoreCase);
    }
}
