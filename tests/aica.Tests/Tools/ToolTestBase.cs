using System.Text.Json.Nodes;
using Aica;

namespace aica.Tests.Tools;

public abstract class ToolTestBase : IDisposable
{
    protected readonly string TempDir;
    protected readonly AppConfig Config;

    protected ToolTestBase()
    {
        TempDir = Path.Combine(Path.GetTempPath(), $"aica-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(TempDir);
        Config = new AppConfig
        {
            WorkingDirectory = TempDir,
            Model = "claude-sonnet-4-6",
            ApiKey = "test",
            AutoApprove = true
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(TempDir))
            Directory.Delete(TempDir, recursive: true);
    }

    protected string CreateFile(string relativePath, string content = "hello")
    {
        var full = Path.Combine(TempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return relativePath;
    }

    protected static JsonObject Args(params (string key, object? value)[] pairs)
    {
        var obj = new JsonObject();
        foreach (var (key, value) in pairs)
            obj[key] = value is null ? null : JsonValue.Create(value);
        return obj;
    }
}
