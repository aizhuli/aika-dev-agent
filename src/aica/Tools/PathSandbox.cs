namespace Aica.Tools;

public static class PathSandbox
{
    public static string Resolve(string workingDir, string path)
    {
        var full = Path.GetFullPath(path, workingDir);
        if (!full.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException(
                $"Path '{path}' is outside the working directory.");
        return full;
    }
}
