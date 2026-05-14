using System.Text.Json;

namespace Aica;

public static class EnvFile
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aica", "env.json");

    public static Dictionary<string, string> Load()
    {
        if (!File.Exists(FilePath))
            return [];
        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void Save(Dictionary<string, string> vars)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(vars, JsonOptions));
    }

    // Load env.json and inject each entry into the process environment.
    // Existing env vars (set by the OS/shell) are never overwritten.
    public static void Apply()
    {
        foreach (var (key, value) in Load())
        {
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
