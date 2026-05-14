namespace Aica;

public class AppConfig
{
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();
    public string Model { get; init; } = "claude-sonnet-4-6";
    public string ApiKey { get; init; } = string.Empty;
    public bool AutoApprove { get; init; }

    public static AppConfig Load(DirectoryInfo? workdir, string model, bool autoApprove)
    {
        // EnvFile.Apply() has already been called in Program.cs before this point,
        // so ANTHROPIC_API_KEY is available via Environment if it was in env.json.
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                $"No API key found.\n" +
                $"  Run:  aica config set ANTHROPIC_API_KEY <your-key>\n" +
                $"  Or:   set the ANTHROPIC_API_KEY environment variable\n" +
                $"  File: {EnvFile.FilePath}");

        var settings = Settings.Load();
        var resolvedModel = model != "claude-sonnet-4-6"
            ? model
            : settings.DefaultModel ?? model;

        var resolvedWorkdir = workdir?.FullName ?? Directory.GetCurrentDirectory();
        if (!Directory.Exists(resolvedWorkdir))
            throw new DirectoryNotFoundException(
                $"Working directory does not exist: {resolvedWorkdir}");

        return new AppConfig
        {
            WorkingDirectory = resolvedWorkdir,
            Model = resolvedModel,
            ApiKey = apiKey,
            AutoApprove = autoApprove
        };
    }
}
