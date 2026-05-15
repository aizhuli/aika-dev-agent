namespace Aica.Tools;

public static class PathSandbox
{
    public static string Resolve(string workingDir, string path)
    {
        var full = Path.GetFullPath(path, workingDir);
        // Normalize both with trailing separator to prevent prefix bypass:
        // e.g., workingDir="C:\foo" should NOT match full="C:\foobar\file.txt"
        var sep = Path.DirectorySeparatorChar;
        var normalizedWorkingDir = workingDir.TrimEnd(sep, Path.AltDirectorySeparatorChar) + sep;
        var normalizedFull = full.TrimEnd(sep, Path.AltDirectorySeparatorChar) + sep;
        if (!normalizedFull.StartsWith(normalizedWorkingDir, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException(
                $"Path '{path}' is outside the working directory.");
        return full;
    }
}
